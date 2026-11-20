using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Market.Api.Modules.Requests.Data;

internal sealed class EnviosStore([FromKeyedServices(RequestsModuleExtensions.ModuleKey)] IMongoDatabase database)
{
    private IMongoCollection<BsonDocument> Collection => database.GetCollection<BsonDocument>("envios");

    public async Task<IReadOnlyList<ObjectId>> FindTechniciansAsync(ObjectId requestId, CancellationToken cancellationToken)
    {
        var documents = await Collection.Find(Builders<BsonDocument>.Filter.Eq("solicitudId", requestId))
            .Project(Builders<BsonDocument>.Projection.Include("tecnicoId")).ToListAsync(cancellationToken);
        return documents.Where(x => x.TryGetValue("tecnicoId", out var id) && id.IsObjectId)
            .Select(x => x["tecnicoId"].AsObjectId).ToArray();
    }

    public Task InsertAsync(ObjectId requestId, ObjectId technicianId, string technicianUserId, int wave, string reason,
        DateTime now, CancellationToken cancellationToken) =>
        Collection.InsertOneAsync(Document(requestId, technicianId, technicianUserId, wave, reason, now), cancellationToken: cancellationToken);

    public Task InsertAsync(IClientSessionHandle session, ObjectId requestId, ObjectId technicianId, string technicianUserId, int wave,
        string reason, DateTime now, CancellationToken cancellationToken) =>
        Collection.InsertOneAsync(session, Document(requestId, technicianId, technicianUserId, wave, reason, now), cancellationToken: cancellationToken);

    private static BsonDocument Document(ObjectId requestId, ObjectId technicianId, string technicianUserId, int wave, string reason, DateTime now) =>
        new()
        {
            ["solicitudId"] = requestId,
            ["tecnicoId"] = technicianId,
            ["tecnicoUsuarioId"] = technicianUserId,
            ["oleada"] = wave,
            ["motivo"] = reason,
            ["estado"] = "activo",
            ["enviadoEn"] = now,
            ["actualizadoEn"] = now,
        };
}
