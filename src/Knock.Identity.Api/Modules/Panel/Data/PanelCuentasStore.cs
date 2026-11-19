using Knock.Identity.Api.Hosting;
using Knock.Identity.Api.Modules.Panel.Domain;
using Knock.Identity.Api.Modules.Sesiones.Data;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Identity.Api.Modules.Panel.Data;

/// <summary>
/// <c>panel_cuentas</c> de la base del servicio (spec 033, §6). Es el único punto del módulo con esta colección. Los filtros son tipados del driver; el
/// contador de fallos usa <c>$inc</c> atómico en MongoDB (persistente y compartido entre réplicas, R-11). Nada de la colección se imprime.
/// </summary>
internal sealed class PanelCuentasStore(
    [FromKeyedServices(SesionesDataExtensions.ModuleKey)] IMongoDatabase database,
    IOptions<KnockMongoOptions> mongoOptions) : IPanelCuentasStore
{
    public const string Collection = "panel_cuentas";

    public const string IdField = "_id";
    public const string SubField = "sub";
    public const string NameField = "nombre";
    public const string RolesField = "roles";
    public const string StateField = "estado";
    public const string VersionField = "permisosVersion";
    public const string SecretField = "totpSecretoCifrado";
    public const string KeyVersionField = "totpVersionLlave";
    public const string LastStepField = "totpUltimoPaso";
    public const string TotpStateField = "totpEstado";
    public const string FailuresField = "intentosFallidos";
    public const string WindowStartField = "ventanaFallosInicia";
    public const string LockedUntilField = "bloqueadoHasta";
    public const string CreatedAtField = "creadaEn";
    public const string UpdatedAtField = "actualizadaEn";
    public const string SessionsVersionField = "versionSesiones";

    private static readonly FilterDefinitionBuilder<BsonDocument> _filter = Builders<BsonDocument>.Filter;
    private static readonly UpdateDefinitionBuilder<BsonDocument> _update = Builders<BsonDocument>.Update;

    private readonly FindOptions _findOptions = new() { MaxTime = mongoOptions.Value.QueryMaxTime };

    public IMongoCollection<BsonDocument> Accounts { get; } = database.GetCollection<BsonDocument>(Collection);

    /// <summary>Índice único en <c>sub</c> (idempotente); lo invocan las pruebas y el script <c>db/044-identity-panel.js</c>.</summary>
    public async Task EnsureIndexesAsync(CancellationToken cancellationToken) =>
        await Accounts.Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending(SubField), new CreateIndexOptions { Unique = true, Name = "sub_1" }),
            cancellationToken: cancellationToken);

    public async Task<CuentaPanel?> FindBySubAsync(string sub, CancellationToken cancellationToken) =>
        ToAccount(await Accounts.Find(_filter.Eq(SubField, sub), _findOptions).Limit(1).FirstOrDefaultAsync(cancellationToken));

    public async Task<bool> InsertAsync(NuevaCuentaPanel account, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(account);
        var configured = account.TotpSecretoCifrado is { Length: > 0 };
        var document = new BsonDocument
        {
            { IdField, ObjectId.GenerateNewId() },
            { SubField, account.Sub },
            { NameField, account.Nombre },
            { RolesField, new BsonArray(account.Roles) },
            { StateField, PanelCuentaEstado.Activa },
            { VersionField, 1 },
            { SecretField, configured ? new BsonBinaryData(account.TotpSecretoCifrado!) : BsonNull.Value },
            { KeyVersionField, account.TotpVersionLlave },
            { LastStepField, 0L },
            { TotpStateField, configured ? PanelTotpEstado.Configurado : PanelTotpEstado.Pendiente },
            { FailuresField, 0 },
            { WindowStartField, BsonNull.Value },
            { LockedUntilField, BsonNull.Value },
            { CreatedAtField, new BsonDateTime(account.CreadaEn) },
            { UpdatedAtField, new BsonDateTime(account.CreadaEn) },
        };
        try
        {
            await Accounts.InsertOneAsync(document, cancellationToken: cancellationToken);
            return true;
        }
        catch (Exception exception) when (SesionesStore.IsDuplicateKey(exception))
        {
            return false;
        }
    }

    public Task<CuentaPanel?> SetRolesAsync(string sub, IReadOnlyList<string> roles, DateTime now, CancellationToken cancellationToken) =>
        ChangeAsync(sub, _update.Set(RolesField, new BsonArray(roles)), now, cancellationToken);

    public Task<CuentaPanel?> SetEstadoAsync(string sub, string estado, DateTime now, CancellationToken cancellationToken) =>
        ChangeAsync(sub, _update.Set(StateField, estado), now, cancellationToken);

    public Task<CuentaPanel?> ResetTotpAsync(string sub, DateTime now, CancellationToken cancellationToken) =>
        ChangeAsync(
            sub,
            _update.Combine(
                _update.Set(TotpStateField, PanelTotpEstado.Pendiente),
                _update.Set(SecretField, BsonNull.Value),
                _update.Set(KeyVersionField, 0),
                _update.Set(LastStepField, 0L),
                _update.Set(FailuresField, 0),
                _update.Set(WindowStartField, BsonNull.Value),
                _update.Set(LockedUntilField, BsonNull.Value)),
            now,
            cancellationToken);

    public async Task<bool> TryAcceptStepAsync(string sub, long step, CancellationToken cancellationToken)
    {
        var result = await Accounts.UpdateOneAsync(
            _filter.And(_filter.Eq(SubField, sub), _filter.Lt(LastStepField, step)),
            _update.Set(LastStepField, step),
            cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    public async Task<CuentaPanel?> RegisterFailureAsync(string sub, DateTime now, CancellationToken cancellationToken)
    {
        // 1) Si la ventana fija terminó (o nunca empezó) se abre una nueva con el contador en cero. Es condicional: una ventana viva no se toca.
        var ended = _filter.Or(
            _filter.Eq(WindowStartField, BsonNull.Value),
            _filter.Lte(WindowStartField, new BsonDateTime(now - PanelSesionReglas.FailureWindow)));
        await Accounts.UpdateOneAsync(
            _filter.And(_filter.Eq(SubField, sub), ended),
            _update.Combine(
                _update.Set(WindowStartField, new BsonDateTime(now)),
                _update.Set(FailuresField, 0),
                _update.Set(LockedUntilField, BsonNull.Value)),
            cancellationToken: cancellationToken);

        // 2) El conteo es un $inc atómico del servidor: dos réplicas que fallan a la vez suman las dos.
        var after = await Accounts.FindOneAndUpdateAsync(
            _filter.Eq(SubField, sub),
            _update.Inc(FailuresField, 1),
            new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After },
            cancellationToken);
        if (after is null)
        {
            return null;
        }

        var account = ToAccount(after)!;
        if (account.IntentosFallidos < PanelSesionReglas.MaxAccountFailures || account.EstaBloqueada(now) || account.VentanaFallosInicia is not { } windowStart)
        {
            return account;
        }

        // 3) Al llegar al máximo la cuenta queda bloqueada hasta el final de la ventana.
        var lockedUntil = PanelSesionReglas.LockUntil(windowStart);
        await Accounts.UpdateOneAsync(
            _filter.And(_filter.Eq(SubField, sub), _filter.Gte(FailuresField, PanelSesionReglas.MaxAccountFailures)),
            _update.Set(LockedUntilField, new BsonDateTime(lockedUntil)),
            cancellationToken: cancellationToken);
        return account with { BloqueadoHasta = lockedUntil };
    }

    public async Task ResetFailuresAsync(string sub, CancellationToken cancellationToken) =>
        await Accounts.UpdateOneAsync(
            _filter.Eq(SubField, sub),
            _update.Combine(_update.Set(FailuresField, 0), _update.Set(WindowStartField, BsonNull.Value), _update.Set(LockedUntilField, BsonNull.Value)),
            cancellationToken: cancellationToken);

    private async Task<CuentaPanel?> ChangeAsync(string sub, UpdateDefinition<BsonDocument> change, DateTime now, CancellationToken cancellationToken)
    {
        var update = _update.Combine(change, _update.Inc(VersionField, 1), _update.Set(UpdatedAtField, new BsonDateTime(now)));
        var after = await Accounts.FindOneAndUpdateAsync(
            _filter.Eq(SubField, sub),
            update,
            new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After },
            cancellationToken);
        return ToAccount(after);
    }

    internal static CuentaPanel? ToAccount(BsonDocument? document)
    {
        if (document is null)
        {
            return null;
        }

        return new CuentaPanel(
            document[IdField].AsObjectId.ToString(),
            document[SubField].AsString,
            document[NameField].AsString,
            [.. document[RolesField].AsBsonArray.Select(role => role.AsString)],
            document[StateField].AsString,
            document[VersionField].ToInt32(),
            OptionalBytes(document[SecretField]),
            document[KeyVersionField].ToInt32(),
            document[LastStepField].ToInt64(),
            document[TotpStateField].AsString,
            document[FailuresField].ToInt32(),
            OptionalDate(document[WindowStartField]),
            OptionalDate(document[LockedUntilField]));
    }

    private static byte[]? OptionalBytes(BsonValue value) => value.IsBsonNull ? null : value.AsBsonBinaryData.Bytes;

    private static DateTime? OptionalDate(BsonValue value) => value.IsBsonNull ? null : value.ToUniversalTime();
}
