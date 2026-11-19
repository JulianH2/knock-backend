namespace Knock.Market.Api.Modules.Catalog.Domain;

// Group: 0 nombre en zona, 1 oficio en zona, 2 nombre fuera de zona.
internal sealed record SearchRank(string Id, int Group, bool Verified, decimal Rating, double? DistanceKm);

internal static class OrdenDeResultados
{
    public static int? Classify(bool matchesName, bool servesColonia, bool matchesTrade) => (matchesName, servesColonia, matchesTrade) switch
    {
        (true, true, _) => 0,
        (_, true, true) => 1,
        (true, false, _) => 2,
        _ => null,
    };

    public static IOrderedEnumerable<SearchRank> Order(IEnumerable<SearchRank> ranks) =>
        ranks.Order(Comparer<SearchRank>.Create(Compare));

    public static int Compare(SearchRank left, SearchRank right)
    {
        var comparison = left.Group.CompareTo(right.Group);
        if (comparison != 0) return comparison;
        comparison = right.Verified.CompareTo(left.Verified);
        if (comparison != 0) return comparison;
        comparison = right.Rating.CompareTo(left.Rating);
        if (comparison != 0) return comparison;
        comparison = (left.DistanceKm ?? double.PositiveInfinity).CompareTo(right.DistanceKm ?? double.PositiveInfinity);
        return comparison != 0 ? comparison : StringComparer.Ordinal.Compare(left.Id, right.Id);
    }
}
