using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Market.Api.Modules.Requests.Dtos;

/// <summary>
/// Consulta de <c>GET /api/v1/interno/historial</c> (spec 041, R-17), ruta interna que solo llama Identidad con su token de servicio. <c>usuarioId</c> y <c>elementoId</c> también
/// van como claims del token y deben coincidir con él.
/// </summary>
internal sealed class InternalHistoryQuery
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 50;

    [FromQuery(Name = "usuarioId")]
    [Description("The sub of the client whose finished visits are listed.")]
    public string? UsuarioId { get; set; }

    [FromQuery(Name = "elementoId")]
    [Description("The element id (24 hexadecimal characters) linked to the requests.")]
    public string? ElementoId { get; set; }

    [FromQuery(Name = "cursor")]
    [Description("Opaque cursor returned as nextCursor by the previous page.")]
    public string? Cursor { get; set; }

    [FromQuery(Name = "limite")]
    [Description("Page size, 1 to 50. Defaults to 20.")]
    public int Limite { get; set; } = DefaultLimit;
}

/// <summary>Un trabajo terminado del historial (spec 041, R-17): día de la visita (<c>YYYY-MM-DD</c>), oficio y nombre del técnico. Sin precios ni direcciones.</summary>
internal sealed record HistoryEntryResponse(string Fecha, string Oficio, string Tecnico);
