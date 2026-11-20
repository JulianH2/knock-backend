using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Market.Api.Modules.Requests.Data;

internal sealed class EnviosStore([FromKeyedServices(RequestsModuleExtensions.ModuleKey)] IMongoDatabase database)
{
    private static readonly string[] _visibleStates = ["activo", "cotizado"];
    private IMongoCollection<BsonDocument> Collection => database.GetCollection<BsonDocument>("envios");

    public async Task<IReadOnlyList<ObjectId>> FindTechniciansAsync(ObjectId requestId, CancellationToken cancellationToken)
    {
        var documents = await Collection.Find(Builders<BsonDocument>.Filter.Eq("solicitudId", requestId))
            .Project(Builders<BsonDocument>.Projection.Include("tecnicoId")).ToListAsync(cancellationToken);
        return documents.Where(x => x.TryGetValue("tecnicoId", out var id) && id.IsObjectId)
            .Select(x => x["tecnicoId"].AsObjectId).ToArray();
    }

    public async Task<bool> AnyAsync(IClientSessionHandle session, ObjectId requestId, CancellationToken cancellationToken) =>
        await Collection.Find(session, Builders<BsonDocument>.Filter.Eq("solicitudId", requestId)).AnyAsync(cancellationToken);

    public async Task<bool> CanViewAsync(ObjectId requestId, ObjectId technicianId, CancellationToken cancellationToken) =>
        await Collection.Find(Builders<BsonDocument>.Filter.Eq("solicitudId", requestId)
            & Builders<BsonDocument>.Filter.Eq("tecnicoId", technicianId)
            & Builders<BsonDocument>.Filter.In("estado", _visibleStates)).AnyAsync(cancellationToken);

    public async Task<bool> MarkQuotedAsync(IClientSessionHandle session, ObjectId requestId, ObjectId technicianId, DateTime now, CancellationToken cancellationToken)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("solicitudId", requestId)
            & Builders<BsonDocument>.Filter.Eq("tecnicoId", technicianId)
            & Builders<BsonDocument>.Filter.Eq("estado", "activo");
        var result = await Collection.UpdateOneAsync(session, filter,
            Builders<BsonDocument>.Update.Set("estado", "cotizado").Set("actualizadoEn", now), cancellationToken: cancellationToken);
        return result.MatchedCount == 1;
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
