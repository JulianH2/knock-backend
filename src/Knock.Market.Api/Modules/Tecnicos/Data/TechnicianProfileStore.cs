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
    IOptions<KnockMongoOptions> options) : ITechnicianProfileStore, ITechnicianProfileReader
{
    public const string CollectionName = "tecnicos";

    private IMongoCollection<BsonDocument> Technicians => database.GetCollection<BsonDocument>(CollectionName);

    public async Task<bool> ExistsAsync(string sub, CancellationToken cancellationToken) =>
        await Technicians.Find(Builders<BsonDocument>.Filter.Eq("usuarioId", sub), new FindOptions { MaxTime = options.Value.QueryMaxTime })
            .Project(Builders<BsonDocument>.Projection.Include("_id")).FirstOrDefaultAsync(cancellationToken) is not null;

    public Task<bool> InsertAsync(NewTechnician technician, CancellationToken cancellationToken) =>
        InsertOnceAsync(
            () => Technicians.InsertOneAsync(ToDocument(technician), cancellationToken: cancellationToken),
            () => ExistsAsync(technician.Sub, cancellationToken),
            delay => Task.Delay(delay, cancellationToken));

    /// <summary>
    /// Dos registros simultáneos de la misma cuenta chocan en el índice único de <c>usuarioId</c>: MongoDB responde «llave duplicada» (11000) y DocumentDB puede responder «conflicto
    /// de escritura» (112) o «interbloqueo» (24). En todos los casos se relee el perfil por <c>usuarioId</c>: si existe, el otro registro ganó (CA-69, <see langword="false"/>); si no, se reintenta con espera
    /// creciente y, agotados los intentos, se relanza la excepción.
    /// </summary>
    internal static async Task<bool> InsertOnceAsync(Func<Task> insert, Func<Task<bool>> exists, Func<TimeSpan, Task> wait)
    {
        var attempt = 0;
        while (true)
        {
            attempt++;
            try
            {
                await insert();
                return true;
            }
            catch (MongoException exception) when (IsConflict(exception))
            {
                if (await exists())
                {
                    return false;
                }

                if (attempt >= _maxWriteConflictAttempts)
                {
                    throw;
                }

                await wait(TimeSpan.FromMilliseconds(_backoffMilliseconds * attempt));
            }
        }
    }

    private static bool IsConflict(MongoException exception) =>
        exception is MongoWriteException { WriteError.Category: ServerErrorCategory.DuplicateKey }
        or MongoWriteException { WriteError.Code: _writeConflictCode }
        or MongoCommandException { Code: _duplicateKeyCode or _writeConflictCode }
        || IsDeadlock(exception);

    private static bool IsDeadlock(Exception exception) =>
        exception is MongoCommandException { Code: _lockErrorCode } command && command.Message.Contains("deadlock", StringComparison.OrdinalIgnoreCase);

    private const int _duplicateKeyCode = 11000;

    private const int _writeConflictCode = 112;

    /// <summary>Código 24 de documentdb-local («Could not acquire lock ... due to deadlock»): la escritura no se aplicó. Solo cuenta si el mensaje dice «deadlock».</summary>
    private const int _lockErrorCode = 24;

    private const int _maxWriteConflictAttempts = 6;

    private const int _backoffMilliseconds = 50;

    /// <summary>Campos del perfil público: nunca se proyectan <c>tallerDireccion</c>, <c>contactoPublico</c>, <c>ubicacionBase</c> ni <c>usuarioId</c>.</summary>
    private static readonly string[] _profileFields =
    [
        "nombre", "nombreNegocio", "fotoUrl", "nivelVerificacion", "calificacion", "anios", "descripcion", "oficios", "atencion", "radioAtencionKm",
        "coloniaBaseId", "estado", "oculto", "ocultoHasta", "disponibleHasta",
    ];

    public async Task<TechnicianProfileRecord?> FindByIdAsync(ObjectId id, CancellationToken cancellationToken)
    {
        var projection = Builders<BsonDocument>.Projection.Combine(_profileFields.Select(f => Builders<BsonDocument>.Projection.Include(f)));
        var document = await Technicians.Find(Builders<BsonDocument>.Filter.Eq("_id", id), new FindOptions { MaxTime = options.Value.QueryMaxTime })
            .Project(projection).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToProfile(document);
    }

    internal static TechnicianProfileRecord ToProfile(BsonDocument d) => new(
        d["_id"].AsObjectId,
        Text(d, "nombre") ?? "Técnico",
        Text(d, "nombreNegocio"),
        Text(d, "fotoUrl"),
        Text(d, "nivelVerificacion") ?? TechnicianRules.NoVerification,
        d.TryGetValue("calificacion", out var rating) && rating.IsNumeric ? rating.ToDouble() : null,
        Int(d, "anios"),
        Text(d, "descripcion"),
        d.TryGetValue("oficios", out var trades) && trades.IsBsonArray ? [.. trades.AsBsonArray.Where(v => v.IsString).Select(v => v.AsString)] : [],
        Text(d, "atencion"),
        Int(d, "radioAtencionKm"),
        Text(d, "coloniaBaseId"),
        Text(d, "estado") ?? string.Empty,
        !d.TryGetValue("oculto", out var hidden) || !hidden.IsBoolean || hidden.AsBoolean,
        Date(d, "ocultoHasta"),
        Date(d, "disponibleHasta"));

    private static string? Text(BsonDocument d, string name) => d.TryGetValue(name, out var v) && v.IsString ? v.AsString : null;

    private static int? Int(BsonDocument d, string name) => d.TryGetValue(name, out var v) && v.IsInt32 ? v.AsInt32 : null;

    private static DateTime? Date(BsonDocument d, string name) => d.TryGetValue(name, out var v) && v.IsValidDateTime ? v.ToUniversalTime() : null;

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
