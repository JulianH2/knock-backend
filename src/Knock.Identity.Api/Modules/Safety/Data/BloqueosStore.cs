using Knock.Identity.Api.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Identity.Api.Modules.Safety.Data;

/// <summary>Un bloqueo guardado: quién bloqueó a quién (identificadores públicos) y cuándo; <see cref="RecordId"/> sirve de cursor.</summary>
internal sealed record StoredBlock(ObjectId RecordId, string BlockedId, DateTime CreatedAtUtc);

/// <summary>
/// Colección <c>bloqueos</c> (spec 043, §6). Es el único acceso a MongoDB del módulo. Las dos personas se guardan con su identificador público (el <c>sub</c>), porque el <c>_id</c> interno
/// de <c>usuarios</c> no sale de la base. Toda lectura y escritura lleva el <c>sub</c> de quien llama (R-24). El índice único (bloqueaId, bloqueadoId) impide bloquear dos veces.
/// </summary>
internal sealed class BloqueosStore(
    [FromKeyedServices(DataExtensions.DatabaseKey)] IMongoDatabase database,
    IOptions<KnockMongoOptions> mongoOptions)
{
    public const string BlocksCollection = "bloqueos";
    private const string _blockerField = "bloqueaId";
    private const string _blockedField = "bloqueadoId";
    private const string _createdField = "creadoEn";
    private const string _idField = "_id";

    private readonly FindOptions _findOptions = new() { MaxTime = mongoOptions.Value.QueryMaxTime };

    private IMongoCollection<BsonDocument> Blocks => database.GetCollection<BsonDocument>(BlocksCollection);

    /// <summary>Índices de la spec 043 (§6). Idempotente; lo usan las pruebas y el script de <c>db/</c> hace lo mismo.</summary>
    public Task EnsureIndexesAsync(CancellationToken cancellationToken) =>
        Blocks.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending(_blockerField).Ascending(_blockedField),
                    new CreateIndexOptions { Unique = true, Name = "bloqueaId_1_bloqueadoId_1" }),
                new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending(_blockedField), new CreateIndexOptions { Name = "bloqueadoId_1" }),
            ],
            cancellationToken);

    /// <summary>Guarda el bloqueo; falso si ya existía (el índice único decide también una carrera entre dos peticiones iguales; ante un conflicto de escritura relee el bloqueo y reintenta).</summary>
    public async Task<bool> AddAsync(string blockerId, string blockedId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var filters = Builders<BsonDocument>.Filter;
        return await MongoWriteErrors.InsertIfAbsentAsync(
            () => Blocks.InsertOneAsync(
                new BsonDocument
                {
                    [_idField] = ObjectId.GenerateNewId(),
                    [_blockerField] = blockerId,
                    [_blockedField] = blockedId,
                    [_createdField] = new BsonDateTime(nowUtc),
                },
                cancellationToken: cancellationToken),
            async () => await Blocks.Find(filters.Eq(_blockerField, blockerId) & filters.Eq(_blockedField, blockedId), _findOptions)
                .Project(Builders<BsonDocument>.Projection.Include(_idField)).FirstOrDefaultAsync(cancellationToken) is not null,
            delay => Task.Delay(delay, cancellationToken));
    }

    /// <summary>Quita el bloqueo de quien llama; falso si no había.</summary>
    public async Task<bool> RemoveAsync(string blockerId, string blockedId, CancellationToken cancellationToken)
    {
        var filters = Builders<BsonDocument>.Filter;
        return (await Blocks.DeleteOneAsync(filters.Eq(_blockerField, blockerId) & filters.Eq(_blockedField, blockedId), cancellationToken)).DeletedCount > 0;
    }

    /// <summary>
    /// Los bloqueos de quien llama, del más reciente al más antiguo (por <c>_id</c>, que crece con el tiempo). Pide uno de más para saber si hay otra página; el cursor es el
    /// <c>_id</c> del último elemento devuelto.
    /// </summary>
    public async Task<IReadOnlyList<StoredBlock>> ListAsync(string blockerId, ObjectId? after, int take, CancellationToken cancellationToken)
    {
        var filters = Builders<BsonDocument>.Filter;
        var filter = filters.Eq(_blockerField, blockerId);
        if (after is { } cursor)
        {
            filter &= filters.Lt(_idField, cursor);
        }

        var documents = await Blocks.Find(filter, _findOptions).Sort(Builders<BsonDocument>.Sort.Descending(_idField)).Limit(take).ToListAsync(cancellationToken);
        return [.. documents.Select(d => new StoredBlock(d[_idField].AsObjectId, d[_blockedField].AsString, d[_createdField].ToUniversalTime()))];
    }
}
