using Knock.Identity.Api.Hosting;
using Knock.Identity.Api.Modules.Account.Domain;
using Knock.Identity.Api.Modules.Sesiones.Services;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Identity.Api.Modules.Account.Data;

/// <summary>
/// Colecciones <c>usuarios</c>, <c>sesiones</c> y <c>recuperaciones</c> de la base del servicio para la spec 039. Los filtros son tipados del driver y
/// la persona es siempre la del <c>sub</c> del token (R-27): ninguna entrada se arma como JSON. Ninguna colección se cachea.
/// </summary>
internal sealed class AccountStore(
    [FromKeyedServices(DataExtensions.DatabaseKey)] IMongoDatabase database,
    IMongoClient client,
    IOptions<KnockMongoOptions> mongoOptions) : IAccountStore
{
    public const string UsersCollection = "usuarios";
    public const string SessionsCollection = "sesiones";
    private const int _writeConflictCode = 112;

    public const string RecoveriesCollection = "recuperaciones";

    public const string DeleteAtField = "eliminarEn";
    public const string UpdatedAtField = "actualizadoEn";

    public const string ActiveField = "vigente";
    public const string ExportRequestedAtField = "exportacionSolicitadaEn";
    public const string NewPhoneField = "celularNuevoProtegido";
    public const string RecoveryTypeField = "tipo";

    private const int _maxWriteConflictAttempts = 6;
    private const int _maxConsentAttempts = 5;

    private static readonly FilterDefinitionBuilder<BsonDocument> _filter = Builders<BsonDocument>.Filter;

    private static readonly ProjectionDefinition<BsonDocument> _accountProjection = Builders<BsonDocument>.Projection
        .Include(Fields.SubField)
        .Include(Fields.StateField)
        .Include(Fields.IsTechnicianField)
        .Include(Fields.PhoneHashField)
        .Include(DeleteAtField);

    private readonly FindOptions _findOptions = new() { MaxTime = mongoOptions.Value.QueryMaxTime };

    public IMongoCollection<BsonDocument> Users { get; } = database.GetCollection<BsonDocument>(UsersCollection);

    public IMongoCollection<BsonDocument> Sessions { get; } = database.GetCollection<BsonDocument>(SessionsCollection);

    public IMongoCollection<BsonDocument> Recoveries { get; } = database.GetCollection<BsonDocument>(RecoveriesCollection);

    /// <summary>Índices de la spec 039 (§6). Es idempotente y debe coincidir con <c>db/039-cuenta-y-datos-personales.js</c>.</summary>
    public async Task EnsureIndexesAsync(CancellationToken cancellationToken)
    {
        var keys = Builders<BsonDocument>.IndexKeys;
        await Users.Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(
                keys.Ascending(Fields.StateField).Ascending(DeleteAtField),
                new CreateIndexOptions { Name = "estado_1_eliminarEn_1" }),
            cancellationToken: cancellationToken);
        await Recoveries.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<BsonDocument>(
                    keys.Ascending(Fields.StateField).Ascending(Fields.CreatedAtField),
                    new CreateIndexOptions { Name = "estado_1_creadoEn_1" }),
                new CreateIndexModel<BsonDocument>(
                    keys.Ascending(Fields.UserIdField).Ascending(Fields.StateField),
                    new CreateIndexOptions { Name = "usuarioId_1_estado_1" }),
                new CreateIndexModel<BsonDocument>(
                    keys.Ascending(UpdatedAtField),
                    new CreateIndexOptions { Name = "actualizadoEn_1", ExpireAfter = TimeSpan.FromDays(90) }),
            ],
            cancellationToken);
    }

    public async Task<AccountSnapshot?> FindBySubAsync(string sub, CancellationToken cancellationToken) =>
        ToSnapshot(await Users.Find(_filter.Eq(Fields.SubField, sub), _findOptions).Project(_accountProjection).Limit(1)
            .FirstOrDefaultAsync(cancellationToken));

    /// <summary>Busca por el HMAC con el índice único: no descifra nada.</summary>
    public async Task<bool> PhoneHashExistsAsync(byte[] phoneHash, CancellationToken cancellationToken) =>
        await Users.Find(_filter.Eq(Fields.PhoneHashField, new BsonBinaryData(phoneHash)), _findOptions)
            .Project(Builders<BsonDocument>.Projection.Include(Fields.IdField)).Limit(1).AnyAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> FindOtherOpenSessionIdsAsync(
        string userId, string currentSessionId, DateTime now, CancellationToken cancellationToken)
    {
        var documents = await Sessions.Find(OpenSessionsExcept(userId, currentSessionId, now), _findOptions)
            .Project(Builders<BsonDocument>.Projection.Include(Fields.IdField))
            .ToListAsync(cancellationToken);
        return [.. documents.Select(document => document[Fields.IdField].AsObjectId.ToString())];
    }

    public async Task<(PhoneChangeOutcome Outcome, IReadOnlyList<string> RevokedSessionIds)> ChangePhoneAsync(
        string userId, ProtectedPhone phone, PhoneHash hash, string currentSessionId, DateTime now, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(phone);
        ArgumentNullException.ThrowIfNull(hash);
        var attempt = 0;
        while (true)
        {
            attempt++;
            try
            {
                using var session = await client.StartSessionAsync(cancellationToken: cancellationToken);
                var revoked = await session.WithTransactionAsync(
                    async (transaction, token) =>
                    {
                        var update = Builders<BsonDocument>.Update
                            .Set(Fields.PhoneCiphertextField, new BsonBinaryData(phone.Ciphertext))
                            .Set(Fields.PhoneCiphertextVersionField, phone.KeyVersion)
                            .Set(Fields.PhoneHashField, new BsonBinaryData(hash.Value))
                            .Set(Fields.PhoneHashVersionField, hash.KeyVersion)
                            .Set(UpdatedAtField, new BsonDateTime(now))
                            .Inc(Fields.VersionField, 1);
                        await Users.UpdateOneAsync(transaction, _filter.Eq(Fields.IdField, ObjectId.Parse(userId)), update, cancellationToken: token);
                        var open = OpenSessionsExcept(userId, currentSessionId, now);
                        var ids = (await Sessions.Find(transaction, open, _findOptions)
                                .Project(Builders<BsonDocument>.Projection.Include(Fields.IdField))
                                .ToListAsync(token))
                            .Select(document => document[Fields.IdField].AsObjectId)
                            .ToList();
                        if (ids.Count > 0)
                        {
                            await Sessions.UpdateManyAsync(
                                transaction,
                                _filter.In(Fields.IdField, ids),
                                Builders<BsonDocument>.Update.Set(Fields.RevokedField, true),
                                cancellationToken: token);
                        }

                        return ids.Select(id => id.ToString()).ToList();
                    },
                    cancellationToken: cancellationToken);
                return (PhoneChangeOutcome.Changed, revoked);
            }
            catch (Exception exception) when (IsDuplicateKey(exception))
            {
                return (PhoneChangeOutcome.PhoneTaken, []);
            }
            catch (MongoCommandException exception) when (exception.Code == _writeConflictCode && attempt < _maxWriteConflictAttempts)
            {
                await Task.Delay(WriteConflictBackoff(attempt), cancellationToken);
            }
        }
    }

    public async Task<AccountSnapshot?> FindByPhoneHashAsync(byte[] phoneHash, CancellationToken cancellationToken) =>
        ToSnapshot(await Users.Find(_filter.Eq(Fields.PhoneHashField, new BsonBinaryData(phoneHash)), _findOptions)
            .Project(_accountProjection).Limit(1).FirstOrDefaultAsync(cancellationToken));

    public async Task<bool> CreateRecoveryCaseIfNoneOpenAsync(RecoveryCase recoveryCase, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(recoveryCase);
        var created = new BsonDateTime(recoveryCase.CreatedAt);
        var update = Builders<BsonDocument>.Update
            .SetOnInsert(NewPhoneField, new BsonDocument
            {
                { "cifrado", new BsonBinaryData(recoveryCase.NewPhone.Ciphertext) },
                { "cifradoVersion", recoveryCase.NewPhone.KeyVersion },
                { "hash", new BsonBinaryData(recoveryCase.NewPhoneHash.Value) },
                { "hashVersion", recoveryCase.NewPhoneHash.KeyVersion },
            })
            .SetOnInsert(RecoveryTypeField, recoveryCase.Type)
            .SetOnInsert(Fields.CreatedAtField, created)
            .SetOnInsert(UpdatedAtField, created);
        var open = _filter.And(
            _filter.Eq(Fields.UserIdField, ObjectId.Parse(recoveryCase.UserId)),
            _filter.Eq(Fields.StateField, RecoveryStates.Pending));
        // Con upsert, los campos del filtro por igualdad (usuarioId y estado) pasan al documento nuevo.
        var before = await Recoveries.FindOneAndUpdateAsync(
            open,
            update,
            new FindOneAndUpdateOptions<BsonDocument> { IsUpsert = true, ReturnDocument = ReturnDocument.Before, MaxTime = mongoOptions.Value.QueryMaxTime },
            cancellationToken);
        return before is null;
    }

    public async Task<ExportStartOutcome> TryStartExportAsync(string userId, DateTime now, CancellationToken cancellationToken)
    {
        var filter = _filter.And(
            _filter.Eq(Fields.IdField, ObjectId.Parse(userId)),
            _filter.Exists(ExportRequestedAtField, false));
        var update = Builders<BsonDocument>.Update
            .Set(ExportRequestedAtField, new BsonDateTime(now))
            .Set(UpdatedAtField, new BsonDateTime(now))
            .Inc(Fields.VersionField, 1);
        var result = await Users.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount == 1 ? ExportStartOutcome.Started : ExportStartOutcome.AlreadyInProgress;
    }

    public async Task ClearExportAsync(string userId, CancellationToken cancellationToken) =>
        await Users.UpdateOneAsync(
            _filter.Eq(Fields.IdField, ObjectId.Parse(userId)),
            Builders<BsonDocument>.Update.Unset(ExportRequestedAtField).Inc(Fields.VersionField, 1),
            cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<StoredConsent>?> FindConsentsAsync(string sub, CancellationToken cancellationToken)
    {
        var document = await Users.Find(_filter.Eq(Fields.SubField, sub), _findOptions)
            .Project(Builders<BsonDocument>.Projection.Include(Fields.ConsentsField)).Limit(1).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ReadConsents(document);
    }

    public async Task<StoredConsent?> SetOptionalConsentAsync(
        string sub, string type, bool active, string version, DateTime now, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < _maxConsentAttempts; attempt++)
        {
            var document = await Users.Find(_filter.Eq(Fields.SubField, sub), _findOptions)
                .Project(Builders<BsonDocument>.Projection.Include(Fields.ConsentsField).Include(Fields.VersionField))
                .Limit(1).FirstOrDefaultAsync(cancellationToken);
            if (document is null)
            {
                return null;
            }

            var current = document.TryGetValue(Fields.ConsentsField, out var array) && array.IsBsonArray ? array.AsBsonArray : [];
            var entry = new BsonDocument
            {
                { Fields.DocumentField, type },
                { Fields.VersionField, version },
                { ActiveField, active },
                { Fields.AcceptedAtField, new BsonDateTime(now) },
            };
            var updated = new BsonArray(current.Where(item => item.AsBsonDocument[Fields.DocumentField].AsString != type)) { entry };
            var version0 = document[Fields.VersionField];
            var filter = _filter.And(_filter.Eq(Fields.SubField, sub), _filter.Eq(Fields.VersionField, version0));
            var update = Builders<BsonDocument>.Update
                .Set(Fields.ConsentsField, updated)
                .Set(UpdatedAtField, new BsonDateTime(now))
                .Inc(Fields.VersionField, 1);
            if ((await Users.UpdateOneAsync(filter, update, cancellationToken: cancellationToken)).ModifiedCount == 1)
            {
                return new StoredConsent(type, version, now, active);
            }
        }

        throw new Common.Errors.DomainException(Common.Errors.DomainErrorKind.Conflict, "The consents were changed concurrently; try again.");
    }

    private static List<StoredConsent> ReadConsents(BsonDocument document) =>
        document.TryGetValue(Fields.ConsentsField, out var array) && array.IsBsonArray
            ? [.. array.AsBsonArray.Select(item => item.AsBsonDocument).Select(consent => new StoredConsent(
                consent[Fields.DocumentField].AsString,
                consent[Fields.VersionField].AsString,
                consent[Fields.AcceptedAtField].ToUniversalTime(),
                consent.TryGetValue(ActiveField, out var active) && active.IsBoolean ? active.AsBoolean : null))]
            : [];

    public async Task<DateTime?> FindSessionStartAsync(string sessionId, string userId, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(sessionId, out var id))
        {
            return null;
        }

        var filter = _filter.And(_filter.Eq(Fields.IdField, id), _filter.Eq(Fields.UserIdField, ObjectId.Parse(userId)));
        var document = await Sessions.Find(filter, _findOptions).Project(Builders<BsonDocument>.Projection.Include(Fields.CreatedAtField))
            .Limit(1).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : document[Fields.CreatedAtField].ToUniversalTime();
    }

    public async Task<bool> ScheduleDeletionAsync(string userId, DateTime deleteAt, DateTime now, CancellationToken cancellationToken)
    {
        var filter = _filter.And(
            _filter.Eq(Fields.IdField, ObjectId.Parse(userId)),
            _filter.Eq(Fields.StateField, AccountStates.Active));
        var update = Builders<BsonDocument>.Update
            .Set(Fields.StateField, AccountStates.DeletionScheduled)
            .Set(DeleteAtField, new BsonDateTime(deleteAt))
            .Set(UpdatedAtField, new BsonDateTime(now))
            .Inc(Fields.VersionField, 1);
        return (await Users.UpdateOneAsync(filter, update, cancellationToken: cancellationToken)).ModifiedCount == 1;
    }

    public async Task<bool> CancelDeletionAsync(string userId, DateTime now, CancellationToken cancellationToken)
    {
        var filter = _filter.And(
            _filter.Eq(Fields.IdField, ObjectId.Parse(userId)),
            _filter.Eq(Fields.StateField, AccountStates.DeletionScheduled),
            _filter.Gt(DeleteAtField, new BsonDateTime(now)));
        var update = Builders<BsonDocument>.Update
            .Set(Fields.StateField, AccountStates.Active)
            .Unset(DeleteAtField)
            .Set(UpdatedAtField, new BsonDateTime(now))
            .Inc(Fields.VersionField, 1);
        return (await Users.UpdateOneAsync(filter, update, cancellationToken: cancellationToken)).ModifiedCount == 1;
    }

    private static FilterDefinition<BsonDocument> OpenSessionsExcept(string userId, string currentSessionId, DateTime now) =>
        _filter.And(
            _filter.Eq(Fields.UserIdField, ObjectId.Parse(userId)),
            _filter.Eq(Fields.RevokedField, false),
            _filter.Gt(Fields.ExpiresAtField, new BsonDateTime(now)),
            _filter.Ne(Fields.IdField, ObjectId.Parse(currentSessionId)));

    private static AccountSnapshot? ToSnapshot(BsonDocument? document)
    {
        if (document is null)
        {
            return null;
        }

        DateTime? deleteAt = document.TryGetValue(DeleteAtField, out var value) && value.IsValidDateTime ? value.ToUniversalTime() : null;
        return new AccountSnapshot(
            document[Fields.IdField].AsObjectId.ToString(),
            document[Fields.SubField].AsString,
            document[Fields.StateField].AsString,
            document[Fields.IsTechnicianField].AsBoolean,
            document[Fields.PhoneHashField].AsBsonBinaryData.Bytes,
            deleteAt);
    }

    /// <summary>Espera técnica y corta entre reintentos de un conflicto de escritura; no es un plazo de negocio.</summary>
    private static TimeSpan WriteConflictBackoff(int attempt) => TimeSpan.FromMilliseconds(50 * attempt);

    /// <summary>Clave duplicada (11000) de una escritura simple o de una transacción.</summary>
    private static bool IsDuplicateKey(Exception exception) =>
        exception is MongoWriteException { WriteError.Category: ServerErrorCategory.DuplicateKey }
        or MongoCommandException { Code: 11000 };

    /// <summary>Nombres de los campos de <c>usuarios</c> y <c>sesiones</c> que usa este módulo (iguales a los de Sesiones).</summary>
    private static class Fields
    {
        public const string IdField = "_id";
        public const string SubField = "sub";
        public const string PhoneCiphertextField = "celularCifrado";
        public const string PhoneCiphertextVersionField = "celularCifradoVersion";
        public const string PhoneHashField = "celularHash";
        public const string PhoneHashVersionField = "celularHashVersion";
        public const string StateField = "estado";
        public const string IsTechnicianField = "esTecnico";
        public const string ConsentsField = "consentimientos";
        public const string DocumentField = "documento";
        public const string VersionField = "version";
        public const string AcceptedAtField = "aceptadoEn";
        public const string CreatedAtField = "creadoEn";
        public const string UserIdField = "usuarioId";
        public const string ExpiresAtField = "venceEn";
        public const string RevokedField = "revocada";
    }
}
