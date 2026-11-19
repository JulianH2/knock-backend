using Knock.Market.Api.Modules.Catalog.Domain;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Market.Api.Modules.Catalog.Data;

internal sealed record SearchPageRequest(string ColoniaId, double? Latitude, double? Longitude, IReadOnlyList<string> Words,
    string? Trade, IReadOnlyList<string> ActiveTrades, bool VerifiedOnly);
internal sealed record RankedCandidate(SearchCandidate Candidate, SearchRank Rank);

internal sealed partial class BusquedaStore
{
    public async Task<IReadOnlyList<RankedCandidate>> FindPageAsync(SearchPageRequest request, SearchRank? after, int limit, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (limit is < 1 or > 21) throw new ArgumentOutOfRangeException(nameof(limit));
        if ((request.Latitude is null) != (request.Longitude is null)) throw new ArgumentException("Both coordinates are required.");
        var result = new List<RankedCandidate>();
        for (var group = after?.Group ?? 0; group <= 2 && result.Count < limit; group++)
        {
            var filter = GroupFilter(request, group);
            if (filter is null) continue;
            var documents = await ReadGroupAsync(request, group, filter, after?.Group == group ? after : null, limit - result.Count, cancellationToken);
            foreach (var doc in documents)
            {
                var distance = request.Latitude is null ? (double?)null : doc["sortDistance"].ToDouble();
                var candidate = ToCandidate(doc, request.Words, group != 2, distance);
                result.Add(new RankedCandidate(candidate, new SearchRank(candidate.Id, group, candidate.Verified, candidate.Rating, distance)));
            }
        }
        return result;
    }

    private static FilterDefinition<BsonDocument>? GroupFilter(SearchPageRequest request, int group)
    {
        var f = Builders<BsonDocument>.Filter;
        var filter = Eligibility(request.ActiveTrades, request.VerifiedOnly);
        var names = NameFilter(request.Words);
        if (group is 0 or 2) return names is null ? null : filter & names;
        if (request.Trade is null || !request.ActiveTrades.Contains(request.Trade, StringComparer.Ordinal)) return null;
        filter &= f.Eq("oficios", request.Trade);
        return names is null ? filter : filter & f.Not(names);
    }

    private async Task<List<BsonDocument>> ReadGroupAsync(SearchPageRequest request, int group, FilterDefinition<BsonDocument> filter,
        SearchRank? after, int limit, CancellationToken cancellationToken)
    {
        var collection = database.GetCollection<BsonDocument>("tecnicos");
        var rendered = filter.Render(new RenderArgs<BsonDocument>(collection.DocumentSerializer, collection.Settings.SerializerRegistry));
        var pipeline = new List<BsonDocument>();
        if (request.Latitude is not null)
        {
            var geo = new BsonDocument
            {
                ["near"] = new BsonDocument { ["type"] = "Point", ["coordinates"] = new BsonArray { request.Longitude!.Value, request.Latitude.Value } },
                ["key"] = "ubicacionBase",
                ["distanceField"] = "distanceMeters",
                ["spherical"] = true,
                ["query"] = rendered
            };
            if (group != 2) geo["maxDistance"] = 30000;
            pipeline.Add(new BsonDocument("$geoNear", geo));
            var op = group == 2 ? "$gt" : "$lte";
            pipeline.Add(new BsonDocument("$match", new BsonDocument("$expr", new BsonDocument(op,
                new BsonArray { "$distanceMeters", new BsonDocument("$multiply", new BsonArray { "$radioAtencionKm", 1000 }) }))));
        }
        else
        {
            var f = Builders<BsonDocument>.Filter;
            var coverage = group == 2 ? f.Ne("coloniaBaseId", request.ColoniaId) : f.Eq("coloniaBaseId", request.ColoniaId);
            rendered = (filter & coverage).Render(new RenderArgs<BsonDocument>(collection.DocumentSerializer, collection.Settings.SerializerRegistry));
            pipeline.Add(new BsonDocument("$match", rendered));
        }
        pipeline.Add(new BsonDocument("$addFields", new BsonDocument
        {
            ["sortVerified"] = new BsonDocument("$cond", new BsonArray { new BsonDocument("$in", new BsonArray { "$nivelVerificacion", new BsonArray { "basica", "completa" } }), 1, 0 }),
            ["sortRating"] = new BsonDocument("$ifNull", new BsonArray { "$calificacion", 0 }),
            ["sortDistance"] = request.Latitude is null ? new BsonDouble(1e100) : new BsonDocument("$divide", new BsonArray { "$distanceMeters", 1000 }),
        }));
        if (after is not null) pipeline.Add(new BsonDocument("$match", Seek(after)));
        pipeline.Add(new BsonDocument("$sort", new BsonDocument { ["sortVerified"] = -1, ["sortRating"] = -1, ["sortDistance"] = 1, ["_id"] = 1 }));
        pipeline.Add(new BsonDocument("$limit", limit));
        var projection = ProjectionDocument();
        projection["sortDistance"] = 1;
        pipeline.Add(new BsonDocument("$project", projection));
        using var cursor = await collection.AggregateAsync(PipelineDefinition<BsonDocument, BsonDocument>.Create(pipeline), new AggregateOptions { MaxTime = options.Value.QueryMaxTime }, cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    private static BsonDocument Seek(SearchRank after)
    {
        var verified = after.Verified ? 1 : 0;
        var rating = new BsonDecimal128(after.Rating);
        var distance = after.DistanceKm ?? 1e100;
        var id = ObjectId.Parse(after.Id);
        return new BsonDocument("$or", new BsonArray
        {
            new BsonDocument("sortVerified", new BsonDocument("$lt", verified)),
            new BsonDocument { ["sortVerified"] = verified, ["sortRating"] = new BsonDocument("$lt", rating) },
            new BsonDocument { ["sortVerified"] = verified, ["sortRating"] = rating, ["sortDistance"] = new BsonDocument("$gt", distance) },
            new BsonDocument { ["sortVerified"] = verified, ["sortRating"] = rating, ["sortDistance"] = distance, ["_id"] = new BsonDocument("$gt", id) },
        });
    }
}
