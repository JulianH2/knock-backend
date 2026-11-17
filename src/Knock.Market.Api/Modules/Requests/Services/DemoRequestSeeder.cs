using System.Globalization;
using Knock.Market.Api.Modules.Requests.Data;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Knock.Market.Api.Modules.Requests.Services;

/// <summary>
/// Carga las solicitudes y cotizaciones de demostración (KNOCK-246, spec 020 T-13) desde <c>db/seeds/demo/market</c>. Solo corre con <c>Seed:Demo</c> encendido y fuera de Producción, solo
/// contra MongoDB local y solo si el archivo completo es válido: un documento inválido no deja nada escrito. Las fechas de los archivos son texto ISO y se guardan como fecha.
/// </summary>
internal sealed class DemoRequestSeeder(DemoRequestStore store, IConfiguration configuration, IHostEnvironment environment)
{
    private static readonly string[] _requestFields =
    [
        "_id", "clienteId", "oficioClave", "modalidad", "descripcion", "fotos", "dia", "franja", "coloniaId", "coloniaNombre", "coloniaCp", "estado", "creadoEn", "actualizadoEn", "venceEn", "version", "demo",
    ];

    private static readonly string[] _quoteFields =
    [
        "_id", "solicitudId", "tecnicoId", "tecnicoUsuarioId", "tipoPrecio", "precio", "precioMin", "precioMax", "tope", "requiereAprobacion", "dia", "hora", "nota", "estado", "creadoEn", "actualizadoEn", "version", "demo",
    ];

    private static readonly string[] _dateFields = ["creadoEn", "actualizadoEn", "venceEn"];

    public async Task SeedAsync(string requestsPath, string quotesPath, CancellationToken cancellationToken)
    {
        if (!DemoTechnicianStartup.ShouldRun(configuration.GetValue<bool>("Seed:Demo"), environment.EnvironmentName))
        {
            throw new InvalidOperationException("Demo seed is disabled or the environment is Production.");
        }

        await store.ValidateAsync(cancellationToken);
        var requests = (await ReadAsync(requestsPath, cancellationToken)).ToArray();
        var quotes = (await ReadAsync(quotesPath, cancellationToken)).ToArray();
        Validate(requests, quotes);
        foreach (var document in requests.Concat(quotes))
        {
            ConvertDates(document);
        }

        await store.SaveAsync(requests, quotes, cancellationToken);
    }

    /// <summary>Valida cada solicitud y cotización y sus referencias. Lanza <see cref="InvalidOperationException"/> sin repetir ningún valor del archivo.</summary>
    internal static void Validate(BsonDocument[] requests, BsonDocument[] quotes)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(quotes);
        var requestIds = new HashSet<ObjectId>();
        foreach (var request in requests)
        {
            Check(IsValidRequest(request) && requestIds.Add(request["_id"].AsObjectId));
        }

        var ids = new HashSet<ObjectId>();
        var pairs = new HashSet<(ObjectId, ObjectId)>();
        foreach (var quote in quotes)
        {
            Check(IsValidQuote(quote) && requestIds.Contains(quote["solicitudId"].AsObjectId) && ids.Add(quote["_id"].AsObjectId)
                && pairs.Add((quote["solicitudId"].AsObjectId, quote["tecnicoId"].AsObjectId)));
        }

