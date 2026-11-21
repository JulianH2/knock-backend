using Knock.Identity.Api.Hosting;
using Knock.Identity.Api.Modules.Trust.Domain;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Identity.Api.Modules.Trust.Data;

/// <summary>La cuenta del <c>sub</c>: su <c>_id</c> y su estado (<c>activa</c> o <c>eliminacion_programada</c>).</summary>
internal sealed record VerificationAccount(ObjectId UserId, string State);

/// <summary>Una foto guardada en la verificación: tipo y estado; la ruta interna del blob nunca sale de aquí.</summary>
internal sealed record StoredPhoto(string Type, string State);

/// <summary>Lo que el servicio necesita de una verificación guardada.</summary>
internal sealed record StoredVerification(
    ObjectId Id,
    string Level,
    string State,
    string? GrantedLevel,
    string? Reason,
    IReadOnlyList<StoredPhoto> Photos,
    DateTime CreatedAt,
    DateTime? SentAt,
    DateTime? DecidedAt,
    DateTime? ExpiresAt);

/// <summary>Una verificación nueva y su constancia de consentimiento biométrico (R-09).</summary>
internal sealed record NewVerification(
    ObjectId Id,
    string Sub,
    string Level,
    DateTime CreatedAt,
    DateTime CaptureExpiresAt,
    string ConsentText,
    string ConsentVersion,
    string DeviceId);

/// <summary>Lo que la verificación necesita de MongoDB; existe para probar el servicio con dobles sin base de datos.</summary>
internal interface IVerificationStore
{
    Task<VerificationAccount?> FindAccountAsync(string sub, CancellationToken cancellationToken);

    /// <summary>El <c>deviceId</c> de la sesión <paramref name="sessionId"/> de esa cuenta, si está abierta (R-09: sale de la sesión, no del cuerpo).</summary>
    Task<string?> FindSessionDeviceAsync(ObjectId userId, string sessionId, CancellationToken cancellationToken);

    /// <summary>La verificación más reciente de la persona (R-14), o <see langword="null"/> si nunca hubo.</summary>
    Task<StoredVerification?> FindLatestAsync(string sub, CancellationToken cancellationToken);

    /// <summary>
    /// En una transacción: cancela la captura viva de la persona (R-07: una sola por persona), guarda la verificación nueva en <c>en_captura</c> y su consentimiento.
    /// </summary>
    Task CreateAsync(NewVerification verification, CancellationToken cancellationToken);
}

