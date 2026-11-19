using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Tecnicos.Domain;
using Knock.Market.Api.Modules.Tecnicos.Services;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Market.Api.Modules.Tecnicos.Data;

/// <summary>
/// Perfiles de la colección <c>tecnicos</c> (spec 046, §6). Usa los nombres de campo que ya leen la elegibilidad de cotizar (<c>coloniaBaseId</c>,
/// <c>ubicacionBase</c>, <c>radioAtencionKm</c>, <c>estado</c>, <c>oculto</c>) y la elección de cotización (<c>tallerDireccion</c>, <c>nivelVerificacion</c>).
/// </summary>
internal sealed class TechnicianProfileStore(
    [FromKeyedServices(TecnicosModuleExtensions.ModuleKey)] IMongoDatabase database,
    IOptions<KnockMongoOptions> options) : ITechnicianProfileStore
{
    public const string CollectionName = "tecnicos";

    private IMongoCollection<BsonDocument> Technicians => database.GetCollection<BsonDocument>(CollectionName);

    public async Task<bool> ExistsAsync(string sub, CancellationToken cancellationToken) =>
        await Technicians.Find(Builders<BsonDocument>.Filter.Eq("usuarioId", sub), new FindOptions { MaxTime = options.Value.QueryMaxTime })
            .Project(Builders<BsonDocument>.Projection.Include("_id")).FirstOrDefaultAsync(cancellationToken) is not null;

    public async Task<bool> InsertAsync(NewTechnician technician, CancellationToken cancellationToken)
    {
        try
        {
            await Technicians.InsertOneAsync(ToDocument(technician), cancellationToken: cancellationToken);
            return true;
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    internal static BsonDocument ToDocument(NewTechnician t)
    {
        var document = new BsonDocument
        {
            ["_id"] = t.Id,
            ["usuarioId"] = t.Sub,
            ["nombre"] = t.Nombre,
            ["oficios"] = new BsonArray(t.Oficios),
            ["atencion"] = t.Atencion,
            ["coloniaBaseId"] = t.ColoniaId,
            ["estado"] = TechnicianRules.ActiveState,
            ["oculto"] = false,
            ["nivelVerificacion"] = TechnicianRules.NoVerification,
            ["disponibleHasta"] = t.CreadoEn,
            ["creadoEn"] = t.CreadoEn,
            ["version"] = 1,
        };
        Add(document, "otroOficio", t.OtroOficio);
        Add(document, "tallerDireccion", t.TallerDireccionCifrada);
        if (t.RadioKm is { } radius) document["radioAtencionKm"] = radius;
        if (t.Centro is { } center)
        {
            document["ubicacionBase"] = new BsonDocument { ["type"] = "Point", ["coordinates"] = new BsonArray { center.Longitude, center.Latitude } };
        }

        Add(document, "nombreNegocio", t.Perfil?.NombreNegocio);
        Add(document, "descripcion", t.Perfil?.Descripcion);
        if (t.Perfil?.Anios is { } years) document["anios"] = years;
        return document;
    }

    private static void Add(BsonDocument document, string name, string? value)
    {
        if (value is not null) document[name] = value;
    }
}