        Check(requestIds.All(id => quotes.Count(q => q["solicitudId"].AsObjectId == id) <= 5));
    }

    private static bool IsValidRequest(BsonDocument d) =>
        OnlyFields(d, _requestFields, _requestFields)
        && d["_id"].IsObjectId
        && IsGuid(d["clienteId"])
        && Text(d, "oficioClave") is "ele" or "mec" or "plo"
        && Text(d, "modalidad") is "domicilio" or "taller"
        && Text(d, "descripcion") is { Length: >= 8 and <= 500 } description && description.StartsWith("Problema ficticio", StringComparison.Ordinal)
        && d["fotos"] is BsonArray { Count: >= 1 and <= 5 } photos && photos.All(x => x.IsString && ObjectId.TryParse(x.AsString, out _))
        && Day(d, "dia")
        && Text(d, "franja") is "manana" or "tarde" or "noche"
        && ObjectId.TryParse(Text(d, "coloniaId"), out _)
        && !string.IsNullOrWhiteSpace(Text(d, "coloniaNombre"))
        && Text(d, "coloniaCp") is { Length: 5 } postal && postal.All(char.IsAsciiDigit)
        && Text(d, "estado") == "cotizando"
        && Date(d, "creadoEn") is { } created && Date(d, "actualizadoEn") is not null && Date(d, "venceEn") is { } expires
        && expires - created == TimeSpan.FromHours(48)
        && IsDemo(d);

    private static bool IsValidQuote(BsonDocument d)
    {
        var type = Text(d, "tipoPrecio");
        var hasPrice = d.Contains("precio");
        var hasRange = d.Contains("precioMin") || d.Contains("precioMax");
        var shape = type switch
        {
            "fijo" or "visita" => hasPrice && !hasRange && Amount(d, "precio") is > 0,
            "rango" => !hasPrice && Amount(d, "precioMin") is { } min && Amount(d, "precioMax") is { } max && min > 0 && min < max,
            _ => false,
        };
        return shape
            && OnlyFields(d, _quoteFields, ["_id", "solicitudId", "tecnicoId", "tecnicoUsuarioId", "tipoPrecio", "tope", "requiereAprobacion", "dia", "hora", "estado", "creadoEn", "actualizadoEn", "version", "demo"])
            && d["_id"].IsObjectId && d["solicitudId"].IsObjectId && d["tecnicoId"].IsObjectId
            && IsGuid(d["tecnicoUsuarioId"])
            && Amount(d, "tope") is > 0
            && d["requiereAprobacion"].IsBoolean
            && Day(d, "dia")
            && TimeOnly.TryParseExact(Text(d, "hora"), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
            && (!d.Contains("nota") || (d["nota"].IsString && d["nota"].AsString.Length <= 300))
            && Text(d, "estado") == "enviada"
            && Date(d, "creadoEn") is not null && Date(d, "actualizadoEn") is not null
            && IsDemo(d);
    }

    private static bool OnlyFields(BsonDocument d, string[] allowed, string[] required) =>
        d.Names.All(name => allowed.Contains(name, StringComparer.Ordinal)) && required.All(d.Contains);

    private static bool IsDemo(BsonDocument d) => d["demo"] == BsonBoolean.True && d["version"] == 1;

    private static bool IsGuid(BsonValue value) => value.IsString && Guid.TryParseExact(value.AsString, "D", out _);

    private static string? Text(BsonDocument d, string name) => d.TryGetValue(name, out var value) && value.IsString ? value.AsString : null;

    private static int? Amount(BsonDocument d, string name) => d.TryGetValue(name, out var value) && value.IsInt32 ? value.AsInt32 : null;

    private static bool Day(BsonDocument d, string name) =>
        DateOnly.TryParseExact(Text(d, name), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    private static DateTime? Date(BsonDocument d, string name) =>
        DateTime.TryParse(Text(d, name), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var value) ? value : null;

    private static void ConvertDates(BsonDocument document)
    {
        foreach (var name in _dateFields.Where(document.Contains))
        {
            document[name] = new BsonDateTime(Date(document, name)!.Value);
        }
    }

    private static void Check(bool valid)
    {
        if (!valid)
        {
            throw new InvalidOperationException("Invalid demo request seed.");
        }
    }

    private static async Task<IEnumerable<BsonDocument>> ReadAsync(string path, CancellationToken cancellationToken) =>
        BsonSerializer.Deserialize<BsonArray>(await File.ReadAllTextAsync(path, cancellationToken)).Select(x => x.AsBsonDocument);
}
