using System.ComponentModel;
using System.Text.Json.Serialization;
using Knock.Market.Api.Modules.Catalog.Domain;
using Knock.Market.Api.Modules.Catalog.Services;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Market.Api.Modules.Catalog.Dtos;

internal sealed record BusquedaQuery(IQueryCollection Values)
{
    public string? Get(string name) => Values.TryGetValue(name, out var values) && values.Count == 1 ? values[0] : null;
}

internal sealed class BusquedaQueryParameters
{
    [FromQuery(Name = "q"), Description("Obligatorio; texto normalizado de 2 a 80 caracteres.")]
    public string? Q { get; init; }

    [FromQuery(Name = "coloniaId"), Description("Obligatorio; ObjectId de la colonia activa.")]
    public string? ColoniaId { get; init; }

    [FromQuery(Name = "oficio"), Description("Clave opcional de un oficio activo.")]
    public string? Oficio { get; init; }

    [FromQuery(Name = "soloVerificados"), Description("Booleano opcional true o false; por defecto false.")]
    public string? SoloVerificados { get; init; }

    [FromQuery(Name = "cursor"), Description("Cursor opaco de la consulta y versión del diccionario.")]
    public string? Cursor { get; init; }

    [FromQuery(Name = "limit"), Description("Entero de 1 a 20; por defecto 20.")]
    public string? Limit { get; init; }
}

internal sealed record BusquedaOficio(string Clave, string Nombre);
internal sealed record BusquedaInterpretacion(string Estado,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] BusquedaOficio? OficioDetectado,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<BusquedaOficio>? OficiosSugeridos);
internal sealed record BusquedaItem(string Id, string Nombre, IReadOnlyList<BusquedaOficio> Oficios, decimal Calificacion,
    int TotalResenas, bool Verificado, string NivelVerificacion, string? ColoniaBase, int? DistanciaKm, bool AtiendeTuColonia, string? FotoUrl);
internal sealed record BusquedaResponse(BusquedaInterpretacion Interpretacion, IReadOnlyList<BusquedaItem> Items, string? NextCursor)
{
    public static BusquedaResponse From(SearchResponse response) => new(
        new BusquedaInterpretacion(response.Interpretation.State,
            response.Interpretation.Detected is { } detected ? Trade(detected) : null,
            response.Interpretation.Suggestions.Count > 0 ? response.Interpretation.Suggestions.Select(Trade).ToArray() : null),
        response.Items.Select(item => new BusquedaItem(item.Id, item.Name, item.Trades.Select(Trade).ToArray(), item.Rating,
            item.Reviews, item.Verified, item.Verification, item.BaseColonia, item.DistanceKm, item.ServesColonia, item.PhotoUrl)).ToArray(),
        response.NextCursor);

    private static BusquedaOficio Trade(SearchTrade trade) => new(trade.Key, trade.Name);
}
