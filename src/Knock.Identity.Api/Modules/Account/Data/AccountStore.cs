using Knock.Identity.Api.Hosting;
using Knock.Identity.Api.Modules.Account.Domain;
using Knock.Identity.Api.Modules.Sesiones.Data;
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
    IOptions<KnockMongoOptions> mongoOptions) : IAccountStore
{
    public const string RecoveriesCollection = "recuperaciones";

    public const string DeleteAtField = "eliminarEn";
    public const string UpdatedAtField = "actualizadoEn";

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
