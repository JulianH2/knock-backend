using Knock.Identity.Api.Hosting;
using Knock.Identity.Api.Modules.Panel.Domain;
using Knock.Identity.Api.Modules.Sesiones.Data;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Identity.Api.Modules.Panel.Data;

/// <summary>
/// <c>panel_sesiones</c> (spec 033, §6): sesiones del panel con la huella SHA-256 del token de renovación (nunca el token), las últimas 5 huellas usadas y la
/// rotación condicionada a la huella vigente. Es el único punto del módulo con esta colección.
/// </summary>
internal sealed class PanelSesionesStore(
    [FromKeyedServices(SesionesDataExtensions.ModuleKey)] IMongoDatabase database,
    IMongoClient client,
    IOptions<KnockMongoOptions> mongoOptions,
    TimeProvider timeProvider) : IPanelSesionesStore
{
    public const string Collection = "panel_sesiones";

    public const string IdField = "_id";
    public const string SubField = "sub";
    public const string FingerprintField = "huellaRenovacion";
    public const string UsedField = "huellasUsadas";
    public const string RotatedAtField = "rotadaEn";
    public const string VersionField = "permisosVersion";
    public const string CreatedAtField = "creadaEn";
    public const string LastActivityField = "ultimaActividad";
    public const string RenewalExpiresField = "renovacionVenceEn";
    public const string ExpiresField = "venceEn";
    public const string RevokedField = "revocada";

    private const int _maxWriteConflictAttempts = 6;

    private static readonly FilterDefinitionBuilder<BsonDocument> _filter = Builders<BsonDocument>.Filter;
    private static readonly UpdateDefinitionBuilder<BsonDocument> _update = Builders<BsonDocument>.Update;

    private readonly FindOptions _findOptions = new() { MaxTime = mongoOptions.Value.QueryMaxTime };

    public IMongoCollection<BsonDocument> Sessions { get; } = database.GetCollection<BsonDocument>(Collection);

    private IMongoCollection<BsonDocument> Accounts { get; } = database.GetCollection<BsonDocument>(PanelCuentasStore.Collection);

    /// <summary>Índices de §6 (idempotentes); los invocan las pruebas y el script <c>db/044-identity-panel.js</c>.</summary>
    public async Task EnsureIndexesAsync(CancellationToken cancellationToken)
    {
        var keys = Builders<BsonDocument>.IndexKeys;
        await Sessions.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<BsonDocument>(keys.Ascending(FingerprintField), new CreateIndexOptions { Unique = true, Name = "huellaRenovacion_1" }),
                new CreateIndexModel<BsonDocument>(keys.Ascending(UsedField), new CreateIndexOptions { Name = "huellasUsadas_1" }),
                new CreateIndexModel<BsonDocument>(
                    keys.Ascending(SubField).Ascending(RevokedField).Ascending(CreatedAtField),
                    new CreateIndexOptions { Name = "sub_1_revocada_1_creadaEn_1" }),
                new CreateIndexModel<BsonDocument>(keys.Ascending(ExpiresField), new CreateIndexOptions { ExpireAfter = TimeSpan.Zero, Name = "venceEn_1" }),
            ],
            cancellationToken);
    }

    public async Task<IReadOnlyList<string>> CreateAsync(NuevaSesionPanel session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        var attempt = 0;
        while (true)
        {
            attempt++;
            try
            {
                using var mongoSession = await client.StartSessionAsync(cancellationToken: cancellationToken);
                return await mongoSession.WithTransactionAsync(
                    async (transaction, token) =>
                    {
                        // Dos accesos de la misma cuenta insertan documentos distintos y no chocan solos: tocar el documento de la cuenta fuerza el
                        // conflicto de escritura (112) y los serializa; el reintento ya cuenta la sesión nueva.
                        await Accounts.UpdateOneAsync(
                            transaction,
                            _filter.Eq(PanelCuentasStore.SubField, session.Sub),
                            _update.Inc(PanelCuentasStore.SessionsVersionField, 1),
                            cancellationToken: token);
                        await Sessions.InsertOneAsync(transaction, ToDocument(session), cancellationToken: token);
                        return await RevokeOldestBeyondTheLimitAsync(transaction, session.Sub, token);
                    },
                    cancellationToken: cancellationToken);
            }
            catch (MongoCommandException exception) when (exception.Code == SesionesStore.WriteConflictCode && attempt < _maxWriteConflictAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50 * attempt), cancellationToken);
            }
        }
    }

    public async Task<SesionPanel?> FindByFingerprintAsync(string fingerprint, CancellationToken cancellationToken) =>
        ToSession(await Sessions.Find(_filter.Eq(FingerprintField, fingerprint), _findOptions).Limit(1).FirstOrDefaultAsync(cancellationToken));

    public async Task<SesionPanel?> FindByUsedFingerprintAsync(string fingerprint, CancellationToken cancellationToken) =>
        ToSession(await Sessions.Find(_filter.AnyEq(UsedField, fingerprint), _findOptions).Limit(1).FirstOrDefaultAsync(cancellationToken));

    public async Task<SesionPanel?> FindByIdAsync(string sid, CancellationToken cancellationToken) =>
        ToSession(await Sessions.Find(_filter.Eq(IdField, sid), _findOptions).Limit(1).FirstOrDefaultAsync(cancellationToken));

    /// <summary>Un solo <c>updateOne</c> con la huella vigente en el filtro: MongoDB lo ejecuta de forma atómica sobre el documento (R-17).</summary>
    public async Task<bool> RotateAsync(string sid, string currentFingerprint, string newFingerprint, DateTime now, DateTime renewalExpiresAt, CancellationToken cancellationToken)
    {
        var filter = _filter.And(
            _filter.Eq(IdField, sid),
            _filter.Eq(FingerprintField, currentFingerprint),
            _filter.Eq(RevokedField, false),
            _filter.Gt(ExpiresField, new BsonDateTime(now)),
            _filter.Gt(RenewalExpiresField, new BsonDateTime(now)));
        var update = _update
            .Set(FingerprintField, newFingerprint)
            .Set(RotatedAtField, new BsonDateTime(now))
            .Set(LastActivityField, new BsonDateTime(now))
            .Set(RenewalExpiresField, new BsonDateTime(renewalExpiresAt))
            .PushEach(UsedField, [currentFingerprint], slice: -PanelSesionReglas.MaxUsedFingerprints);
        var result = await Sessions.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    public async Task<IReadOnlyList<string>> RevokeAllAsync(string sub, CancellationToken cancellationToken)
    {
        var open = _filter.And(_filter.Eq(SubField, sub), _filter.Eq(RevokedField, false));
        var ids = (await Sessions.Find(open, _findOptions).Project(Builders<BsonDocument>.Projection.Include(IdField)).ToListAsync(cancellationToken))
            .Select(document => document[IdField].AsString)
            .ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        await Sessions.UpdateManyAsync(_filter.In(IdField, ids), _update.Set(RevokedField, true), cancellationToken: cancellationToken);
        return ids;
    }

    public async Task<bool> RevokeAsync(string sid, CancellationToken cancellationToken)
    {
        var result = await Sessions.UpdateOneAsync(
            _filter.And(_filter.Eq(IdField, sid), _filter.Eq(RevokedField, false)),
            _update.Set(RevokedField, true),
            cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    private async Task<IReadOnlyList<string>> RevokeOldestBeyondTheLimitAsync(IClientSessionHandle transaction, string sub, CancellationToken cancellationToken)
    {
        var now = new BsonDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var open = _filter.And(_filter.Eq(SubField, sub), _filter.Eq(RevokedField, false), _filter.Gt(ExpiresField, now));
        var openIds = await Sessions.Find(transaction, open, _findOptions)
            .Project(Builders<BsonDocument>.Projection.Include(IdField))
            .Sort(Builders<BsonDocument>.Sort.Ascending(CreatedAtField).Ascending(IdField))
            .ToListAsync(cancellationToken);
        var excess = openIds.Count - PanelSesionReglas.MaxOpenSessions;
        if (excess <= 0)
        {
            return [];
        }

        var oldest = openIds.Take(excess).Select(document => document[IdField].AsString).ToList();
        await Sessions.UpdateManyAsync(transaction, _filter.In(IdField, oldest), _update.Set(RevokedField, true), cancellationToken: cancellationToken);
        return oldest;
    }

    private static BsonDocument ToDocument(NuevaSesionPanel session) => new()
    {
        { IdField, session.Id },
        { SubField, session.Sub },
        { FingerprintField, session.HuellaRenovacion },
        { UsedField, new BsonArray() },
        { RotatedAtField, new BsonDateTime(session.CreadaEn) },
        { VersionField, session.PermisosVersion },
        { CreatedAtField, new BsonDateTime(session.CreadaEn) },
        { LastActivityField, new BsonDateTime(session.CreadaEn) },
        { RenewalExpiresField, new BsonDateTime(session.RenovacionVenceEn) },
        { ExpiresField, new BsonDateTime(session.VenceEn) },
        { RevokedField, false },
    };

    private static SesionPanel? ToSession(BsonDocument? document) =>
        document is null
            ? null
            : new SesionPanel(
                document[IdField].AsString,
                document[SubField].AsString,
                document[FingerprintField].AsString,
                [.. document[UsedField].AsBsonArray.Select(value => value.AsString)],
                document[RotatedAtField].ToUniversalTime(),
                document[VersionField].ToInt32(),
                document[CreatedAtField].ToUniversalTime(),
                document[LastActivityField].ToUniversalTime(),
                document[RenewalExpiresField].ToUniversalTime(),
                document[ExpiresField].ToUniversalTime(),
                document[RevokedField].AsBoolean);
}
