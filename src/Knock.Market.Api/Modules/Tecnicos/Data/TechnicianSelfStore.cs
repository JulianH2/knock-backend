using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Tecnicos.Domain;
using Knock.Market.Api.Modules.Tecnicos.Dtos;
using Knock.Market.Api.Modules.Tecnicos.Services;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Market.Api.Modules.Tecnicos.Data;

/// <summary>
/// Lecturas y escrituras del perfil propio en <c>tecnicos</c> (spec 046, TEC-08 a TEC-11). Cada operación filtra por <c>usuarioId</c> = <c>sub</c> del token; nunca
/// por un identificador que llegue en el cuerpo (R-50).
/// </summary>
internal sealed class TechnicianSelfStore(
    [FromKeyedServices(TecnicosModuleExtensions.ModuleKey)] IMongoDatabase database,
    IOptions<KnockMongoOptions> options,
    TimeProvider clock) : ITechnicianSelfStore
{
    private IMongoCollection<BsonDocument> Technicians => database.GetCollection<BsonDocument>(TechnicianProfileStore.CollectionName);

    private FindOptions Find => new() { MaxTime = options.Value.QueryMaxTime };

    private static FilterDefinition<BsonDocument> Owner(string sub) => Builders<BsonDocument>.Filter.Eq("usuarioId", sub);

    public async Task<OwnTechnician?> FindOwnAsync(string sub, CancellationToken cancellationToken)
    {
        var projection = Builders<BsonDocument>.Projection.Include("atencion").Include("tallerDireccion").Include("radioAtencionKm");
        var document = await Technicians.Find(Owner(sub), Find).Project(projection).FirstOrDefaultAsync(cancellationToken);
        if (document is null) return null;
        var radius = document.GetValue("radioAtencionKm", BsonNull.Value);
        var attention = document.GetValue("atencion", BsonNull.Value);
        return new OwnTechnician(document["_id"].AsObjectId, attention.IsString ? attention.AsString : null,
            document.TryGetValue("tallerDireccion", out var workshop) && workshop.IsString, radius.IsInt32 ? radius.AsInt32 : null);
    }

    public async Task<TechnicianResponse?> ReadOwnAsync(string sub, CancellationToken cancellationToken)
    {
        var projection = Builders<BsonDocument>.Projection.Exclude("tallerDireccion").Exclude("contactoPublico").Exclude("ubicacionBase");
        var d = await Technicians.Find(Owner(sub), Find).Project(projection).FirstOrDefaultAsync(cancellationToken);
        if (d is null) return null;
        var until = d.TryGetValue("disponibleHasta", out var available) && available.IsValidDateTime ? available.ToUniversalTime() : (DateTime?)null;
        return new TechnicianResponse(
            d["_id"].AsObjectId.ToString(),
            Text(d, "nombre") ?? "Técnico",
            d.TryGetValue("oficios", out var trades) && trades.IsBsonArray ? [.. trades.AsBsonArray.Where(v => v.IsString).Select(v => v.AsString)] : [],
            Text(d, "otroOficio"),
            Text(d, "atencion") ?? string.Empty,
            Int(d, "radioAtencionKm"),
            Text(d, "coloniaBaseId") ?? string.Empty,
            Text(d, "nombreNegocio"),
            Text(d, "descripcion"),
            Int(d, "anios"),
            Text(d, "estado") ?? string.Empty,
            Text(d, "nivelVerificacion") ?? TechnicianRules.NoVerification,
            TechnicianRules.IsAvailable(until, clock.GetUtcNow().UtcDateTime),
            d.TryGetValue("creadoEn", out var created) && created.IsValidDateTime ? created.ToUniversalTime() : DateTime.MinValue);
    }

    public async Task<ObjectId?> SetAvailableUntilAsync(string sub, DateTime untilUtc, CancellationToken cancellationToken) =>
        await UpdateOneAsync(sub, Builders<BsonDocument>.Update.Set("disponibleHasta", untilUtc), cancellationToken);

    public async Task<IReadOnlyList<DayScheduleDto>?> ReadScheduleAsync(string sub, CancellationToken cancellationToken)
    {
        var document = await Technicians.Find(Owner(sub), Find).Project(Builders<BsonDocument>.Projection.Include("horarios")).FirstOrDefaultAsync(cancellationToken);
        if (document is null) return null;
        if (!document.TryGetValue("horarios", out var stored) || !stored.IsBsonArray) return [];
        return [.. stored.AsBsonArray.Where(v => v.IsBsonDocument).Select(v => ToDay(v.AsBsonDocument))];
    }

    public async Task<ObjectId?> SetScheduleAsync(string sub, IReadOnlyList<DayScheduleDto> schedule, CancellationToken cancellationToken)
    {
        var days = new BsonArray(schedule.Select(day => new BsonDocument
        {
            ["dia"] = day.Dia,
            ["abierto"] = day.Abierto == true,
            ["franjas"] = new BsonArray((day.Franjas ?? []).Select(r => new BsonDocument { ["inicio"] = r.Inicio, ["fin"] = r.Fin })),
        }));
        return await UpdateOneAsync(sub, Builders<BsonDocument>.Update.Set("horarios", days), cancellationToken);
    }

    public async Task UpdateAsync(ObjectId id, IReadOnlyDictionary<string, BsonValue?> changes, CancellationToken cancellationToken)
    {
        var update = Builders<BsonDocument>.Update.Combine(changes.Select(change => change.Value is null
            ? Builders<BsonDocument>.Update.Unset(change.Key)
            : Builders<BsonDocument>.Update.Set(change.Key, change.Value)));
        await Technicians.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", id), update, cancellationToken: cancellationToken);
    }

    private async Task<ObjectId?> UpdateOneAsync(string sub, UpdateDefinition<BsonDocument> update, CancellationToken cancellationToken)
    {
        var updateOptions = new FindOneAndUpdateOptions<BsonDocument, BsonDocument>
        {
            Projection = Builders<BsonDocument>.Projection.Include("_id"),
            ReturnDocument = ReturnDocument.After,
        };
        var document = await Technicians.FindOneAndUpdateAsync(Owner(sub), update, updateOptions, cancellationToken);
        return document?["_id"].AsObjectId;
    }

    private static DayScheduleDto ToDay(BsonDocument d) => new(
        Text(d, "dia"),
        d.TryGetValue("abierto", out var open) && open.IsBoolean && open.AsBoolean,
        d.TryGetValue("franjas", out var ranges) && ranges.IsBsonArray
            ? [.. ranges.AsBsonArray.Where(v => v.IsBsonDocument).Select(v => new TimeRangeDto(Text(v.AsBsonDocument, "inicio"), Text(v.AsBsonDocument, "fin")))]
            : []);

    private static string? Text(BsonDocument d, string name) => d.TryGetValue(name, out var v) && v.IsString ? v.AsString : null;

    private static int? Int(BsonDocument d, string name) => d.TryGetValue(name, out var v) && v.IsInt32 ? v.AsInt32 : null;
}
