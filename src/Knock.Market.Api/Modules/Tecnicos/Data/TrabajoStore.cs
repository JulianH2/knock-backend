using Knock.Market.Api.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Market.Api.Modules.Tecnicos.Data;

internal sealed record TrabajoVerification(string? ErrorCode, string? Estado, string? Nivel);

internal sealed class TrabajoStore([FromKeyedServices(TecnicosModuleExtensions.ModuleKey)] IMongoDatabase database, IOptions<KnockMongoOptions> options)
{
    private static readonly string[] _upcomingStates = ["confirmada", "en_curso"];

    public async Task<BsonDocument?> ReadTechnicianAsync(string sub, CancellationToken cancellationToken) =>
        await database.GetCollection<BsonDocument>("tecnicos")
            .Find(Builders<BsonDocument>.Filter.Eq("usuarioId", sub), new FindOptions { MaxTime = options.Value.QueryMaxTime })
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<BsonDocument>> ReadUpcomingAsync(string sub, CancellationToken cancellationToken)
    {
        var filter = Builders<BsonDocument>.Filter;
        var visits = await database.GetCollection<BsonDocument>("visitas")
            .Find(filter.Eq("tecnicoUsuarioId", sub) & filter.In("estado", _upcomingStates), new FindOptions { MaxTime = options.Value.QueryMaxTime })
            .Sort(Builders<BsonDocument>.Sort.Ascending("dia").Ascending("hora").Ascending("_id"))
            .Limit(20).ToListAsync(cancellationToken);
        return visits;
    }

    public async Task<IReadOnlyList<BsonDocument>> ReadQuotesAsync(string sub, CancellationToken cancellationToken) =>
        await database.GetCollection<BsonDocument>("cotizaciones")
            .Find(Builders<BsonDocument>.Filter.Eq("tecnicoUsuarioId", sub), new FindOptions { MaxTime = options.Value.QueryMaxTime })
            .Sort(Builders<BsonDocument>.Sort.Descending("creadoEn").Descending("_id"))
            .Limit(20).ToListAsync(cancellationToken);

    public Task<long> CountFinishedAsync(string sub, CancellationToken cancellationToken)
    {
        var filter = Builders<BsonDocument>.Filter;
        return database.GetCollection<BsonDocument>("visitas").CountDocumentsAsync(
            filter.Eq("tecnicoUsuarioId", sub) & filter.Eq("estado", "terminada"),
            new CountOptions { MaxTime = options.Value.QueryMaxTime }, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<ObjectId, BsonDocument>> ReadRequestsAsync(
        IEnumerable<ObjectId> ids, CancellationToken cancellationToken)
    {
        var requests = await database.GetCollection<BsonDocument>("solicitudes")
            .Find(Builders<BsonDocument>.Filter.In("_id", ids.Distinct()), new FindOptions { MaxTime = options.Value.QueryMaxTime })
            .Project(Builders<BsonDocument>.Projection.Include("oficioNombre").Include("oficioClave").Include("coloniaNombre").Include("direccionCifrada"))
            .ToListAsync(cancellationToken);
        return requests.ToDictionary(x => x["_id"].AsObjectId);
    }
    public async Task<IReadOnlyList<BsonDocument>> ReadFinishedAsync(
        string sub, (DateTime Date, ObjectId Id)? after, int limit, CancellationToken cancellationToken)
    {
        var match = new BsonDocument { ["tecnicoUsuarioId"] = sub, ["estado"] = "terminada" };
        var pipeline = database.GetCollection<BsonDocument>("visitas").Aggregate(new AggregateOptions { MaxTime = options.Value.QueryMaxTime })
            .Match(match)
            .AppendStage<BsonDocument>(new BsonDocument("$addFields", new BsonDocument("fechaTrabajo",
                new BsonDocument("$ifNull", new BsonArray { "$terminadaEn", "$actualizadoEn" }))));
        if (after is { } cursor)
        {
            var filter = Builders<BsonDocument>.Filter;
            pipeline = pipeline.Match(filter.Lt("fechaTrabajo", cursor.Date)
                | (filter.Eq("fechaTrabajo", cursor.Date) & filter.Lt("_id", cursor.Id)));
        }

        return await pipeline.Sort(Builders<BsonDocument>.Sort.Descending("fechaTrabajo").Descending("_id"))
            .Limit(limit + 1).ToListAsync(cancellationToken);
    }

    public async Task<TrabajoVerification?> ReadVerificationAsync(string sub, CancellationToken cancellationToken)
    {
        try
        {
            var filter = Builders<BsonDocument>.Filter.Eq("usuarioId", sub);
            var projection = Builders<BsonDocument>.Projection.Include("nivelVerificacion");
            var document = await database.GetCollection<BsonDocument>("tecnicos")
                .Find(filter, new FindOptions { MaxTime = options.Value.QueryMaxTime })
                .Project(projection).FirstOrDefaultAsync(cancellationToken);
            if (document is null) return null;
            var level = document.GetValue("nivelVerificacion", BsonNull.Value);
            var verified = level.IsString && level.AsString is "basica" or "completa";
            return new TrabajoVerification(null, verified ? "verificada" : "sin_verificar", null);
        }
        catch (Exception ex) when (ex is MongoException or TimeoutException)
        {
            return new TrabajoVerification("unavailable", null, null);
        }
    }
}
