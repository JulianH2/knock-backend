namespace Knock.Api.Modules.Catalog;

/// <summary>Contrato público del módulo Catalog: la única vía para que otros módulos lo usen (o por eventos).</summary>
public interface ICatalogApi
{
    string ModuleName { get; }

    /// <summary>
    /// Colonia que corresponde a un punto: contención, luego borde y luego centro más cercano a 2 km o menos.
    /// Devuelve <see langword="null"/> si el punto está fuera de cobertura. El punto no se guarda ni se registra.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Latitud fuera de [-90, 90] o longitud fuera de [-180, 180].</exception>
    Task<string?> FindNeighborhoodAtAsync(double latitude, double longitude, CancellationToken cancellationToken);

    /// <summary>Nombres de las colonias de un código postal, en orden alfabético.</summary>
    Task<IReadOnlyList<string>> FindNeighborhoodsByPostalCodeAsync(string postalCode, CancellationToken cancellationToken);
}
