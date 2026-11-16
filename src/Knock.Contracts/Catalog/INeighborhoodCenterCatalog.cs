namespace Knock.Contracts.Catalog;

/// <summary>Neighborhood center lookup for coverage checks.</summary>
public interface INeighborhoodCenterCatalog
{
    /// <summary>Returns null when the neighborhood does not have a center.</summary>
    Task<(double Latitude, double Longitude)?> FindNeighborhoodCenterAsync(string coloniaId, CancellationToken cancellationToken);
}
