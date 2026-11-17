using MongoDB.Bson;

namespace Knock.Market.Api.Modules.Requests.Domain;

/// <summary>Cotización con los datos públicos de su técnico, lista para ordenar y etiquetar (spec 021, R-02 a R-07).</summary>
internal sealed record QuoteToCompare(
    ObjectId Id,
    string Estado,
    string TipoPrecio,
    int? Precio,
    int? PrecioMin,
    int? PrecioMax,
    int Tope,
    bool RequiereAprobacion,
    string Dia,
    string Hora,
    string? Nota,
    string? MotivoRechazo,
    DateTime CreadoEnUtc,
    string TecnicoId,
    string TecnicoNombre,
    string? TecnicoFotoUrl,
    double? Calificacion,
    string NivelVerificacion)
{
    public bool Verificado => NivelVerificacion != QuoteComparer.NoVerification;

    public bool EsVisita => TipoPrecio == QuoteComparer.VisitType;
}

/// <summary>Una cotización ya ordenada, con su texto de «qué cubre» y sus etiquetas.</summary>
internal sealed record ComparedQuote(QuoteToCompare Quote, string Cubre, IReadOnlyList<string> Etiquetas);

/// <summary>Resultado de comparar: las cotizaciones en el orden pedido y si hay tipos de precio distintos (la app muestra el aviso).</summary>
internal sealed record QuoteComparisonResult(IReadOnlyList<ComparedQuote> Items, bool TiposMixtos);

/// <summary>
/// Función pura que ordena y etiqueta las cotizaciones de una solicitud (spec 021, R-04 a R-07): sin acceso a datos ni reloj, para poder probarla con mutación y
/// sustituirla por el orden con aprendizaje automático (HU-077) sin cambiar el contrato. Cualquier empate se resuelve por <c>creadoEn</c> ascendente y luego por <c>_id</c>.
/// </summary>
internal static class QuoteComparer
{
    public const string NoVerification = "ninguno";
    public const string VisitType = "visita";
    public const string Recommended = "recomendadas";
    public const string ByPrice = "precio";
    public const string ByRating = "calificacion";
    public const string Soonest = "pronto";
    public const string VerifiedTag = "verificado";
    public const string BestPriceTag = "mejor_precio";
    public const string SoonestTag = "puede_ir_mas_pronto";

    private static readonly string[] _orders = [Recommended, ByPrice, ByRating, Soonest];

    /// <summary>Valores válidos de <c>?orden</c>.</summary>
    public static IReadOnlyList<string> Orders => _orders;

    public static QuoteComparisonResult Compare(IReadOnlyList<QuoteToCompare> quotes, string order)
    {
        ArgumentNullException.ThrowIfNull(quotes);
        var ordered = Sort(quotes, order);
        var bestPrice = ordered.Where(q => !q.EsVisita).OrderBy(q => q.Tope).ThenBy(q => q.CreadoEnUtc).ThenBy(q => q.Id).FirstOrDefault();
        var soonest = quotes.OrderBy(q => q.Dia, StringComparer.Ordinal).ThenBy(q => q.Hora, StringComparer.Ordinal).ThenBy(q => q.CreadoEnUtc).ThenBy(q => q.Id).FirstOrDefault();
        var items = ordered.Select(q => new ComparedQuote(q, Covers(q.TipoPrecio), Tags(q, bestPrice, soonest))).ToList();
        return new QuoteComparisonResult(items, quotes.Select(q => q.TipoPrecio).Distinct(StringComparer.Ordinal).Count() > 1);
    }

    /// <summary>Texto fijo de lo que cubre cada tipo de precio (R-07).</summary>
    public static string Covers(string priceType) => priceType switch
    {
        VisitType => "Solo visita: ir a ver el trabajo",
        "fijo" => "Arreglo completo, mano de obra incluida, refacciones aparte",
        "rango" => "Estimado del arreglo dentro del rango",
        _ => throw new ArgumentOutOfRangeException(nameof(priceType), "Unknown price type."),
    };

    private static List<string> Tags(QuoteToCompare quote, QuoteToCompare? bestPrice, QuoteToCompare? soonest)
    {
        var tags = new List<string>();
        if (quote.Verificado)
        {
            tags.Add(VerifiedTag);
        }

        if (bestPrice is not null && quote.Id == bestPrice.Id)
        {
            tags.Add(BestPriceTag);
        }

        if (soonest is not null && quote.Id == soonest.Id)
        {
            tags.Add(SoonestTag);
        }

        return tags;
    }

    private static List<QuoteToCompare> Sort(IReadOnlyList<QuoteToCompare> quotes, string order)
    {
        IOrderedEnumerable<QuoteToCompare> sorted = order switch
        {
            ByPrice => quotes.OrderBy(q => q.EsVisita).ThenBy(q => q.Tope),
            ByRating => quotes.OrderByDescending(q => q.Calificacion.HasValue).ThenByDescending(q => q.Calificacion ?? 0),
            Soonest => quotes.OrderBy(q => q.Dia, StringComparer.Ordinal).ThenBy(q => q.Hora, StringComparer.Ordinal),
            Recommended => quotes.OrderByDescending(q => q.Verificado).ThenByDescending(q => q.Calificacion.HasValue).ThenByDescending(q => q.Calificacion ?? 0).ThenBy(q => q.Tope),
            _ => throw new ArgumentOutOfRangeException(nameof(order), "Unknown order."),
        };
        return [.. sorted.ThenBy(q => q.CreadoEnUtc).ThenBy(q => q.Id)];
    }
}
