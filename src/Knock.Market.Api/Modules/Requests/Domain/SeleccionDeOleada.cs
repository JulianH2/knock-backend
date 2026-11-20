namespace Knock.Market.Api.Modules.Requests.Domain;

/// <summary>An eligible technician projected by the candidate store, with distance in kilometres when known.</summary>
internal sealed record WaveCandidate(string TechnicianId, decimal Rating, int CompletedJobs, double? DistanceKm, string? TechnicianUserId = null);

/// <summary>A selected technician and the reason to persist with the delivery.</summary>
internal sealed record WaveSelection(string TechnicianId, string Reason);

/// <summary>Pure selection from candidates already filtered according to R-02.</summary>
internal static class SeleccionDeOleada
{
    public static IReadOnlyList<WaveSelection> Select(IEnumerable<WaveCandidate> candidates, IEnumerable<string> previouslySent, OleadasOptions options)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(previouslySent);
        ArgumentNullException.ThrowIfNull(options);
        if (!options.IsValid()) throw new ArgumentException("Invalid wave options.", nameof(options));

        var excluded = new HashSet<string>(previouslySent, StringComparer.Ordinal);
        var ranked = candidates.Where(x => !excluded.Contains(x.TechnicianId))
            .OrderByDescending(x => x.Rating).ThenByDescending(x => x.CompletedJobs)
            .ThenBy(x => x.TechnicianId, StringComparer.Ordinal)
            .ThenByDescending(x => x.DistanceKm is >= 0 and < double.PositiveInfinity)
            .ThenBy(x => x.DistanceKm is >= 0 and < double.PositiveInfinity ? x.DistanceKm : null)
            .DistinctBy(x => x.TechnicianId, StringComparer.Ordinal).ToArray();
        var selected = new List<WaveSelection>();
        foreach (var candidate in ranked.Take(options.MejorOrdenados))
            Add(candidate, "mejor_ordenado", selected, excluded);

        var newcomer = ranked.FirstOrDefault(x => !excluded.Contains(x.TechnicianId) && x.CompletedJobs < options.UmbralNuevo);
        if (newcomer is not null) Add(newcomer, "nuevo", selected, excluded);

        var nearest = ranked.Where(x => !excluded.Contains(x.TechnicianId) && x.DistanceKm is >= 0 and < double.PositiveInfinity)
            .OrderBy(x => x.DistanceKm).FirstOrDefault();
        if (nearest is not null) Add(nearest, "cercano", selected, excluded);

        foreach (var candidate in ranked.Where(x => !excluded.Contains(x.TechnicianId)).Take(options.TamanoOleada - selected.Count))
            Add(candidate, "relleno", selected, excluded);
        return selected;
    }

    private static void Add(WaveCandidate candidate, string reason, List<WaveSelection> selected, HashSet<string> excluded)
    {
        if (excluded.Add(candidate.TechnicianId)) selected.Add(new WaveSelection(candidate.TechnicianId, reason));
    }
}
