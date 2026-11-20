using Knock.Identity.Api.Hosting;
using Knock.Identity.Api.Modules.Sesiones.Domain;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Identity.Api.Modules.Sesiones.Data;

/// <summary>
/// Colecciones <c>usuarios</c> y <c>sesiones</c> de la base del servicio (<c>Mongo:Databases:Identity</c>). Es el único punto del módulo con
/// <c>IMongoCollection</c> (spec 002). Los filtros son tipados del driver: ninguna entrada se arma como JSON. Ninguna colección se cachea:
/// son datos personales y de sesión.
/// </summary>
internal sealed class SesionesStore(
    [FromKeyedServices(SesionesDataExtensions.ModuleKey)] IMongoDatabase database,
    IMongoClient client,
    IOptions<KnockMongoOptions> mongoOptions,
    TimeProvider timeProvider) : ISesionesStore
{
    public const string UsersCollection = "usuarios";
    public const string SessionsCollection = "sesiones";

    public const string IdField = "_id";
    public const string SubField = "sub";
    public const string PhoneCiphertextField = "celularCifrado";
    public const string PhoneCiphertextVersionField = "celularCifradoVersion";
    public const string PhoneHashField = "celularHash";
    public const string PhoneHashVersionField = "celularHashVersion";
    public const string StateField = "estado";
    public const string IsTechnicianField = "esTecnico";
    public const string ProfileCompleteField = "perfilCompleto";
    public const string ConsentsField = "consentimientos";
    public const string SessionsVersionField = "versionSesiones";
    public const string DocumentField = "documento";
    public const string VersionField = "version";
    public const string AcceptedAtField = "aceptadoEn";
    public const string CreatedAtField = "creadoEn";
    public const string UserIdField = "usuarioId";
    public const string DeviceIdField = "deviceId";
    public const string DeviceNameField = "dispositivo";
    public const string FingerprintField = "huellaRenovacion";
    public const string UsedFingerprintsField = "huellasUsadas";
    public const string LastActivityField = "ultimaActividad";
    public const string ExpiresAtField = "venceEn";
    public const string RevokedField = "revocada";

    /// <summary>Código de «write conflict» de MongoDB y documentdb-local para escrituras concurrentes en transacciones.</summary>
    internal const int WriteConflictCode = 112;

    private const int _maxWriteConflictAttempts = 6;

    private static readonly FilterDefinitionBuilder<BsonDocument> _filter = Builders<BsonDocument>.Filter;

    private static readonly ProjectionDefinition<BsonDocument> _accountProjection = Builders<BsonDocument>.Projection
        .Include(SubField)
        .Include(StateField)
        .Include(ProfileCompleteField)
        .Include(PhoneHashField);

    /// <summary>Huellas usadas que se conservan por sesión para detectar un token ya rotado (spec 016, R-03).</summary>
    public const int MaxUsedFingerprints = 5;

    private static readonly ProjectionDefinition<BsonDocument> _sessionProjection = Builders<BsonDocument>.Projection
        .Include(IdField)
        .Include(UserIdField)
        .Include(SubField)
        .Include(DeviceNameField)
        .Include(LastActivityField)
        .Include(RevokedField)
        .Include(ExpiresAtField);

    private readonly FindOptions _findOptions = new() { MaxTime = mongoOptions.Value.QueryMaxTime };

    public IMongoCollection<BsonDocument> Users { get; } = database.GetCollection<BsonDocument>(UsersCollection);

    public IMongoCollection<BsonDocument> Sessions { get; } = database.GetCollection<BsonDocument>(SessionsCollection);

    /// <summary>
    /// Índices de la spec 015 (R-05, R-08). Es idempotente: crearlos otra vez con la misma definición no cambia nada. Los invocan las pruebas y el
    /// script de <c>db/</c> (<c>002_identidad_usuarios_sesiones.js</c>), igual que el cargador del catálogo.
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
        await Sessions.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<BsonDocument>(keys.Ascending(FingerprintField), new CreateIndexOptions { Unique = true }),
                new CreateIndexModel<BsonDocument>(keys.Ascending(ExpiresAtField), new CreateIndexOptions { ExpireAfter = TimeSpan.Zero }),
                new CreateIndexModel<BsonDocument>(keys.Ascending(UserIdField).Ascending(RevokedField).Ascending(CreatedAtField)),
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
    /// Guarda el usuario (con sus dos consentimientos) y la primera sesión en una sola transacción: si cualquier escritura falla no queda nada. Si un
    /// índice único rechaza el <c>sub</c> o el celular (otro canje ganó la carrera), no guarda nada y devuelve <see cref="AccountCreation.AlreadyExists"/>.
    /// </summary>
    public async Task<AccountCreation> CreateAccountAsync(NewAccount account, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(account);
        var userId = ObjectId.Parse(account.Id);
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
                        await Users.InsertOneAsync(transaction, UserDocument(account, userId), cancellationToken: token);
                        await Sessions.InsertOneAsync(transaction, SessionDocument(account.Session), cancellationToken: token);
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

    /// <summary>
    /// Inserta la sesión de una cuenta que ya existe y aplica el máximo de sesiones abiertas: si la cuenta queda con más de
    /// <see cref="NewSession.MaxOpenPerAccount"/>, revoca las más antiguas hasta quedar en el máximo, en una sola transacción.
    /// </summary>
    public async Task AddSessionAsync(NewSession session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        var userId = ObjectId.Parse(session.UserId);
        var attempt = 0;
        while (true)
        {
            attempt++;
            try
            {
                using var mongoSession = await client.StartSessionAsync(cancellationToken: cancellationToken);
                await mongoSession.WithTransactionAsync(
                    async (transaction, token) =>
                    {
                        // Dos canjes de la misma cuenta insertan documentos distintos y no chocan solos: ambos verían 9 abiertas y dejarían 11.
                        // Tocar el mismo documento del usuario fuerza el conflicto de escritura (112) y los serializa; el reintento ya cuenta la nueva.
                        await Users.UpdateOneAsync(
                            transaction,
                            _filter.Eq(IdField, userId),
                            Builders<BsonDocument>.Update.Inc(SessionsVersionField, 1),
                            cancellationToken: token);
                        await Sessions.InsertOneAsync(transaction, SessionDocument(session), cancellationToken: token);
                        await RevokeOldestBeyondTheLimitAsync(transaction, userId, token);
                        return true;
                    },
                    cancellationToken: cancellationToken);
                return;
            }
            catch (MongoCommandException exception) when (exception.Code == WriteConflictCode && attempt < _maxWriteConflictAttempts)
            {
                await Task.Delay(WriteConflictBackoff(attempt), cancellationToken);
            }
        }
    }

    public async Task<IReadOnlyList<StoredSession>> FindOpenSessionsAsync(string userId, DateTime now, CancellationToken cancellationToken)
    {
        var userObjectId = ObjectId.Parse(userId);
        var filter = _filter.And(
            _filter.Eq(UserIdField, userObjectId),
            _filter.Eq(RevokedField, false),
            _filter.Gt(ExpiresAtField, new BsonDateTime(now)));
        var documents = await Sessions.Find(filter, _findOptions)
            .Project(_sessionProjection)
            .Sort(Builders<BsonDocument>.Sort.Descending(LastActivityField).Descending(IdField))
            .ToListAsync(cancellationToken);
        return documents.Select(ToStoredSession).ToArray();
    }

    public async Task<StoredSession?> FindSessionByOwnerAsync(string id, string userId, CancellationToken cancellationToken)
    {
        var filter = _filter.And(
            _filter.Eq(IdField, ObjectId.Parse(id)),
            _filter.Eq(UserIdField, ObjectId.Parse(userId)));
        var document = await Sessions.Find(filter, _findOptions)
            .Project(_sessionProjection)
            .Limit(1)
            .FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToStoredSession(document);
    }

    public async Task<bool> MarkSessionRevokedAsync(string id, string userId, CancellationToken cancellationToken)
    {
        var filter = _filter.And(
            _filter.Eq(IdField, ObjectId.Parse(id)),
            _filter.Eq(UserIdField, ObjectId.Parse(userId)));
        var result = await Sessions.UpdateOneAsync(
            filter,
            Builders<BsonDocument>.Update.Set(RevokedField, true),
            cancellationToken: cancellationToken);
        return result.MatchedCount == 1;
    }

    private async Task RevokeOldestBeyondTheLimitAsync(IClientSessionHandle transaction, ObjectId userId, CancellationToken cancellationToken)
    {
        var now = new BsonDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var open = _filter.And(
            _filter.Eq(UserIdField, userId),
            _filter.Eq(RevokedField, false),
            _filter.Gt(ExpiresAtField, now));
        var openIds = await Sessions.Find(transaction, open, _findOptions)
            .Project(Builders<BsonDocument>.Projection.Include(IdField))
            .Sort(Builders<BsonDocument>.Sort.Ascending(CreatedAtField).Ascending(IdField))
            .ToListAsync(cancellationToken);
        var excess = openIds.Count - NewSession.MaxOpenPerAccount;
        if (excess <= 0)
        {
            return;
        }

        var oldest = openIds.Take(excess).Select(document => document[IdField].AsObjectId).ToList();
        await Sessions.UpdateManyAsync(
            transaction,
            _filter.In(IdField, oldest),
            Builders<BsonDocument>.Update.Set(RevokedField, true),
            cancellationToken: cancellationToken);
    }

    public async Task<UserAccount?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(userId, out var id))
        {
            return null;
        }

        return ToAccount(await Users.Find(_filter.Eq(IdField, id), _findOptions).Project(_accountProjection).Limit(1)
            .FirstOrDefaultAsync(cancellationToken));
    }

    public async Task<StoredSession?> FindSessionByFingerprintAsync(byte[] fingerprint, CancellationToken cancellationToken) =>
        ToSession(await Sessions.Find(_filter.Eq(FingerprintField, new BsonBinaryData(fingerprint)), _findOptions).Project(_sessionProjection).Limit(1)
            .FirstOrDefaultAsync(cancellationToken));

    public async Task<StoredSession?> FindSessionByUsedFingerprintAsync(byte[] fingerprint, CancellationToken cancellationToken) =>
        ToSession(await Sessions.Find(_filter.AnyEq(UsedFingerprintsField, new BsonBinaryData(fingerprint)), _findOptions).Project(_sessionProjection).Limit(1)
            .FirstOrDefaultAsync(cancellationToken));

    /// <summary>Un solo <c>updateOne</c> con la huella vigente en el filtro: MongoDB lo ejecuta de forma atómica sobre el documento.</summary>
    public async Task<bool> RotateAsync(
        string sessionId, byte[] currentFingerprint, byte[] newFingerprint, DateTime now, DateTime newExpiresAt, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(currentFingerprint);
        ArgumentNullException.ThrowIfNull(newFingerprint);
        if (!ObjectId.TryParse(sessionId, out var id))
        {
            return false;
        }

        var filter = _filter.And(
            _filter.Eq(IdField, id),
            _filter.Eq(FingerprintField, new BsonBinaryData(currentFingerprint)),
            _filter.Eq(RevokedField, false),
            _filter.Gt(ExpiresAtField, new BsonDateTime(now)));
        var update = Builders<BsonDocument>.Update
            .Set(FingerprintField, new BsonBinaryData(newFingerprint))
            .Set(LastActivityField, new BsonDateTime(now))
            .Set(ExpiresAtField, new BsonDateTime(newExpiresAt))
            .PushEach(UsedFingerprintsField, [new BsonBinaryData(currentFingerprint)], slice: -MaxUsedFingerprints);
        var result = await Sessions.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    public async Task<IReadOnlyList<string>> RevokeAllAsync(string userId, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(userId, out var id))
        {
            return [];
        }

        var open = _filter.And(_filter.Eq(UserIdField, id), _filter.Eq(RevokedField, false));
        var ids = (await Sessions.Find(open, _findOptions).Project(Builders<BsonDocument>.Projection.Include(IdField)).ToListAsync(cancellationToken))
            .Select(document => document[IdField].AsObjectId)
            .ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        await Sessions.UpdateManyAsync(
            _filter.And(_filter.In(IdField, ids), _filter.Eq(RevokedField, false)),
            Builders<BsonDocument>.Update.Set(RevokedField, true),
            cancellationToken: cancellationToken);
        return [.. ids.Select(objectId => objectId.ToString())];
    }

    public async Task<bool> RevokeAsync(string sessionId, string sub, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(sessionId, out var id))
        {
            return false;
        }

        var result = await Sessions.UpdateOneAsync(
            _filter.And(_filter.Eq(IdField, id), _filter.Eq(SubField, sub)),
            Builders<BsonDocument>.Update.Set(RevokedField, true),
            cancellationToken: cancellationToken);
        return result.MatchedCount == 1;
    }

    internal static bool IsDuplicateKey(Exception exception) => Knock.Identity.Api.Hosting.MongoWriteErrors.IsDuplicateKey(exception);

    private static BsonDocument UserDocument(NewAccount account, ObjectId userId)
    {
        var acceptedAt = new BsonDateTime(account.CreatedAt);
        return new BsonDocument
        {
            { IdField, userId },
            { SubField, account.Sub },
            { PhoneCiphertextField, new BsonBinaryData(account.PhoneCiphertext) },
            { PhoneCiphertextVersionField, account.PhoneCiphertextVersion },
            { PhoneHashField, new BsonBinaryData(account.PhoneHash) },
            { PhoneHashVersionField, account.PhoneHashVersion },
            { StateField, AccountState.Active },
            { IsTechnicianField, false },
            { ProfileCompleteField, false },
            {
                ConsentsField,
                new BsonArray
                {
                    ConsentDocument(ConsentDocuments.PrivacyNotice, account.PrivacyNoticeVersion, acceptedAt, account.Session.DeviceId),
                    ConsentDocument(ConsentDocuments.Terms, account.TermsVersion, acceptedAt, account.Session.DeviceId),
                }
            },
            { CreatedAtField, acceptedAt },
            { VersionField, 1 },
        };
    }

    private static BsonDocument ConsentDocument(string document, string version, BsonDateTime acceptedAt, string deviceId) => new()
    {
        { DocumentField, document },
        { VersionField, version },
        { AcceptedAtField, acceptedAt },
        { DeviceIdField, deviceId },
    };

    private static BsonDocument SessionDocument(NewSession session) => new()
    {
        { IdField, ObjectId.Parse(session.Id) },
        { UserIdField, ObjectId.Parse(session.UserId) },
        { SubField, session.Sub },
        { DeviceIdField, session.DeviceId },
        { DeviceNameField, session.DeviceName },
        { FingerprintField, new BsonBinaryData(session.Fingerprint) },
        { UsedFingerprintsField, new BsonArray() },
        { CreatedAtField, new BsonDateTime(session.CreatedAt) },
        { LastActivityField, new BsonDateTime(session.CreatedAt) },
        { ExpiresAtField, new BsonDateTime(session.ExpiresAt) },
        { RevokedField, false },
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

    private static StoredSession? ToSession(BsonDocument? document) =>
        document is null
            ? null
            : new StoredSession(
                document[IdField].AsObjectId.ToString(),
                document[UserIdField].AsObjectId.ToString(),
                document[SubField].AsString,
                document[DeviceNameField].AsString,
                document[LastActivityField].ToUniversalTime(),
                document[RevokedField].AsBoolean,
                document[ExpiresAtField].ToUniversalTime());

    private static StoredSession ToStoredSession(BsonDocument document) =>
        new(
            document[IdField].AsObjectId.ToString(),
            document[UserIdField].AsObjectId.ToString(),
            document[SubField].AsString,
            document[DeviceNameField].AsString,
            document[LastActivityField].ToUniversalTime(),
            document[RevokedField].AsBoolean,
            document[ExpiresAtField].ToUniversalTime());
}
