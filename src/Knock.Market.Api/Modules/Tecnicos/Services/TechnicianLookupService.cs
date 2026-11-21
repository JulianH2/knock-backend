using System.Globalization;
using System.Text;
using Knock.Contracts.Catalog;
using Knock.Market.Api.Modules.Catalog;
using Knock.Market.Api.Modules.Tecnicos.Domain;
using Knock.Market.Api.Modules.Tecnicos.Dtos;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;

namespace Knock.Market.Api.Modules.Tecnicos.Services;

/// <summary>Lo que se lee de un técnico para decidir si llega a una colonia (TEC-06). Las coordenadas son las aproximadas de su colonia base.</summary>
internal sealed record TechnicianCoverage(
    string Estado,
    bool Oculto,
    DateTime? OcultoHasta,
    string? ColoniaBaseId,
    string? Atencion,
    int? RadioKm,
    double? Latitude,
    double? Longitude);

/// <summary>Nombre de negocio de un técnico visible, para el autocompletado (TEC-03).</summary>
internal sealed record BusinessName(string Id, string Nombre);

internal interface ITechnicianLookupReader
{
    Task<TechnicianCoverage?> FindCoverageAsync(ObjectId id, CancellationToken cancellationToken);

    /// <summary>Hasta <paramref name="limit"/> negocios de técnicos visibles cuyo nombre contiene <paramref name="text"/> (sin mayúsculas).</summary>
    Task<IReadOnlyList<BusinessName>> FindBusinessNamesAsync(string text, int limit, CancellationToken cancellationToken);

    Task<TechnicianListPage> FindListAsync(TechnicianListQuery query, CancellationToken cancellationToken);
}

internal enum CoverageStatus
{
    Found,
    TechnicianNotFound,
    ColoniaNotFound,
}

internal sealed record CoverageOutcome(CoverageStatus Status, bool Atiende = false);

