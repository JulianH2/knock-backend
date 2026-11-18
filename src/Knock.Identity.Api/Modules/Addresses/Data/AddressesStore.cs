using Knock.Identity.Api.Hosting;
using Knock.Identity.Api.Modules.Addresses.Domain;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Identity.Api.Modules.Addresses.Data;

/// <summary>Lo que las direcciones necesitan de MongoDB; existe para probar el servicio con dobles sin base de datos.</summary>
internal interface IAddressesStore
{
    /// <summary>El <c>_id</c> de la cuenta del <c>sub</c> del token, o <see langword="null"/> si no existe.</summary>
    Task<ObjectId?> FindUserIdAsync(string sub, CancellationToken cancellationToken);

    /// <summary>Las direcciones de la persona, la más reciente primero.</summary>
    Task<IReadOnlyList<StoredAddress>> ListAsync(ObjectId userId, CancellationToken cancellationToken);
}

/// <summary>
/// Colección <c>direcciones</c> de la base del servicio (spec 040, §6). Es el único punto del módulo con <c>IMongoCollection</c> (spec 002): los filtros son tipados del
/// driver y toda lectura y escritura lleva el <c>usuarioId</c> de la cuenta del token, así una dirección ajena no existe para quien la pide (R-14 y R-15). Nada se cachea
/// (R-13). Lee <c>usuarios</c> solo para convertir el <c>sub</c> del token en el <c>_id</c> de la cuenta.
/// </summary>
internal sealed class AddressesStore(
    [FromKeyedServices(DataExtensions.DatabaseKey)] IMongoDatabase database,
    IOptions<KnockMongoOptions> mongoOptions) : IAddressesStore
{
    public const string AddressesCollection = "direcciones";
    public const string UsersCollection = "usuarios";
    public const string UserIdField = "usuarioId";
    public const string StreetField = "calleNumero";
    public const string ColoniaField = "colonia";
    public const string ReferencesField = "referencias";
    public const string PrincipalField = "principal";
    public const string CreatedAtField = "creadoEn";
    private const string _idField = "_id";
    private const string _subField = "sub";

    private static readonly FilterDefinitionBuilder<BsonDocument> _filter = Builders<BsonDocument>.Filter;

    private readonly FindOptions _findOptions = new() { MaxTime = mongoOptions.Value.QueryMaxTime };

    private IMongoCollection<BsonDocument> Addresses { get; } = database.GetCollection<BsonDocument>(AddressesCollection);

    private IMongoCollection<BsonDocument> Users { get; } = database.GetCollection<BsonDocument>(UsersCollection);

    /// <summary>Índice de la spec 040 (§6). Idempotente; lo invocan las pruebas y el script <c>db/040-direcciones-del-cliente.js</c> hace lo mismo.</summary>
    public Task EnsureIndexesAsync(CancellationToken cancellationToken) =>
        Addresses.Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending(UserIdField)),
            cancellationToken: cancellationToken);

    public async Task<ObjectId?> FindUserIdAsync(string sub, CancellationToken cancellationToken)
    {
        var user = await Users.Find(_filter.Eq(_subField, sub), _findOptions)
            .Project(Builders<BsonDocument>.Projection.Include(_idField))
            .Limit(1)
            .FirstOrDefaultAsync(cancellationToken);
        return user?[_idField].AsObjectId;
    }

    public async Task<IReadOnlyList<StoredAddress>> ListAsync(ObjectId userId, CancellationToken cancellationToken)
    {
        var documents = await Addresses.Find(_filter.Eq(UserIdField, userId), _findOptions)
            .Sort(Builders<BsonDocument>.Sort.Descending(CreatedAtField).Descending(_idField))
            .ToListAsync(cancellationToken);
        return [.. documents.Select(ToStored)];
    }

    internal static StoredAddress ToStored(BsonDocument document) => new(
        document[_idField].AsObjectId,
        document[UserIdField].AsObjectId,
        document[StreetField].AsString,
        document[ColoniaField].AsString,
        document.TryGetValue(ReferencesField, out var references) && !references.IsBsonNull ? references.AsString : null,
        document[PrincipalField].AsBoolean,
        document[CreatedAtField].ToUniversalTime());
}
