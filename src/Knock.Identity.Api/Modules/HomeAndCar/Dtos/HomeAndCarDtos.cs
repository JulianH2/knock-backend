using Microsoft.AspNetCore.Mvc;

namespace Knock.Identity.Api.Modules.HomeAndCar.Dtos;

/// <summary>
/// Cuerpo de <c>POST /me/casa-y-auto</c> (spec 041, R-03). Solo estos tres campos: el identificador de otra persona u otro campo del JSON no existe aquí, así que se ignora (R-20).
/// Los campos son anulables a propósito: el validador responde 422 por campo.
/// </summary>
/// <param name="Tipo"><c>auto</c>, <c>electrodomestico</c>, <c>instalacion</c> u <c>otro</c>.</param>
/// <param name="Nombre">De 2 a 60 caracteres.</param>
/// <param name="Anio">Solo para <c>auto</c>, de 1950 al año actual más uno.</param>
internal sealed record CreateItemRequest(string? Tipo = null, string? Nombre = null, int? Anio = null);

/// <summary>Cuerpo de <c>PUT /me/casa-y-auto/{id}</c> (spec 041, R-05 y R-14): cambian solo los campos enviados; lo omitido o nulo se queda como estaba.</summary>
internal sealed record UpdateItemRequest(string? Tipo = null, string? Nombre = null, int? Anio = null);

/// <summary>Un elemento de la persona (spec 041, §5). Nunca lleva el identificador de la persona.</summary>
internal sealed record ItemResponse(string Id, string Tipo, string Nombre, int? Anio, DateTime CreadoEn);

/// <summary>Identificador de la ruta (<c>{id}</c>), tipado para que su validador responda 422 (spec 041, §7).</summary>
internal sealed class ItemRoute
{
    [FromRoute(Name = "id")]
    public string? Id { get; set; }
}

/// <summary>Consulta de <c>GET /me/casa-y-auto/{id}/historial</c> (spec 041, R-13): <c>cursor</c> opaco de Mercado y <c>limite</c> de 1 a 50 (20 por defecto).</summary>
internal sealed class HistoryQuery
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 50;

    [FromQuery(Name = "cursor")]
    public string? Cursor { get; set; }

    /// <summary>Texto a propósito: un valor que no es número responde 422 con el resto, no el 400 automático del enlace de datos.</summary>
    [FromQuery(Name = "limite")]
    public string? Limite { get; set; }

    /// <summary>El límite ya validado (el validador corre antes de la acción).</summary>
    public int ResolvedLimit => int.TryParse(Limite, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : DefaultLimit;
}

/// <summary>Un trabajo terminado del historial de un elemento (spec 041, R-17): día de la visita (<c>YYYY-MM-DD</c>), oficio y técnico, sin precios ni direcciones.</summary>
internal sealed record HistoryItemResponse(string Fecha, string Oficio, string Tecnico);
