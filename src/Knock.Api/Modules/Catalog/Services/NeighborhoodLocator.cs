using Knock.Api.Modules.Catalog.Data;
using Knock.Api.Modules.Catalog.Domain;

namespace Knock.Api.Modules.Catalog.Services;

/// <summary>
/// Consulta interna de colonia por punto (no es contrato entre módulos). La usan <see cref="CatalogApi"/> y el controller de
/// TT-API-07: una sola implementación del desempate (TC-TT-BD-06-19).
/// </summary>
internal sealed class NeighborhoodLocator(CatalogStore store) : INeighborhoodLocator
{
    public const double CenterFallbackMeters = 2000;

    /// <summary>
    /// (1) polígono que contiene o toca el punto, con desempate por <c>nombreBusqueda</c> y <c>_id</c>; (2) si no hay, centro más
    /// cercano a 2000 m o menos; (3) si no, <see langword="null"/>. El punto solo viaja en la consulta: no se guarda ni se registra.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Coordenadas fuera de rango; se lanza antes de consultar.</exception>
    public async Task<Colonia?> FindAtAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        var position = CatalogCoordinates.ToPosition(latitude, longitude);
        return await store.FindIntersectingAsync(position, cancellationToken)
            ?? await store.FindNearestCenterAsync(position, CenterFallbackMeters, cancellationToken);
    }
}
