using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Requests.Domain;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Market.Api.Modules.Requests.Data;

internal sealed class CandidatosStore(
    [FromKeyedServices(RequestsModuleExtensions.ModuleKey)] IMongoDatabase database,
    IOptions<KnockMongoOptions> options)
{
    public async Task<IReadOnlyList<WaveCandidate>> FindAsync(ObjectId requestId, string clientId, string trade, string coloniaId,
        double? latitude, double? longitude, DateTime now, CancellationToken cancellationToken)
    {
        var sent = await new EnviosStore(database).FindTechniciansAsync(requestId, cancellationToken);
        var collection = database.GetCollection<BsonDocument>("tecnicos");
        var f = Builders<BsonDocument>.Filter;
        var filter = f.Eq("estado", "activo") & f.Eq("oculto", false) & f.Eq("oficios", trade)
            & f.Gt("disponibleHasta", now) & f.Type("usuarioId", BsonType.String) & f.Ne("usuarioId", clientId) & f.Nin("_id", sent);
        if ((latitude is null) != (longitude is null)) throw new ArgumentException("Both coordinates are required.");
        if (latitude is null)
        {
            var baseDocuments = await collection.Find(filter & f.Eq("coloniaBaseId", coloniaId),
                new FindOptions { MaxTime = options.Value.QueryMaxTime })
                .Project<BsonDocument>(new BsonDocument { ["calificacion"] = 1, ["trabajosTerminados"] = 1, ["usuarioId"] = 1 }).ToListAsync(cancellationToken);
            return ToCandidates(baseDocuments);
        }
        var query = filter.Render(new RenderArgs<BsonDocument>(collection.DocumentSerializer, collection.Settings.SerializerRegistry));
        var geo = new BsonDocument("$geoNear", new BsonDocument
        {
            ["near"] = new BsonDocument { ["type"] = "Point", ["coordinates"] = new BsonArray { longitude!.Value, latitude.Value } },
            ["key"] = "ubicacionBase",
            ["distanceField"] = "distanceMeters",
            ["spherical"] = true,
            ["query"] = query,
        });
        var coverage = new BsonDocument("$match", new BsonDocument("$or", new BsonArray
        {
            new BsonDocument("coloniaBaseId", coloniaId),
            new BsonDocument("$expr", new BsonDocument("$lte", new BsonArray
            {
                "$distanceMeters", new BsonDocument("$multiply", new BsonArray { "$radioAtencionKm", 1000 }),
            })),
        }));
        var project = new BsonDocument("$project", new BsonDocument
        {
            ["calificacion"] = 1,
            ["trabajosTerminados"] = 1,
            ["distanceMeters"] = 1,
            ["usuarioId"] = 1,
        });
        var pipeline = PipelineDefinition<BsonDocument, BsonDocument>.Create(geo, coverage, project);
        using var cursor = await collection.AggregateAsync(pipeline, new AggregateOptions { MaxTime = options.Value.QueryMaxTime }, cancellationToken);
        var documents = await cursor.ToListAsync(cancellationToken);
        var withoutLocation = filter & f.Eq("coloniaBaseId", coloniaId)
            & (f.Exists("ubicacionBase", false) | f.Eq("ubicacionBase", BsonNull.Value));
        documents.AddRange(await collection.Find(withoutLocation, new FindOptions { MaxTime = options.Value.QueryMaxTime })
            .Project<BsonDocument>(new BsonDocument { ["calificacion"] = 1, ["trabajosTerminados"] = 1, ["usuarioId"] = 1 }).ToListAsync(cancellationToken));
        return ToCandidates(documents);
    }

    private static WaveCandidate[] ToCandidates(IEnumerable<BsonDocument> documents) =>
        documents.Where(x => x["_id"].IsObjectId).Select(x => new WaveCandidate(x["_id"].AsObjectId.ToString(),
            x.GetValue("calificacion", 0).IsNumeric ? x["calificacion"].ToDecimal() : 0,
            x.GetValue("trabajosTerminados", 0).IsNumeric ? x["trabajosTerminados"].ToInt32() : 0,
            x.TryGetValue("distanceMeters", out var distance) && distance.IsNumeric ? distance.ToDouble() / 1000 : null,
            x["usuarioId"].AsString)).ToArray();
}
