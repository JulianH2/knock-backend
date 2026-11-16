using System.Globalization;
using Knock.Contracts.Catalog;
using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Requests.Domain;
using Knock.Market.Api.Modules.Requests.Services;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Market.Api.Modules.Requests.Data;

internal sealed class MongoTechnicianDirectory(
    [FromKeyedServices(RequestsModuleExtensions.ModuleKey)] IMongoDatabase database,
    IOptions<KnockMongoOptions> options) : ITechnicianDirectory
{
    public async Task<Technician?> FindBySubAsync(string sub, CancellationToken cancellationToken)
        => await FindAsync(sub, FilterDefinition<BsonDocument>.Empty, cancellationToken);

    public async Task<Technician?> FindEligibleAsync(string sub, QuoteRequestSnapshot request, CancellationToken cancellationToken)
    {
        var filters = Builders<BsonDocument>.Filter;
        FilterDefinition<BsonDocument> coverage;
        if (request.Latitude is null || request.Longitude is null)
        {
            coverage = filters.Eq("coloniaBaseId", request.ColoniaId);
        }
        else
        {
            var alternatives = Enumerable.Range(1, 30).Select(radius =>
                filters.Eq("radioAtencionKm", radius) & new BsonDocument("ubicacionBase", new BsonDocument("$geoWithin",
                    new BsonDocument("$centerSphere", new BsonArray { new BsonArray { request.Longitude.Value, request.Latitude.Value }, radius / 6371.0 }))));
            coverage = filters.Or(alternatives);
        }
        return await FindAsync(sub, filters.Eq("oficios", request.Trade) & coverage, cancellationToken);
    }

    private async Task<Technician?> FindAsync(string sub, FilterDefinition<BsonDocument> eligibility, CancellationToken cancellationToken)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("usuarioId", sub)
            & Builders<BsonDocument>.Filter.Eq("estado", "activo")
            & Builders<BsonDocument>.Filter.Eq("oculto", false) & eligibility;
        var projection = Builders<BsonDocument>.Projection.Include("usuarioId").Include("estado").Include("oculto")
            .Include("oficios").Include("coloniaBaseId").Include("ubicacionBase").Include("radioAtencionKm");
        var doc = await database.GetCollection<BsonDocument>("tecnicos")
            .Find(filter, new FindOptions { MaxTime = options.Value.QueryMaxTime }).Project(projection).FirstOrDefaultAsync(cancellationToken);
        if (doc is null || !doc.TryGetValue("_id", out var technicianId) || !technicianId.IsObjectId) return null;
        var trades = doc.GetValue("oficios", new BsonArray());
        var point = doc.GetValue("ubicacionBase", BsonNull.Value);
        var coordinates = point.IsBsonDocument ? point.AsBsonDocument.GetValue("coordinates", BsonNull.Value) : BsonNull.Value;
        var validPoint = coordinates.IsBsonArray && coordinates.AsBsonArray.Count == 2 && coordinates.AsBsonArray.All(x => x.IsNumeric);
        var radius = doc.GetValue("radioAtencionKm", 0);
        var hidden = doc.GetValue("oculto", BsonNull.Value);
        return new Technician(sub, doc.GetValue("estado", "").ToString()!, !hidden.IsBoolean || hidden.AsBoolean,
            trades.IsBsonArray ? trades.AsBsonArray.Where(x => x.IsString).Select(x => x.AsString).ToArray() : [],
            doc.GetValue("coloniaBaseId", BsonNull.Value).ToString(),
            validPoint ? coordinates.AsBsonArray[1].ToDouble() : null,
            validPoint ? coordinates.AsBsonArray[0].ToDouble() : null, radius.IsInt32 ? radius.AsInt32 : 0, technicianId.AsObjectId.ToString());
    }

    public async Task<bool> ExistsAsync(string sub, CancellationToken cancellationToken) =>
        await database.GetCollection<BsonDocument>("tecnicos")
            .Find(Builders<BsonDocument>.Filter.Eq("usuarioId", sub), new FindOptions { MaxTime = options.Value.QueryMaxTime })
            .Project(Builders<BsonDocument>.Projection.Include("_id")).FirstOrDefaultAsync(cancellationToken) is not null;
}

internal sealed class MongoQuoteRequestReader(
    [FromKeyedServices(RequestsModuleExtensions.ModuleKey)] IMongoDatabase database,
    IOptions<KnockMongoOptions> options, INeighborhoodCenterCatalog catalog,
    ILogger<MongoQuoteRequestReader> logger) : IQuoteRequestReader
{
    private static readonly Action<ILogger, string, Exception?> _malformedRequest =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(1, "MalformedRequest"), "Malformed request document {RequestId}");

    public async Task<QuoteRequestSnapshot?> FindAsync(string id, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out var objectId)) return null;
        var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
        var projection = Builders<BsonDocument>.Projection.Include("oficioClave").Include("coloniaId").Include("estado").Include("venceEn").Include("dia");
        var doc = await database.GetCollection<BsonDocument>("solicitudes")
            .Find(filter, new FindOptions { MaxTime = options.Value.QueryMaxTime }).Project(projection).FirstOrDefaultAsync(cancellationToken);
        if (doc is null) return null;
        if (!TryRead(doc, out var trade, out var colonia, out var state, out var expiry, out var day))
        {
            _malformedRequest(logger, objectId.ToString(), null);
            return null;
        }
        var center = await catalog.FindNeighborhoodCenterAsync(colonia, cancellationToken);
        return new QuoteRequestSnapshot(trade, colonia, state, expiry, day, center?.Latitude, center?.Longitude);
    }

    private static bool TryRead(BsonDocument doc, out string trade, out string colonia, out RequestState state, out DateTimeOffset expiry, out DateOnly day)
    {
        trade = colonia = string.Empty;
        state = default;
        expiry = default;
        day = default;
        if (!doc.TryGetValue("oficioClave", out var t) || !t.IsString || string.IsNullOrWhiteSpace(t.AsString)
            || !doc.TryGetValue("coloniaId", out var c) || !c.IsString || string.IsNullOrWhiteSpace(c.AsString)
            || !doc.TryGetValue("estado", out var s) || !s.IsString
            || s.AsString is not ("enviada" or "cotizando" or "cita_confirmada" or "terminada" or "calificada" or "cancelada" or "vencida")
            || !doc.TryGetValue("venceEn", out var e) || !e.IsBsonDateTime || !e.AsBsonDateTime.IsValidDateTime
            || !doc.TryGetValue("dia", out var d) || !d.IsString
            || !DateOnly.TryParseExact(d.AsString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out day)) return false;
        trade = t.AsString;
        colonia = c.AsString;
        state = RequestStates.ParseRequest(s.AsString);
        expiry = new DateTimeOffset(e.ToUniversalTime());
        return true;
    }
}
