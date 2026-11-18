using Knock.Identity.Api.Hosting;
using Knock.Identity.Api.Modules.Account.Domain;
using Knock.Identity.Api.Modules.Sesiones.Data;
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
    [FromKeyedServices(SesionesDataExtensions.ModuleKey)] IMongoDatabase database,
    IMongoClient client,
    IOptions<KnockMongoOptions> mongoOptions) : IAccountStore
{
    public const string RecoveriesCollection = "recuperaciones";

    public const string DeleteAtField = "eliminarEn";
    public const string UpdatedAtField = "actualizadoEn";

    public const string ExportRequestedAtField = "exportacionSolicitadaEn";
    public const string NewPhoneField = "celularNuevoProtegido";
    public const string RecoveryTypeField = "tipo";

    private const int _maxWriteConflictAttempts = 6;

    private static readonly FilterDefinitionBuilder<BsonDocument> _filter = Builders<BsonDocument>.Filter;

    private static readonly ProjectionDefinition<BsonDocument> _accountProjection = Builders<BsonDocument>.Projection
        .Include(SesionesStore.SubField)
        .Include(SesionesStore.StateField)
        .Include(SesionesStore.IsTechnicianField)
        .Include(SesionesStore.PhoneHashField)
        .Include(DeleteAtField);

    private readonly FindOptions _findOptions = new() { MaxTime = mongoOptions.Value.QueryMaxTime };

    public IMongoCollection<BsonDocument> Users { get; } = database.GetCollection<BsonDocument>(SesionesStore.UsersCollection);

    public IMongoCollection<BsonDocument> Sessions { get; } = database.GetCollection<BsonDocument>(SesionesStore.SessionsCollection);

    public IMongoCollection<BsonDocument> Recoveries { get; } = database.GetCollection<BsonDocument>(RecoveriesCollection);

    /// <summary>Índices de la spec 039 (§6). Es idempotente y debe coincidir con <c>db/039-cuenta-y-datos-personales.js</c>.</summary>
    public async Task EnsureIndexesAsync(CancellationToken cancellationToken)
    {
        var keys = Builders<BsonDocument>.IndexKeys;
        await Users.Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(
                keys.Ascending(SesionesStore.StateField).Ascending(DeleteAtField),
                new CreateIndexOptions { Name = "estado_1_eliminarEn_1" }),
            cancellationToken: cancellationToken);
        await Recoveries.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<BsonDocument>(
                    keys.Ascending(SesionesStore.StateField).Ascending(SesionesStore.CreatedAtField),
                    new CreateIndexOptions { Name = "estado_1_creadoEn_1" }),
                new CreateIndexModel<BsonDocument>(
                    keys.Ascending(SesionesStore.UserIdField).Ascending(SesionesStore.StateField),
                    new CreateIndexOptions { Name = "usuarioId_1_estado_1" }),
                new CreateIndexModel<BsonDocument>(
                    keys.Ascending(UpdatedAtField),
                    new CreateIndexOptions { Name = "actualizadoEn_1", ExpireAfter = TimeSpan.FromDays(90) }),
            ],
            cancellationToken);
    }

    public async Task<AccountSnapshot?> FindBySubAsync(string sub, CancellationToken cancellationToken) =>
        ToSnapshot(await Users.Find(_filter.Eq(SesionesStore.SubField, sub), _findOptions).Project(_accountProjection).Limit(1)
            .FirstOrDefaultAsync(cancellationToken));

    /// <summary>Busca por el HMAC con el índice único: no descifra nada.</summary>
    public async Task<bool> PhoneHashExistsAsync(byte[] phoneHash, CancellationToken cancellationToken) =>
        await Users.Find(_filter.Eq(SesionesStore.PhoneHashField, new BsonBinaryData(phoneHash)), _findOptions)
            .Project(Builders<BsonDocument>.Projection.Include(SesionesStore.IdField)).Limit(1).AnyAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> FindOtherOpenSessionIdsAsync(
        string userId, string currentSessionId, DateTime now, CancellationToken cancellationToken)
    {
        var documents = await Sessions.Find(OpenSessionsExcept(userId, currentSessionId, now), _findOptions)
            .Project(Builders<BsonDocument>.Projection.Include(SesionesStore.IdField))
            .ToListAsync(cancellationToken);
        return [.. documents.Select(document => document[SesionesStore.IdField].AsObjectId.ToString())];
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
                            .Set(SesionesStore.PhoneCiphertextField, new BsonBinaryData(phone.Ciphertext))
                            .Set(SesionesStore.PhoneCiphertextVersionField, phone.KeyVersion)
                            .Set(SesionesStore.PhoneHashField, new BsonBinaryData(hash.Value))
                            .Set(SesionesStore.PhoneHashVersionField, hash.KeyVersion)
                            .Set(UpdatedAtField, new BsonDateTime(now))
                            .Inc(SesionesStore.VersionField, 1);
                        await Users.UpdateOneAsync(transaction, _filter.Eq(SesionesStore.IdField, ObjectId.Parse(userId)), update, cancellationToken: token);
                        var open = OpenSessionsExcept(userId, currentSessionId, now);
                        var ids = (await Sessions.Find(transaction, open, _findOptions)
                                .Project(Builders<BsonDocument>.Projection.Include(SesionesStore.IdField))
                                .ToListAsync(token))
                            .Select(document => document[SesionesStore.IdField].AsObjectId)
                            .ToList();
                        if (ids.Count > 0)
                        {
                            await Sessions.UpdateManyAsync(
                                transaction,
                                _filter.In(SesionesStore.IdField, ids),
                                Builders<BsonDocument>.Update.Set(SesionesStore.RevokedField, true),
                                cancellationToken: token);
                        }

                        return ids.Select(id => id.ToString()).ToList();
                    },
                    cancellationToken: cancellationToken);
                return (PhoneChangeOutcome.Changed, revoked);
            }
            catch (Exception exception) when (SesionesStore.IsDuplicateKey(exception))
            {
                return (PhoneChangeOutcome.PhoneTaken, []);
            }
            catch (MongoCommandException exception) when (exception.Code == SesionesStore.WriteConflictCode && attempt < _maxWriteConflictAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50 * attempt), cancellationToken);
            }
        }
    }

    public async Task<AccountSnapshot?> FindByPhoneHashAsync(byte[] phoneHash, CancellationToken cancellationToken) =>
        ToSnapshot(await Users.Find(_filter.Eq(SesionesStore.PhoneHashField, new BsonBinaryData(phoneHash)), _findOptions)
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
            .SetOnInsert(SesionesStore.CreatedAtField, created)
            .SetOnInsert(UpdatedAtField, created);
        var open = _filter.And(
            _filter.Eq(SesionesStore.UserIdField, ObjectId.Parse(recoveryCase.UserId)),
            _filter.Eq(SesionesStore.StateField, RecoveryStates.Pending));
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
            _filter.Eq(SesionesStore.IdField, ObjectId.Parse(userId)),
            _filter.Exists(ExportRequestedAtField, false));
        var update = Builders<BsonDocument>.Update
            .Set(ExportRequestedAtField, new BsonDateTime(now))
            .Set(UpdatedAtField, new BsonDateTime(now))
            .Inc(SesionesStore.VersionField, 1);
        var result = await Users.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount == 1 ? ExportStartOutcome.Started : ExportStartOutcome.AlreadyInProgress;
    }

    public async Task ClearExportAsync(string userId, CancellationToken cancellationToken) =>
        await Users.UpdateOneAsync(
            _filter.Eq(SesionesStore.IdField, ObjectId.Parse(userId)),
            Builders<BsonDocument>.Update.Unset(ExportRequestedAtField).Inc(SesionesStore.VersionField, 1),
            cancellationToken: cancellationToken);

    private static FilterDefinition<BsonDocument> OpenSessionsExcept(string userId, string currentSessionId, DateTime now) =>
        _filter.And(
            _filter.Eq(SesionesStore.UserIdField, ObjectId.Parse(userId)),
            _filter.Eq(SesionesStore.RevokedField, false),
            _filter.Gt(SesionesStore.ExpiresAtField, new BsonDateTime(now)),
            _filter.Ne(SesionesStore.IdField, ObjectId.Parse(currentSessionId)));

    private static AccountSnapshot? ToSnapshot(BsonDocument? document)
    {
        if (document is null)
        {
            return null;
        }

        DateTime? deleteAt = document.TryGetValue(DeleteAtField, out var value) && value.IsValidDateTime ? value.ToUniversalTime() : null;
        return new AccountSnapshot(
            document[SesionesStore.IdField].AsObjectId.ToString(),
            document[SesionesStore.SubField].AsString,
            document[SesionesStore.StateField].AsString,
            document[SesionesStore.IsTechnicianField].AsBoolean,
            document[SesionesStore.PhoneHashField].AsBsonBinaryData.Bytes,
            deleteAt);
    }
}