/// <summary>
/// Colecciones <c>verificaciones</c> y <c>consentimientos</c> (spec 025, §6): es el único punto del módulo con MongoDB. Lee <c>usuarios</c> y <c>sesiones</c> solo para el
/// estado de la cuenta y el dispositivo de la sesión, como hace Addresses con <c>usuarios</c>. La CURP no se guarda: sin la llave del HMAC (T-03) no hay
/// <c>curpHmac</c> y <c>curpVigente</c> queda en falso.
/// </summary>
internal sealed class VerificationStore(
    [FromKeyedServices(DataExtensions.DatabaseKey)] IMongoDatabase database,
    IOptions<KnockMongoOptions> mongoOptions) : IVerificationStore
{
    public const string VerificationsCollection = "verificaciones";
    public const string ConsentsCollection = "consentimientos";
    private const string _idField = "_id";
    private const string _userIdField = "usuarioId";
    private const string _stateField = "estado";
    private const string _createdAtField = "creadoEn";

    private static readonly FilterDefinitionBuilder<BsonDocument> _filter = Builders<BsonDocument>.Filter;

    private IMongoCollection<BsonDocument> Verifications => database.GetCollection<BsonDocument>(VerificationsCollection);

    private IMongoCollection<BsonDocument> Consents => database.GetCollection<BsonDocument>(ConsentsCollection);

    private FindOptions FindOptions => new() { MaxTime = mongoOptions.Value.QueryMaxTime };

    /// <summary>Índices de §6; idempotente. Coincide con <c>db/025-verificaciones.js</c>; lo usan las pruebas.</summary>
    public async Task EnsureIndexesAsync(CancellationToken cancellationToken)
    {
        var keys = Builders<BsonDocument>.IndexKeys;
        await Verifications.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<BsonDocument>(keys.Ascending(_userIdField).Descending(_createdAtField), new CreateIndexOptions { Name = "usuarioId_1_creadoEn_-1" }),
                new CreateIndexModel<BsonDocument>(keys.Ascending(_stateField).Ascending("expiraEn"), new CreateIndexOptions { Name = "estado_1_expiraEn_1" }),
                new CreateIndexModel<BsonDocument>(keys.Ascending("curpHmac"), new CreateIndexOptions<BsonDocument>
                {
                    Name = "curpHmac_1_vigente",
                    Unique = true,
                    PartialFilterExpression = _filter.Eq("curpVigente", true),
                }),
            ],
            cancellationToken);
        await Consents.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<BsonDocument>(keys.Ascending(_userIdField).Descending("aceptadoEn"), new CreateIndexOptions { Name = "usuarioId_1_aceptadoEn_-1" }),
                new CreateIndexModel<BsonDocument>(keys.Ascending("verificacionId"), new CreateIndexOptions { Name = "verificacionId_1", Unique = true }),
            ],
            cancellationToken);
    }

    public async Task<VerificationAccount?> FindAccountAsync(string sub, CancellationToken cancellationToken)
    {
        var document = await database.GetCollection<BsonDocument>("usuarios").Find(_filter.Eq("sub", sub), FindOptions)
            .Project(Builders<BsonDocument>.Projection.Include(_idField).Include(_stateField)).Limit(1).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : new VerificationAccount(document[_idField].AsObjectId, Text(document, _stateField) ?? string.Empty);
    }

    public async Task<string?> FindSessionDeviceAsync(ObjectId userId, string sessionId, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(sessionId, out var id))
        {
            return null;
        }

        var filter = _filter.Eq(_idField, id) & _filter.Eq(_userIdField, userId) & _filter.Ne("revocada", true);
        var document = await database.GetCollection<BsonDocument>("sesiones").Find(filter, FindOptions)
            .Project(Builders<BsonDocument>.Projection.Include("deviceId")).Limit(1).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : Text(document, "deviceId");
    }

    public async Task<StoredVerification?> FindLatestAsync(string sub, CancellationToken cancellationToken)
    {
        var document = await Verifications.Find(_filter.Eq(_userIdField, sub), FindOptions)
            .Sort(Builders<BsonDocument>.Sort.Descending(_createdAtField).Descending(_idField))
            .Project(Builders<BsonDocument>.Projection.Exclude("curpHmac").Exclude("llaveEnvuelta"))
            .Limit(1).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToVerification(document);
    }

    public async Task CreateAsync(NewVerification verification, CancellationToken cancellationToken)
    {
        using var session = await database.Client.StartSessionAsync(cancellationToken: cancellationToken);
        await session.WithTransactionAsync(
            async (transaction, token) =>
            {
                var living = _filter.Eq(_userIdField, verification.Sub) & _filter.Eq(_stateField, VerificationStates.Capturing);
                await Verifications.UpdateManyAsync(transaction, living, Builders<BsonDocument>.Update
                    .Set(_stateField, VerificationStates.NotVerified)
                    .Set("motivo", VerificationReasons.CaptureReplaced)
                    .Set("fotos.$[].estado", PhotoStates.Deleted), cancellationToken: token);
                await Verifications.InsertOneAsync(transaction, ToDocument(verification), cancellationToken: token);
                await Consents.InsertOneAsync(transaction, new BsonDocument
                {
                    [_userIdField] = verification.Sub,
                    ["tipo"] = "biometria",
                    ["texto"] = verification.ConsentText,
                    ["version"] = verification.ConsentVersion,
                    ["aceptadoEn"] = verification.CreatedAt,
                    ["dispositivoId"] = verification.DeviceId,
                    ["verificacionId"] = verification.Id,
                }, cancellationToken: token);
                return true;
            },
            cancellationToken: cancellationToken);
    }

    private static BsonDocument ToDocument(NewVerification v) => new()
    {
        [_idField] = v.Id,
        [_userIdField] = v.Sub,
        ["nivel"] = v.Level,
        [_stateField] = VerificationStates.Capturing,
        ["fotos"] = new BsonArray(VerificationLevels.PhotosFor(v.Level).Select(type => new BsonDocument { ["tipo"] = type, [_stateField] = PhotoStates.Pending })),
        ["curpVigente"] = false,
        [_createdAtField] = v.CreatedAt,
        ["capturaVenceEn"] = v.CaptureExpiresAt,
    };

    private static StoredVerification ToVerification(BsonDocument d) => new(
        d[_idField].AsObjectId,
        Text(d, "nivel") ?? VerificationLevels.Basic,
        Text(d, _stateField) ?? VerificationStates.NotVerified,
        Text(d, "nivelOtorgado"),
        Text(d, "motivo"),
        d.TryGetValue("fotos", out var photos) && photos.IsBsonArray
            ? [.. photos.AsBsonArray.Where(p => p.IsBsonDocument).Select(p => new StoredPhoto(Text(p.AsBsonDocument, "tipo") ?? string.Empty,
                Text(p.AsBsonDocument, _stateField) ?? PhotoStates.Pending))]
            : [],
        Date(d, _createdAtField) ?? DateTime.MinValue,
        Date(d, "enviadoEn"),
        Date(d, "decididoEn"),
        Date(d, "expiraEn"));

    private static string? Text(BsonDocument d, string name) => d.TryGetValue(name, out var v) && v.IsString ? v.AsString : null;

    private static DateTime? Date(BsonDocument d, string name) => d.TryGetValue(name, out var v) && v.IsValidDateTime ? v.ToUniversalTime() : null;
}
