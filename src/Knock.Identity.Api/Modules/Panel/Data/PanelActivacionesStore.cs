using Knock.Identity.Api.Hosting;
using Knock.Identity.Api.Modules.Panel.Domain;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Identity.Api.Modules.Panel.Data;

/// <summary>
/// <c>panel_activaciones</c> (spec 033, §6, R-29): invitaciones de un solo uso para activar el TOTP. Solo se guarda la huella SHA-256 del token. El consumo
/// es una transacción que marca la invitación y pasa la cuenta de <c>pendiente</c> a <c>configurado</c>: de dos consumos simultáneos gana uno.
/// </summary>
internal sealed class PanelActivacionesStore(
    [FromKeyedServices(DataExtensions.DatabaseKey)] IMongoDatabase database,
    IMongoClient client,
    IOptions<KnockMongoOptions> mongoOptions) : IPanelActivacionesStore
{
    public const string Collection = "panel_activaciones";

    public const string AccountIdField = "cuentaId";
    public const string FingerprintField = "huella";
    public const string CreatedAtField = "creadaEn";
    public const string ExpiresAtField = "venceEn";
    public const string ConsumedAtField = "consumidaEn";

    private const int _maxWriteConflictAttempts = 6;

    private static readonly TimeSpan _deleteAfter = TimeSpan.FromHours(24);
    private static readonly FilterDefinitionBuilder<BsonDocument> _filter = Builders<BsonDocument>.Filter;
    private static readonly UpdateDefinitionBuilder<BsonDocument> _update = Builders<BsonDocument>.Update;

    private readonly FindOptions _findOptions = new() { MaxTime = mongoOptions.Value.QueryMaxTime };

    public IMongoCollection<BsonDocument> Invitations { get; } = database.GetCollection<BsonDocument>(Collection);

    private IMongoCollection<BsonDocument> Accounts { get; } = database.GetCollection<BsonDocument>(PanelCuentasStore.Collection);

    /// <summary>Índices de §6 (idempotentes); los invocan las pruebas y el script <c>db/044-identity-panel.js</c>.</summary>
    public async Task EnsureIndexesAsync(CancellationToken cancellationToken)
    {
        var keys = Builders<BsonDocument>.IndexKeys;
        await Invitations.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<BsonDocument>(keys.Ascending(FingerprintField), new CreateIndexOptions { Unique = true, Name = "huella_1" }),
                new CreateIndexModel<BsonDocument>(keys.Ascending(AccountIdField), new CreateIndexOptions { Unique = true, Name = "cuentaId_1" }),
                new CreateIndexModel<BsonDocument>(keys.Ascending(CreatedAtField), new CreateIndexOptions { ExpireAfter = _deleteAfter, Name = "creadaEn_1" }),
            ],
            cancellationToken);
    }

    public async Task CreateAsync(string accountId, byte[] fingerprint, DateTime now, DateTime expiresAt, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fingerprint);
        var id = ObjectId.Parse(accountId);
        await Invitations.DeleteManyAsync(_filter.Eq(AccountIdField, id), cancellationToken);
        await Invitations.InsertOneAsync(
            new BsonDocument
            {
                { AccountIdField, id },
                { FingerprintField, new BsonBinaryData(fingerprint) },
                { CreatedAtField, new BsonDateTime(now) },
                { ExpiresAtField, new BsonDateTime(expiresAt) },
                { ConsumedAtField, BsonNull.Value },
            },
            cancellationToken: cancellationToken);
    }

    public async Task<InvitacionVigente?> FindLiveAsync(byte[] fingerprint, DateTime now, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fingerprint);
        var invitation = await Invitations.Find(LiveFilter(fingerprint, now), _findOptions).Limit(1).FirstOrDefaultAsync(cancellationToken);
        if (invitation is null)
        {
            return null;
        }

        var accountId = invitation[AccountIdField].AsObjectId;
        var account = await Accounts.Find(_filter.And(_filter.Eq(PanelCuentasStore.IdField, accountId), _filter.Eq(PanelCuentasStore.TotpStateField, PanelTotpEstado.Pendiente)), _findOptions)
            .Limit(1).FirstOrDefaultAsync(cancellationToken);
        return account is null ? null : new InvitacionVigente(accountId.ToString(), account[PanelCuentasStore.SubField].AsString);
    }

    public async Task<ActivacionResultado> ConsumeAsync(byte[] fingerprint, string accountId, ProtectedSecretForStore secret, DateTime now, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fingerprint);
        ArgumentNullException.ThrowIfNull(secret);
        var id = ObjectId.Parse(accountId);
        var attempt = 0;
        while (true)
        {
            attempt++;
            try
            {
                using var session = await client.StartSessionAsync(cancellationToken: cancellationToken);
                return await session.WithTransactionAsync(
                    async (transaction, token) =>
                    {
                        var marked = await Invitations.UpdateOneAsync(
                            transaction,
                            _filter.And(LiveFilter(fingerprint, now), _filter.Eq(AccountIdField, id)),
                            _update.Set(ConsumedAtField, new BsonDateTime(now)),
                            cancellationToken: token);
                        if (marked.ModifiedCount != 1)
                        {
                            return ActivacionResultado.Invalida;
                        }

                        var configured = await Accounts.UpdateOneAsync(
                            transaction,
                            _filter.And(_filter.Eq(PanelCuentasStore.IdField, id), _filter.Eq(PanelCuentasStore.TotpStateField, PanelTotpEstado.Pendiente)),
                            _update.Combine(
                                _update.Set(PanelCuentasStore.TotpStateField, PanelTotpEstado.Configurado),
                                _update.Set(PanelCuentasStore.SecretField, new BsonBinaryData(secret.Ciphertext)),
                                _update.Set(PanelCuentasStore.KeyVersionField, secret.KeyVersion),
                                _update.Set(PanelCuentasStore.LastStepField, 0L),
                                _update.Set(PanelCuentasStore.UpdatedAtField, new BsonDateTime(now)),
                                _update.Inc(PanelCuentasStore.VersionField, 1)),
                            cancellationToken: token);
                        if (configured.ModifiedCount == 1)
                        {
                            return ActivacionResultado.Consumida;
                        }

                        await transaction.AbortTransactionAsync(token);
                        return ActivacionResultado.Invalida;
                    },
                    cancellationToken: cancellationToken);
            }
            catch (MongoCommandException exception) when (exception.Code == MongoWriteErrors.WriteConflictCode && attempt < _maxWriteConflictAttempts)
            {
                // Otro consumo de la misma invitación no confirma todavía: al reintentar ya la ve consumida y responde inválida.
                await Task.Delay(TimeSpan.FromMilliseconds(50 * attempt), cancellationToken);
            }
        }
    }

    private static FilterDefinition<BsonDocument> LiveFilter(byte[] fingerprint, DateTime now) =>
        _filter.And(
            _filter.Eq(FingerprintField, new BsonBinaryData(fingerprint)),
            _filter.Eq(ConsumedAtField, BsonNull.Value),
            _filter.Gt(ExpiresAtField, new BsonDateTime(now)));
}
