using Knock.Api.Hosting;
using Knock.Api.Modules.Identity.Domain;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Api.Modules.Identity.Data;

/// <summary>
/// Colecciones <c>usuarios</c>, <c>consentimientos</c>, <c>auditoria</c> y <c>refresh_tokens</c> de la base del módulo
/// (<c>Mongo:Databases:Identity</c>, hoy <c>identity</c>). Es el único punto del módulo con <c>IMongoCollection</c> (constitución, principio 10).
/// Los filtros son tipados del driver: ninguna entrada se arma como JSON. Ninguna colección se cachea (<c>cache: no</c>): son datos personales y de
/// sesión. La auditoría es solo de inserción: esta clase no expone ninguna operación que la actualice o la borre.
/// </summary>
internal sealed class IdentityStore(
    [FromKeyedServices(IdentityDataExtensions.ModuleKey)] IMongoDatabase database,
    IMongoClient client,
    IOptions<KnockMongoOptions> mongoOptions) : IIdentityStore, IRefreshSessionStore
{
    public const string UsersCollection = "usuarios";
    public const string ConsentsCollection = "consentimientos";
    public const string AuditCollection = "auditoria";
    public const string RefreshTokensCollection = "refresh_tokens";

    public const string IdField = "_id";
    public const string SubField = "sub";
    public const string PhoneCiphertextField = "celularCifrado";
    public const string PhoneCiphertextVersionField = "celularCifradoVersion";
    public const string PhoneHashField = "celularHash";
    public const string PhoneHashVersionField = "celularHashVersion";
    public const string StateField = "estado";
    public const string ProfileCompleteField = "perfilCompleto";
    public const string CreatedAtField = "creadoEn";
    public const string VersionField = "version";
    public const string UserIdField = "usuarioId";
    public const string DocumentField = "documento";
    public const string AcceptedAtField = "aceptadoEn";
    public const string DeviceIdField = "dispositivoId";
    public const string EventField = "evento";
    public const string OccurredAtField = "ocurridoEn";
    public const string CorrelationIdField = "correlationId";
    public const string TraceIdField = "traceId";
    public const string FamilyIdField = "familiaId";
    public const string FingerprintField = "huella";
    public const string ExpiresAtField = "venceEn";
    public const string ReplacedByField = "reemplazadoPor";
    public const string RevokedAtField = "revocadoEn";
    public const string RevocationReasonField = "motivoRevocacion";

    /// <summary>Código de «write conflict» de MongoDB y documentdb-local para escrituras concurrentes en transacciones.</summary>
    internal const int WriteConflictCode = 112;

    private const int _maxWriteConflictAttempts = 6;

    private static readonly FilterDefinitionBuilder<BsonDocument> _filter = Builders<BsonDocument>.Filter;

    private static readonly ProjectionDefinition<BsonDocument> _refreshProjection = Builders<BsonDocument>.Projection
        .Include(UserIdField)
        .Include(FamilyIdField)
        .Include(DeviceIdField)
        .Include(ReplacedByField)
        .Include(RevokedAtField);

    private static readonly ProjectionDefinition<BsonDocument> _accountProjection = Builders<BsonDocument>.Projection
        .Include(SubField)
        .Include(StateField)
        .Include(ProfileCompleteField)
        .Include(PhoneHashField);

    private readonly FindOptions _findOptions = new() { MaxTime = mongoOptions.Value.QueryMaxTime };

    public IMongoCollection<BsonDocument> Users { get; } = database.GetCollection<BsonDocument>(UsersCollection);

    public IMongoCollection<BsonDocument> Consents { get; } = database.GetCollection<BsonDocument>(ConsentsCollection);

    public IMongoCollection<BsonDocument> Audit { get; } = database.GetCollection<BsonDocument>(AuditCollection);

    public IMongoCollection<BsonDocument> RefreshTokens { get; } = database.GetCollection<BsonDocument>(RefreshTokensCollection);

    /// <summary>
    /// Índices de spec §5. Es idempotente: crearlos otra vez con la misma definición no cambia nada; si uno no se puede crear, lanza.
    /// Los invocan las pruebas y el script de entrega de índices (TT-BD-09), igual que el cargador del catálogo.
    /// </summary>
    public async Task EnsureIndexesAsync(CancellationToken cancellationToken)
    {
        var keys = Builders<BsonDocument>.IndexKeys;
        await Users.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<BsonDocument>(keys.Ascending(SubField), new CreateIndexOptions { Unique = true }),
                new CreateIndexModel<BsonDocument>(keys.Ascending(PhoneHashField), new CreateIndexOptions { Unique = true }),
            ],
            cancellationToken);
        await Consents.Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(
                keys.Ascending(UserIdField).Ascending(DocumentField).Ascending(VersionField),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);
        await Audit.Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(keys.Ascending(UserIdField).Descending(OccurredAtField)),
            cancellationToken: cancellationToken);
        await RefreshTokens.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<BsonDocument>(keys.Ascending(FingerprintField), new CreateIndexOptions { Unique = true }),
                new CreateIndexModel<BsonDocument>(keys.Ascending(ExpiresAtField), new CreateIndexOptions { ExpireAfter = TimeSpan.Zero }),
                new CreateIndexModel<BsonDocument>(keys.Ascending(FamilyIdField)),
            ],
            cancellationToken);
    }

    public async Task<UserAccount?> FindBySubAsync(string sub, CancellationToken cancellationToken) =>
        ToAccount(await Users.Find(_filter.Eq(SubField, sub), _findOptions).Project(_accountProjection).Limit(1)
            .FirstOrDefaultAsync(cancellationToken));

    /// <summary>Busca por el HMAC del celular con el índice único: no descifra nada.</summary>
    public async Task<UserAccount?> FindByPhoneHashAsync(byte[] phoneHash, CancellationToken cancellationToken) =>
        ToAccount(await Users.Find(_filter.Eq(PhoneHashField, new BsonBinaryData(phoneHash)), _findOptions).Project(_accountProjection).Limit(1)
            .FirstOrDefaultAsync(cancellationToken));

    /// <summary>
    /// Guarda usuario, dos consentimientos, la auditoría <c>USER_REGISTERED</c> y la huella de la primera sesión en una sola transacción: si
    /// cualquier escritura falla no queda nada. Si un índice único rechaza el <c>sub</c> o el celular (otro canje ganó la carrera), no guarda nada y
    /// devuelve <see cref="AccountCreation.AlreadyExists"/>.
    /// </summary>
    public async Task<AccountCreation> CreateAccountAsync(NewAccount account, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(account);
        var userId = ObjectId.Parse(account.Id);
        var createdAt = new BsonDateTime(account.CreatedAt);
        var attempt = 0;
        while (true)
        {
            attempt++;
            try
            {
                using var session = await client.StartSessionAsync(cancellationToken: cancellationToken);
                await session.WithTransactionAsync(
                    async (transaction, token) =>
                    {
                        await Users.InsertOneAsync(transaction, UserDocument(account, userId, createdAt), cancellationToken: token);
                        await Consents.InsertManyAsync(transaction, ConsentDocuments(account, userId, createdAt), cancellationToken: token);
                        await Audit.InsertOneAsync(transaction, AuditDocument(account, userId, createdAt), cancellationToken: token);
                        await RefreshTokens.InsertOneAsync(transaction, RefreshDocument(account.Refresh), cancellationToken: token);
                        return true;
                    },
                    cancellationToken: cancellationToken);
                return AccountCreation.Created;
            }
            catch (Exception exception) when (IsDuplicateKey(exception))
            {
                return AccountCreation.AlreadyExists;
            }
            catch (MongoCommandException exception) when (exception.Code == WriteConflictCode && attempt < _maxWriteConflictAttempts)
            {
                // Otro canje está creando la misma cuenta y todavía no confirma. documentdb-local responde 112 sin la etiqueta de error
                // transitorio, así que el driver no reintenta solo; al confirmarse el otro, el reintento recibe la clave duplicada.
                await Task.Delay(WriteConflictBackoff(attempt), cancellationToken);
            }
        }
    }

    /// <summary>Espera técnica y corta entre reintentos de un conflicto de escritura; no es un plazo de negocio.</summary>
    private static TimeSpan WriteConflictBackoff(int attempt) => TimeSpan.FromMilliseconds(50 * attempt);

    /// <summary>Guarda la huella de una sesión de una cuenta que ya existe (un canje de cuenta existente solo inserta esto).</summary>
    public Task AddRefreshTokenAsync(RefreshTokenRecord refresh, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(refresh);
        return RefreshTokens.InsertOneAsync(RefreshDocument(refresh), cancellationToken: cancellationToken);
    }

    /// <summary>Cuenta por <c>_id</c>: la renovación necesita su estado y su <c>sub</c> para emitir el JWT.</summary>
    public async Task<UserAccount?> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
        ToAccount(await Users.Find(_filter.Eq(IdField, ObjectId.Parse(userId)), _findOptions).Project(_accountProjection).Limit(1)
            .FirstOrDefaultAsync(cancellationToken));

    /// <summary>
    /// Rotación atómica (spec HU-003 §5): en una transacción marca el refresh con esa huella como reemplazado (solo si no lo estaba, no está
    /// revocado y no venció) e inserta el siguiente de su misma familia; si dos renovaciones llegan a la vez con el mismo refresh gana una y la otra
    /// ve el documento ya reemplazado (reuso).
    /// </summary>
    public async Task<RefreshRotation> RotateAsync(byte[] fingerprint, byte[] nextFingerprint, DateTime now, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fingerprint);
        ArgumentNullException.ThrowIfNull(nextFingerprint);
        var attempt = 0;
        while (true)
        {
            attempt++;
            try
            {
                using var session = await client.StartSessionAsync(cancellationToken: cancellationToken);
                var parent = await session.WithTransactionAsync(
                    (transaction, token) => ConsumeAsync(transaction, fingerprint, nextFingerprint, now, token),
                    cancellationToken: cancellationToken);
                return parent is null ? await ClassifyMissAsync(fingerprint, now, cancellationToken) : RefreshRotation.Rotated(parent);
            }
            catch (MongoCommandException exception) when (exception.Code == WriteConflictCode && attempt < _maxWriteConflictAttempts)
            {
                // Otra renovación con el mismo refresh todavía no confirma; al confirmarse, el reintento ve el documento reemplazado (reuso).
                await Task.Delay(WriteConflictBackoff(attempt), cancellationToken);
            }
        }
    }

    public Task RevokeFamilyAsync(string familyId, string reason, DateTime now, CancellationToken cancellationToken) =>
        RefreshTokens.UpdateManyAsync(
            _filter.And(_filter.Eq(FamilyIdField, ObjectId.Parse(familyId)), _filter.Eq(RevokedAtField, BsonNull.Value)),
            Builders<BsonDocument>.Update.Set(RevokedAtField, new BsonDateTime(now)).Set(RevocationReasonField, reason),
            cancellationToken: cancellationToken);

    public async Task<StoredRefresh?> FindByFingerprintAsync(byte[] fingerprint, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fingerprint);
        var existing = await FindRefreshAsync(fingerprint, cancellationToken);
        return existing is null ? null : ToStored(existing);
    }

    private async Task<StoredRefresh?> ConsumeAsync(
        IClientSessionHandle transaction, byte[] fingerprint, byte[] nextFingerprint, DateTime now, CancellationToken token)
    {
        var nextId = ObjectId.GenerateNewId();
        var nowBson = new BsonDateTime(now);
        var claimed = await RefreshTokens.FindOneAndUpdateAsync(
            transaction,
            _filter.And(
                _filter.Eq(FingerprintField, new BsonBinaryData(fingerprint)),
                _filter.Eq(ReplacedByField, BsonNull.Value),
                _filter.Eq(RevokedAtField, BsonNull.Value),
                _filter.Gt(ExpiresAtField, nowBson)),
            Builders<BsonDocument>.Update.Set(ReplacedByField, nextId),
            new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.Before },
            token);
        if (claimed is null)
        {
            return null;
        }

        await RefreshTokens.InsertOneAsync(transaction, NextRefreshDocument(claimed, nextId, nextFingerprint, now), cancellationToken: token);
        return ToStored(claimed);
    }

    /// <summary>
    /// No se pudo consumir: si el refresh existe y ya estaba reemplazado o revocado es un reuso y se revoca su familia; si no existe o solo está
    /// vencido, no se toca nada.
    /// </summary>
    private async Task<RefreshRotation> ClassifyMissAsync(byte[] fingerprint, DateTime now, CancellationToken cancellationToken)
    {
        var existing = await FindRefreshAsync(fingerprint, cancellationToken);
        if (existing is null)
        {
            return RefreshRotation.Missing;
        }

        var consumed = !existing.GetValue(ReplacedByField, BsonNull.Value).IsBsonNull || !existing.GetValue(RevokedAtField, BsonNull.Value).IsBsonNull;
        if (!consumed)
        {
            return RefreshRotation.Missing;
        }

        await RevokeFamilyAsync(ToStored(existing).FamilyId, RevocationReasons.Reuse, now, cancellationToken);
        return RefreshRotation.Reused;
    }

    private Task<BsonDocument?> FindRefreshAsync(byte[] fingerprint, CancellationToken cancellationToken) =>
        RefreshTokens.Find(_filter.Eq(FingerprintField, new BsonBinaryData(fingerprint)), _findOptions).Project(_refreshProjection).Limit(1)
            .FirstOrDefaultAsync(cancellationToken)!;

    // reemplazadoPor, revocadoEn y motivoRevocacion no se escriben al insertar: ausente equivale a null en los filtros (los documentos de HU-001 se
    // leen igual) y solo los escribe quien reemplaza o revoca.
    private static BsonDocument NextRefreshDocument(BsonDocument parent, ObjectId nextId, byte[] nextFingerprint, DateTime now) => new()
    {
        { IdField, nextId },
        { UserIdField, parent[UserIdField] },
        { FamilyIdField, parent[FamilyIdField] },
        { FingerprintField, new BsonBinaryData(nextFingerprint) },
        { DeviceIdField, parent[DeviceIdField] },
        { CreatedAtField, new BsonDateTime(now) },
        { ExpiresAtField, new BsonDateTime(now + RefreshTokenRecord.Lifetime) },
    };

    private static StoredRefresh ToStored(BsonDocument document) =>
        new(document[UserIdField].AsObjectId.ToString(), document[FamilyIdField].AsObjectId.ToString(), document[DeviceIdField].AsString);

    internal static bool IsDuplicateKey(Exception exception) => exception switch
    {
        MongoWriteException write => write.WriteError.Category == ServerErrorCategory.DuplicateKey,
        MongoBulkWriteException bulk => bulk.WriteErrors.Any(error => error.Category == ServerErrorCategory.DuplicateKey),
        MongoCommandException command => command.Code == 11000,
        _ => false,
    };

    private static BsonDocument UserDocument(NewAccount account, ObjectId userId, BsonDateTime createdAt) => new()
    {
        { IdField, userId },
        { SubField, account.Sub },
        { PhoneCiphertextField, new BsonBinaryData(account.PhoneCiphertext) },
        { PhoneCiphertextVersionField, account.PhoneCiphertextVersion },
        { PhoneHashField, new BsonBinaryData(account.PhoneHash) },
        { PhoneHashVersionField, account.PhoneHashVersion },
        { StateField, UserState.Active },
        { ProfileCompleteField, false },
        { CreatedAtField, createdAt },
        { VersionField, 1 },
    };

    private static List<BsonDocument> ConsentDocuments(NewAccount account, ObjectId userId, BsonDateTime acceptedAt) =>
    [
        ConsentDocument(account, userId, acceptedAt, Domain.ConsentDocuments.PrivacyNotice, Domain.ConsentDocuments.PrivacyNoticeVersion),
        ConsentDocument(account, userId, acceptedAt, Domain.ConsentDocuments.Terms, Domain.ConsentDocuments.TermsVersion),
    ];

    private static BsonDocument ConsentDocument(NewAccount account, ObjectId userId, BsonDateTime acceptedAt, string document, string version) => new()
    {
        { IdField, ObjectId.GenerateNewId() },
        { UserIdField, userId },
        { DocumentField, document },
        { VersionField, version },
        { AcceptedAtField, acceptedAt },
        { DeviceIdField, account.DeviceId },
    };

    private static BsonDocument AuditDocument(NewAccount account, ObjectId userId, BsonDateTime occurredAt) => new()
    {
        { IdField, ObjectId.GenerateNewId() },
        { EventField, AuditEvents.UserRegistered },
        { UserIdField, userId },
        { OccurredAtField, occurredAt },
        { CorrelationIdField, account.CorrelationId },
        { TraceIdField, account.TraceId },
    };

    private static BsonDocument RefreshDocument(RefreshTokenRecord refresh) => new()
    {
        { IdField, ObjectId.GenerateNewId() },
        { UserIdField, ObjectId.Parse(refresh.UserId) },
        { FamilyIdField, ObjectId.Parse(refresh.FamilyId) },
        { FingerprintField, new BsonBinaryData(refresh.Fingerprint) },
        { DeviceIdField, refresh.DeviceId },
        { CreatedAtField, new BsonDateTime(refresh.CreatedAt) },
        { ExpiresAtField, new BsonDateTime(refresh.ExpiresAt) },
    };

    private static UserAccount? ToAccount(BsonDocument? document) =>
        document is null
            ? null
            : new UserAccount(
                document[IdField].AsObjectId.ToString(),
                document[SubField].AsString,
                document[StateField].AsString,
                document[ProfileCompleteField].AsBoolean,
                document[PhoneHashField].AsBsonBinaryData.Bytes);
}
