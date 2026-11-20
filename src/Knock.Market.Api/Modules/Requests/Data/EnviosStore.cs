using Knock.Market.Api.Modules.Requests.Domain;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Market.Api.Modules.Requests.Data;

internal sealed record InboxEntry(RequestRecord Request, int Wave, DateTime ReceivedAt, ObjectId SendId);
internal sealed record InboxPage(IReadOnlyList<InboxEntry> Items, string? NextCursor);

internal sealed class EnviosStore([FromKeyedServices(RequestsModuleExtensions.ModuleKey)] IMongoDatabase database)
{
    private static readonly string[] _visibleStates = ["activo", "cotizado"];
    private IMongoCollection<BsonDocument> Collection => database.GetCollection<BsonDocument>("envios");

    public async Task<string?> DiscardAsync(ObjectId requestId, ObjectId technicianId, DateTime now, CancellationToken token)
    {
        var f = Builders<BsonDocument>.Filter;
        var pair = f.Eq("solicitudId", requestId) & f.Eq("tecnicoId", technicianId);
        var changed = await Collection.UpdateOneAsync(pair & f.Eq("estado", "activo"),
            Builders<BsonDocument>.Update.Set("estado", "descartado").Set("descartadoEn", now).Set("actualizadoEn", now),
            cancellationToken: token);
        if (changed.MatchedCount == 1) return "descartado";
        var document = await Collection.Find(pair).FirstOrDefaultAsync(token);
        return document?.GetValue("estado", BsonNull.Value).AsString;
    }

    public async Task<InboxPage> ListAsync(string sub, string? cursor, int limit, DateTime now, CancellationToken token)
    {
        var f = Builders<BsonDocument>.Filter;
        var filter = f.Eq("tecnicoUsuarioId", sub) & f.Eq("estado", "activo");
        if (cursor is not null)
        {
            var after = RequestCursor.Decode(cursor);
            filter &= f.Lt("enviadoEn", after.CreatedAtUtc) | (f.Eq("enviadoEn", after.CreatedAtUtc) & f.Lt("_id", after.Id));
        }
        var pipeline = Collection.Aggregate().Match(filter)
            .Lookup("solicitudes", "solicitudId", "_id", "solicitud")
            .Unwind<BsonDocument>("solicitud")
            .Match(new BsonDocument { ["solicitud.estado"] = new BsonDocument("$in", new BsonArray { "enviada", "cotizando" }), ["solicitud.venceEn"] = new BsonDocument("$gt", now) })
            .Sort(new BsonDocument { ["enviadoEn"] = -1, ["_id"] = -1 }).Limit(limit + 1);
        var documents = await pipeline.ToListAsync(token);
        var items = documents.Take(limit).Select(d => new InboxEntry(RequestsStore.ToRecord(d["solicitud"].AsBsonDocument),
            d["oleada"].ToInt32(), d["enviadoEn"].ToUniversalTime(), d["_id"].AsObjectId)).ToArray();
        return new InboxPage(items, documents.Count > limit ? RequestCursor.Encode(items[^1].ReceivedAt, items[^1].SendId) : null);
    }

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
