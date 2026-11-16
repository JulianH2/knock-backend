using System.ComponentModel;

namespace Knock.Market.Api.Modules.Requests.Dtos;

/// <summary>Consulta de <c>GET /solicitudes</c> (spec 020, R-09). Fuera de rango responde 422; un cursor ilegible, 400 <c>invalid_cursor</c>.</summary>
internal sealed class ListRequestsQuery
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 50;

    [Description("Opaque cursor returned as nextCursor by the previous page.")]
    public string? Cursor { get; set; }

    [Description("Page size, 1 to 50. Defaults to 20.")]
    public int Limit { get; set; } = DefaultLimit;

    [Description("Optional filter by state: enviada, cotizando, cita_confirmada, terminada, calificada, cancelada or vencida.")]
    public string? Estado { get; set; }
}
