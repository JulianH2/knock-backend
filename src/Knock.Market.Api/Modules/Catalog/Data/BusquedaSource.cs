using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Catalog.Domain;
using Knock.Market.Api.Modules.Catalog.Services;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Market.Api.Modules.Catalog.Data;

internal sealed class BusquedaSource(
    [FromKeyedServices(CatalogModuleExtensions.ModuleKey)] IMongoDatabase database,
    IOptions<KnockMongoOptions> options, CatalogStore catalog, BusquedaStore store) : IBusquedaSource
{
    public async Task<SearchContext?> GetContextAsync(string colonia, CancellationToken token)
    {
        try
        {
            if (!ObjectId.TryParse(colonia, out var id)) return new(false, null, null, 0, [], []);
            var f = Builders<BsonDocument>.Filter;
            var doc = await database.GetCollection<BsonDocument>("colonias")
                .Find(f.Eq("_id", id) & f.Eq("activo", true), new FindOptions { MaxTime = options.Value.QueryMaxTime })
                .Project<BsonDocument>(Builders<BsonDocument>.Projection.Include("centro")).FirstOrDefaultAsync(token);
            if (doc is null) return new(false, null, null, 0, [], []);
            var center = ReadCenter(doc);
            var trades = (await catalog.FindActiveOficiosAsync(token)).Select(t => new SearchTrade(t.Clave, t.Nombre)).ToArray();
            var synonyms = await database.GetCollection<BsonDocument>("sinonimos")
                .Find(f.Empty, new FindOptions { MaxTime = options.Value.QueryMaxTime }).ToListAsync(token);
            var meta = await database.GetCollection<BsonDocument>("catalogo_meta")
                .Find(f.Eq("_id", "sinonimos"), new FindOptions { MaxTime = options.Value.QueryMaxTime }).FirstOrDefaultAsync(token);
            if (meta is null || !meta.TryGetValue("version", out var version) || !version.IsInt32) return null;
            var entries = synonyms.Where(s => s.GetValue("termino", BsonNull.Value).IsString && s.GetValue("oficioClave", BsonNull.Value).IsString)
                .Select(s => new SearchSynonym(s["termino"].AsString, s["oficioClave"].AsString)).ToArray();
            return new(true, center?.Latitude, center?.Longitude, version.AsInt32, trades, entries);
        }
        catch (Exception exception) when (exception is MongoException or TimeoutException)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<RankedCandidate>?> FindPageAsync(SearchPageRequest query, SearchRank? after, int limit, CancellationToken token)
    {
        try { return await store.FindPageAsync(query, after, limit, token); }
        catch (Exception exception) when (exception is MongoException or TimeoutException) { return null; }
    }

    public async Task<bool> CountAsync(string term, DateTimeOffset now, CancellationToken token)
    {
        try { await store.CountUnmatchedAsync(term, now, token); return true; }
        catch (Exception exception) when (exception is MongoException or TimeoutException) { return false; }
    }

    private static (double Latitude, double Longitude)? ReadCenter(BsonDocument doc)
    {
        if (!doc.TryGetValue("centro", out var center) || !center.IsBsonDocument ||
            !center.AsBsonDocument.TryGetValue("coordinates", out var coordinates) || !coordinates.IsBsonArray ||
            coordinates.AsBsonArray.Count != 2 || !coordinates[0].IsNumeric || !coordinates[1].IsNumeric) return null;
        return (coordinates[1].ToDouble(), coordinates[0].ToDouble());
    }
}
