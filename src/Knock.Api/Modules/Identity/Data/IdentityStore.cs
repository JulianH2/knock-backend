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
    IOptions<KnockMongoOptions> mongoOptions)
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

    private static readonly FilterDefinitionBuilder<BsonDocument> _filter = Builders<BsonDocument>.Filter;

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
    }

    /// <summary>Guarda la huella de una sesión de una cuenta que ya existe (un canje de cuenta existente solo inserta esto).</summary>
    public Task AddRefreshTokenAsync(RefreshTokenRecord refresh, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(refresh);
        return RefreshTokens.InsertOneAsync(RefreshDocument(refresh), cancellationToken: cancellationToken);
    }

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
