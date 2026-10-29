using Knock.Api.Modules.Catalog.Domain;

namespace Knock.Api.Modules.Catalog.Services;

/// <summary>Consulta interna de colonia por punto (<see cref="NeighborhoodLocator"/>); la interfaz deja a las pruebas contar las llamadas del endpoint.</summary>
internal interface INeighborhoodLocator
{
    /// <inheritdoc cref="NeighborhoodLocator.FindAtAsync"/>
    Task<Colonia?> FindAtAsync(double latitude, double longitude, CancellationToken cancellationToken);
}
