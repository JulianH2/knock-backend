namespace Knock.Market.Api.Modules.Requests.Domain;

/// <summary>Wave distribution settings (spec 028). Distribution is disabled until explicitly enabled.</summary>
internal sealed class OleadasOptions
{
    public const string SectionName = "Oleadas";

    public bool Habilitado { get; set; }
    public int TamanoOleada { get; set; } = 5;
    public int MejorOrdenados { get; set; } = 3;
    public int UmbralNuevo { get; set; } = 3;
    public int MinutosRevision { get; set; } = 15;
    public int MinimoCotizaciones { get; set; } = 2;
    public int MaximoOleadas { get; set; } = 3;
    public int MaximoCotizaciones { get; set; } = 5;

    public bool IsValid() => TamanoOleada > 0 && MejorOrdenados > 0 && UmbralNuevo > 0
        && MinutosRevision > 0 && MinimoCotizaciones > 0 && MaximoOleadas > 0 && MaximoCotizaciones > 0
        && (long)MejorOrdenados + 2 <= TamanoOleada;
}
