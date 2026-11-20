namespace Knock.Market.Api.Modules.Requests.Dtos;

internal sealed class MyQuotesQuery
{
    public string? Estado { get; set; }
    public string? Cursor { get; set; }
    public string? Limite { get; set; }
}

internal sealed record MyQuote(string Id, string SolicitudId, string Estado, string TipoPrecio, int? Precio, int? PrecioMin,
    int? PrecioMax, int Tope, string Dia, string Hora, DateTime CreadoEn);

internal sealed record MyQuotePage(IReadOnlyList<MyQuote> Items, string? NextCursor);
