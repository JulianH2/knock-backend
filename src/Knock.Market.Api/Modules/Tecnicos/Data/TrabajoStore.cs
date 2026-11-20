using Knock.Market.Api.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Market.Api.Modules.Tecnicos.Data;

internal sealed record TrabajoVerification(string? ErrorCode, string? Estado, string? Nivel);

internal sealed class TrabajoStore(IMongoDatabase database, IOptions<KnockMongoOptions> options)
{
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