/// <summary>
/// Lecturas puntuales de técnicos (spec 046): «llega a mi colonia» (TEC-06, R-43) y autocompletado (TEC-03, R-41). Ambas se guardan en la caché de la réplica:
/// 1 minuto por técnico y colonia, y 30 segundos por texto normalizado. Ninguna clave lleva datos de otra persona, y el texto buscado no se registra.
/// </summary>
internal sealed class TechnicianLookupService(
    ITechnicianLookupReader reader,
    ICatalogApi catalog,
    INeighborhoodCenterCatalog centers,
    IMemoryCache cache,
    TimeProvider clock)
{
    public const int MaxSuggestions = 8;

    public static readonly TimeSpan CoverageLifetime = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan SuggestionsLifetime = TimeSpan.FromSeconds(30);

    private const double _earthRadiusKm = 6371.0;

    public async Task<CoverageOutcome> CoverageAsync(ObjectId technicianId, string coloniaId, CancellationToken cancellationToken)
    {
        var colonia = ObjectId.Parse(coloniaId).ToString();
        var key = $"tecnicos:llega:{technicianId}:{colonia}";
        if (cache.TryGetValue(key, out CoverageOutcome? cached) && cached is not null)
        {
            return cached;
        }

        var technician = await reader.FindCoverageAsync(technicianId, cancellationToken);
        if (technician is null || !TechnicianRules.IsVisible(technician.Estado, technician.Oculto, technician.OcultoHasta, clock.GetUtcNow().UtcDateTime))
        {
            return new CoverageOutcome(CoverageStatus.TechnicianNotFound);
        }

        if (await catalog.FindNeighborhoodAsync(colonia, cancellationToken) is null)
        {
            return new CoverageOutcome(CoverageStatus.ColoniaNotFound);
        }

        var outcome = new CoverageOutcome(CoverageStatus.Found, await ServesAsync(technician, colonia, cancellationToken));
        cache.Set(key, outcome, CoverageLifetime);
        return outcome;
    }

    /// <summary>Su colonia base siempre; a domicilio, también cualquier colonia cuyo centro quede dentro de su radio.</summary>
    private async Task<bool> ServesAsync(TechnicianCoverage technician, string colonia, CancellationToken cancellationToken)
    {
        if (string.Equals(technician.ColoniaBaseId, colonia, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!TechnicianRules.VisitsHomes(technician.Atencion) || technician.RadioKm is not { } radius
            || technician.Latitude is not { } lat || technician.Longitude is not { } lon)
        {
            return false;
        }

        var center = await centers.FindNeighborhoodCenterAsync(colonia, cancellationToken);
        return center is { } point && DistanceKm(lat, lon, point.Latitude, point.Longitude) <= radius;
    }

    public async Task<AutocompleteResponse> AutocompleteAsync(string text, CancellationToken cancellationToken)
    {
        var normalized = Normalize(text);
        var key = $"tecnicos:autocompletar:{normalized}";
        if (cache.TryGetValue(key, out AutocompleteResponse? cached) && cached is not null)
        {
            return cached;
        }

        var trades = (await catalog.ListActiveTradesAsync(cancellationToken))
            .Where(t => Normalize(t.Nombre).Contains(normalized, StringComparison.Ordinal))
            .OrderBy(t => Normalize(t.Nombre).StartsWith(normalized, StringComparison.Ordinal) ? 0 : 1).ThenBy(t => t.Nombre, StringComparer.Ordinal)
            .Select(t => new Suggestion("oficio", t.Nombre, t.Clave, null))
            .Take(MaxSuggestions).ToList();
        if (trades.Count < MaxSuggestions)
        {
            var businesses = await reader.FindBusinessNamesAsync(text.Trim(), MaxSuggestions - trades.Count, cancellationToken);
            trades.AddRange(businesses.Select(b => new Suggestion("negocio", b.Nombre, null, b.Id)));
        }

        var response = new AutocompleteResponse(trades);
        cache.Set(key, response, SuggestionsLifetime);
        return response;
    }

    /// <summary>Sin acentos, en mayúsculas y con espacios simples: así «plomeria» encuentra «Plomería».</summary>
    internal static string Normalize(string value)
    {
        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark))
        {
            builder.Append(char.ToUpperInvariant(c));
        }

        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    internal static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        static double Radians(double degrees) => degrees * Math.PI / 180.0;
        var dLat = Radians(lat2 - lat1);
        var dLon = Radians(lon2 - lon1);
        var a = (Math.Sin(dLat / 2) * Math.Sin(dLat / 2)) + (Math.Cos(Radians(lat1)) * Math.Cos(Radians(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2));
        return 2 * _earthRadiusKm * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    /// <summary>TEC-02 (spec 046, R-04): filtros estructurados, orden y cursor por página. La caché de la réplica dura
    /// 30 segundos por consulta; su clave no lleva datos de otra persona, y se invalida al escribir (R-07).</summary>
    public async Task<TechnicianListOutcome> SearchAsync(TechnicianListQuery query, CancellationToken cancellationToken)
    {
        var key = CacheKey(query);
        if (cache.TryGetValue(key, out TechnicianListResponse? cached) && cached is not null)
        {
            return new TechnicianListOutcome(TechnicianListStatus.Ok, cached);
        }

        if (query.Oficio is not null && await catalog.FindActiveTradeAsync(query.Oficio, cancellationToken) is null)
        {
            return new TechnicianListOutcome(TechnicianListStatus.OficioInvalid, null);
        }

        if (query.Colonia is not null && await catalog.FindNeighborhoodAsync(query.Colonia, cancellationToken) is null)
        {
            return new TechnicianListOutcome(TechnicianListStatus.ColoniaNotFound, null);
        }

        if (query.Orden == "distancia" && (query.Lat is null || query.Lng is null))
        {
            return new TechnicianListOutcome(TechnicianListStatus.MissingPosition, null);
        }

        var page = await reader.FindListAsync(query, cancellationToken);
        var response = new TechnicianListResponse(page.Tecnicos.Select(ToResponse).ToList(), page.NextCursor);
        cache.Set(key, response, SuggestionsLifetime);
        lock (_listKeysLock)
        {
            _listKeys.Add(key);
        }

        return new TechnicianListOutcome(TechnicianListStatus.Ok, response);
    }

    /// <summary>Quita de la caché de esta réplica todas las consultas de lista (llamada por las escrituras del perfil).</summary>
    public void ForgetListCache()
    {
        lock (_listKeysLock)
        {
            foreach (var key in _listKeys)
            {
                cache.Remove(key);
            }

            _listKeys.Clear();
        }
    }

    private static TechnicianProfileResponse ToResponse(TechnicianProfileRecord record) =>
        new(record.Id.ToString(), record.Nombre, record.NombreNegocio, record.FotoUrl, record.Verificacion,
            record.Calificacion is { } rating ? Math.Round(rating, 1, MidpointRounding.AwayFromZero) : null, record.Anios,
            TechnicianProfileService.Shorten(record.Descripcion), record.Oficios, Detalle: null);

    internal static string CacheKey(TechnicianListQuery query) =>
        $"tecnicos:lista:{query.Oficio ?? ""}|{query.Colonia ?? ""}|{query.Lat ?? ""}|{query.Lng ?? ""}|{query.RadioKm ?? ""}|{query.DisponibleHoy ?? ""}|{query.SoloVerificados ?? ""}|{query.Orden ?? ""}|{query.Cursor ?? ""}|{query.Limite ?? ""}";

    private readonly HashSet<string> _listKeys = [];
    private readonly object _listKeysLock = new();
}
