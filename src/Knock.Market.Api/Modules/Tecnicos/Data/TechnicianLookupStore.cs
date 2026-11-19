using System.Text.RegularExpressions;
using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Tecnicos.Domain;
using Knock.Market.Api.Modules.Tecnicos.Services;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Market.Api.Modules.Tecnicos.Data;

/// <summary>Lecturas de <c>tecnicos</c> para «llega a mi colonia» y el autocompletado (spec 046, TEC-06 y TEC-03). Nunca proyecta dirección ni contacto.</summary>
internal sealed class TechnicianLookupStore(
    [FromKeyedServices(TecnicosModuleExtensions.ModuleKey)] IMongoDatabase database,
    IOptions<KnockMongoOptions> options,
    TimeProvider clock) : ITechnicianLookupReader
{
    private IMongoCollection<BsonDocument> Technicians => database.GetCollection<BsonDocument>(TechnicianProfileStore.CollectionName);

    public async Task<TechnicianCoverage?> FindCoverageAsync(ObjectId id, CancellationToken cancellationToken)
    {
        var projection = Builders<BsonDocument>.Projection.Include("estado").Include("oculto").Include("ocultoHasta").Include("coloniaBaseId")
            .Include("atencion").Include("radioAtencionKm").Include("ubicacionBase");
        var d = await Technicians.Find(Builders<BsonDocument>.Filter.Eq("_id", id), new FindOptions { MaxTime = options.Value.QueryMaxTime })
            .Project(projection).FirstOrDefaultAsync(cancellationToken);
        return d is null ? null : ToCoverage(d);
    }

    internal static TechnicianCoverage ToCoverage(BsonDocument d)
    {
        var coordinates = d.TryGetValue("ubicacionBase", out var point) && point.IsBsonDocument ? point.AsBsonDocument.GetValue("coordinates", BsonNull.Value) : BsonNull.Value;
        var valid = coordinates.IsBsonArray && coordinates.AsBsonArray.Count == 2 && coordinates.AsBsonArray.All(v => v.IsNumeric);
        return new TechnicianCoverage(
            Text(d, "estado") ?? string.Empty,
            !d.TryGetValue("oculto", out var hidden) || !hidden.IsBoolean || hidden.AsBoolean,
            d.TryGetValue("ocultoHasta", out var until) && until.IsValidDateTime ? until.ToUniversalTime() : null,
            Text(d, "coloniaBaseId"),
            Text(d, "atencion"),
            d.TryGetValue("radioAtencionKm", out var radius) && radius.IsInt32 ? radius.AsInt32 : null,
            valid ? coordinates.AsBsonArray[1].ToDouble() : null,
            valid ? coordinates.AsBsonArray[0].ToDouble() : null);
    }

    private static string? Text(BsonDocument d, string name) => d.TryGetValue(name, out var v) && v.IsString ? v.AsString : null;

    public async Task<IReadOnlyList<BusinessName>> FindBusinessNamesAsync(string text, int limit, CancellationToken cancellationToken)
    {
        var filters = Builders<BsonDocument>.Filter;
        var now = clock.GetUtcNow().UtcDateTime;
        // El texto ya pasó el validador (letras, números y espacios) y además se escapa: nunca se interpreta como expresión regular.
        var filter = filters.Regex("nombreNegocio", new BsonRegularExpression(Regex.Escape(text), "i"))
            & filters.Eq("estado", TechnicianRules.ActiveState) & filters.Eq("oculto", false)
            & (filters.Exists("ocultoHasta", false) | filters.Lte("ocultoHasta", now));
        var documents = await Technicians.Find(filter, new FindOptions { MaxTime = options.Value.QueryMaxTime })
            .Project(Builders<BsonDocument>.Projection.Include("nombreNegocio")).Sort(Builders<BsonDocument>.Sort.Ascending("nombreNegocio"))
            .Limit(limit).ToListAsync(cancellationToken);
        return [.. documents.Where(d => d["nombreNegocio"].IsString).Select(d => new BusinessName(d["_id"].AsObjectId.ToString(), d["nombreNegocio"].AsString))];
    }
}
