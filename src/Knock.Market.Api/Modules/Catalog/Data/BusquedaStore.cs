using System.Globalization;
using System.Text.RegularExpressions;
using Knock.Market.Api.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Market.Api.Modules.Catalog.Data;

internal sealed record SearchCandidate(string Id, string Name, IReadOnlyList<string> Trades, decimal Rating, int Reviews,
    string Verification, string? BaseColonia, string? PhotoUrl, bool MatchesName, bool ServesColonia, double? DistanceKm)
{
    public bool Verified => Verification is "basica" or "completa";
}

/// <summary>Acceso a candidatos y conteos; el llamador debe filtrar datos personales antes de contar.</summary>
internal sealed partial class BusquedaStore(
    [FromKeyedServices(CatalogModuleExtensions.ModuleKey)] IMongoDatabase database,
    IOptions<KnockMongoOptions> options)
{
    private static readonly TimeZoneInfo _mexicoCity = TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");
    private static readonly string[] _verifiedLevels = ["basica", "completa"];
    private static readonly string[] _fields = ["nombrePublico", "oficios", "calificacion", "totalResenas", "nivelVerificacion", "coloniaBaseId", "coloniaBaseNombre", "fotoUrl", "palabrasBusqueda", "radioAtencionKm", "ubicacionBase"];

    public async Task<IReadOnlyList<SearchCandidate>> FindAsync(string coloniaId, double? latitude, double? longitude,
        IReadOnlyList<string> words, string? trade, IReadOnlyList<string> activeTrades, bool verifiedOnly, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(words);
        ArgumentNullException.ThrowIfNull(activeTrades);
        if ((latitude is null) != (longitude is null)) throw new ArgumentException("Both coordinates are required.");
        var names = NameFilter(words);
        var eligibility = Eligibility(activeTrades, verifiedOnly);
        var results = new Dictionary<string, SearchCandidate>(StringComparer.Ordinal);
        if (latitude is not null && longitude is not null && trade is not null && activeTrades.Contains(trade, StringComparer.Ordinal))
        {
            var nearby = await NearbyAsync(latitude.Value, longitude.Value, eligibility, trade, cancellationToken);
            foreach (var doc in nearby)
            {
                var candidate = ToCandidate(doc, words, true, doc["distanceMeters"].ToDouble() / 1000);
                results[candidate.Id] = candidate;
            }
        }
        else if (latitude is null && trade is not null && activeTrades.Contains(trade, StringComparer.Ordinal))
        {
            var filter = eligibility & Builders<BsonDocument>.Filter.Eq("oficios", trade) & Builders<BsonDocument>.Filter.Eq("coloniaBaseId", coloniaId);
            foreach (var doc in await FindDocumentsAsync(filter, cancellationToken))
            {
                var candidate = ToCandidate(doc, words, true, null);
                results[candidate.Id] = candidate;
            }
        }

        if (names is not null) await AddNamesAsync(results, eligibility & names, coloniaId, latitude, longitude, words, cancellationToken);
        return results.Values.OrderBy(x => x.DistanceKm ?? double.PositiveInfinity).ThenBy(x => x.Id, StringComparer.Ordinal).ToArray();
    }

    private async Task AddNamesAsync(Dictionary<string, SearchCandidate> results, FilterDefinition<BsonDocument> filter, string coloniaId,
        double? latitude, double? longitude, IReadOnlyList<string> words, CancellationToken cancellationToken)
    {
        foreach (var doc in await FindDocumentsAsync(filter, cancellationToken))
        {
            var id = doc["_id"].ToString()!;
            if (results.ContainsKey(id)) continue;
            var distance = Distance(doc, latitude, longitude);
            var serves = latitude is null ? doc.GetValue("coloniaBaseId", "").ToString() == coloniaId
                : distance is not null && doc.GetValue("radioAtencionKm", 0).IsNumeric && distance <= doc["radioAtencionKm"].ToDouble();
            results[id] = ToCandidate(doc, words, serves, distance);
        }
    }

    /// <summary>The caller must supply a normalized term already cleared by the personal-data filter.</summary>
    public async Task CountUnmatchedAsync(string filteredTerm, DateTimeOffset now, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filteredTerm);
        var day = TimeZoneInfo.ConvertTime(now, _mexicoCity).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var filter = Builders<BsonDocument>.Filter.Eq("terminoNormalizado", filteredTerm) & Builders<BsonDocument>.Filter.Eq("dia", day);
        var update = Builders<BsonDocument>.Update.Inc("conteo", 1).SetOnInsert("creadoEn", now.UtcDateTime);
        var collection = database.GetCollection<BsonDocument>("busquedas_sin_resultado");
        try
        {
            await collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            await collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = false }, cancellationToken);
        }
    }

    private async Task<List<BsonDocument>> NearbyAsync(double latitude, double longitude, FilterDefinition<BsonDocument> eligibility, string trade, CancellationToken cancellationToken)
    {
        var collection = database.GetCollection<BsonDocument>("tecnicos");
        var query = eligibility & Builders<BsonDocument>.Filter.Eq("oficios", trade);
        var serializer = collection.DocumentSerializer;
        var filter = query.Render(new RenderArgs<BsonDocument>(serializer, collection.Settings.SerializerRegistry));
        var geo = new BsonDocument("$geoNear", new BsonDocument
        {
            ["near"] = new BsonDocument { ["type"] = "Point", ["coordinates"] = new BsonArray { longitude, latitude } },
            ["key"] = "ubicacionBase",
            ["distanceField"] = "distanceMeters",
            ["maxDistance"] = 30000,
            ["spherical"] = true,
            ["query"] = filter,
        });
        var coverage = new BsonDocument("$match", new BsonDocument("$expr", new BsonDocument("$lte", new BsonArray
        {
            "$distanceMeters", new BsonDocument("$multiply", new BsonArray { "$radioAtencionKm", 1000 }),
        })));
        var projection = ProjectionDocument();
        projection.Remove("ubicacionBase");
        projection.Remove("radioAtencionKm");
        projection["distanceMeters"] = 1;
        var pipeline = PipelineDefinition<BsonDocument, BsonDocument>.Create(geo, coverage, new BsonDocument("$project", projection));
        using var cursor = await collection.AggregateAsync(pipeline, new AggregateOptions { MaxTime = options.Value.QueryMaxTime }, cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    private async Task<List<BsonDocument>> FindDocumentsAsync(FilterDefinition<BsonDocument> filter, CancellationToken cancellationToken) =>
        await database.GetCollection<BsonDocument>("tecnicos").Find(filter, new FindOptions { MaxTime = options.Value.QueryMaxTime })
            .Project<BsonDocument>(ProjectionDocument()).ToListAsync(cancellationToken);

    private static BsonDocument ProjectionDocument() => new(_fields.Select(field => new BsonElement(field, 1)));

    private static FilterDefinition<BsonDocument> Eligibility(IReadOnlyList<string> activeTrades, bool verifiedOnly)
    {
        var f = Builders<BsonDocument>.Filter;
        var filter = f.Eq("estado", "activo") & f.Eq("oculto", false) & f.In("oficios", activeTrades);
        return verifiedOnly ? filter & f.In("nivelVerificacion", _verifiedLevels) : filter;
    }

    private static FilterDefinition<BsonDocument>? NameFilter(IReadOnlyList<string> words)
    {
        var prefixes = words.Where(word => word.Length >= 3).Distinct(StringComparer.Ordinal)
            .Select(word => Builders<BsonDocument>.Filter.Regex("palabrasBusqueda", new BsonRegularExpression("^" + Regex.Escape(word)))).ToArray();
        return prefixes.Length == 0 ? null : Builders<BsonDocument>.Filter.Or(prefixes);
    }

    private static SearchCandidate ToCandidate(BsonDocument doc, IReadOnlyList<string> words, bool serves, double? distance)
    {
        var names = doc.GetValue("palabrasBusqueda", new BsonArray()).AsBsonArray.Where(x => x.IsString).Select(x => x.AsString).ToArray();
        var matches = words.Any(word => word.Length >= 3 && names.Any(name => name.StartsWith(word, StringComparison.Ordinal)));
        var trades = doc.GetValue("oficios", new BsonArray()).AsBsonArray.Where(x => x.IsString).Select(x => x.AsString).ToArray();
        var rating = doc.GetValue("calificacion", 0);
        var reviews = doc.GetValue("totalResenas", 0);
        return new SearchCandidate(doc["_id"].ToString()!, doc.GetValue("nombrePublico", "").ToString()!, trades,
            rating.IsNumeric ? rating.ToDecimal() : 0, reviews.IsNumeric ? reviews.ToInt32() : 0,
            doc.GetValue("nivelVerificacion", "ninguno").ToString()!, doc.GetValue("coloniaBaseNombre", BsonNull.Value).IsString ? doc["coloniaBaseNombre"].AsString : null,
            doc.GetValue("fotoUrl", BsonNull.Value).IsString ? doc["fotoUrl"].AsString : null, matches, serves, distance);
    }

    private static double? Distance(BsonDocument doc, double? latitude, double? longitude)
    {
        if (latitude is null || longitude is null || !doc.TryGetValue("ubicacionBase", out var location) || !location.IsBsonDocument ||
            !location.AsBsonDocument.TryGetValue("coordinates", out var coordinates) || !coordinates.IsBsonArray || coordinates.AsBsonArray.Count != 2 ||
            !coordinates[0].IsNumeric || !coordinates[1].IsNumeric) return null;
        var radians = Math.PI / 180;
        var lat1 = latitude.Value * radians;
        var lat2 = coordinates[1].ToDouble() * radians;
        var dlat = lat2 - lat1;
        var dlon = (coordinates[0].ToDouble() - longitude.Value) * radians;
        var h = (Math.Sin(dlat / 2) * Math.Sin(dlat / 2)) + (Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dlon / 2) * Math.Sin(dlon / 2));
        return 6371.0088 * 2 * Math.Asin(Math.Sqrt(Math.Clamp(h, 0, 1)));
    }
}
