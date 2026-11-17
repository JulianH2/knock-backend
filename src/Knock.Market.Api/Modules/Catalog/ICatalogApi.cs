namespace Knock.Market.Api.Modules.Catalog;

/// <summary>Contrato público del módulo Catalog: la única vía para que otros módulos lo usen (o por eventos).</summary>
public interface ICatalogApi
{
    string ModuleName { get; }

    /// <summary>
    /// Colonia que corresponde a un punto: polígono que lo contiene o toca (si hay varias, la de menor nombre normalizado) y luego
    /// centro más cercano a 2 km o menos. Devuelve <see langword="null"/> si el punto está fuera de cobertura. Las colonias sin
    /// polígono nunca salen. El punto no se guarda ni se registra.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Latitud fuera de [-90, 90] o longitud fuera de [-180, 180].</exception>
    Task<string?> FindNeighborhoodAtAsync(double latitude, double longitude, CancellationToken cancellationToken);

    /// <summary>Nombres de las colonias de un código postal, en orden alfabético sin acentos (incluye las que no tienen polígono).</summary>
    /// <exception cref="ArgumentException">Código postal vacío o que no son 5 dígitos; se lanza antes de consultar.</exception>
    Task<IReadOnlyList<string>> FindNeighborhoodsByPostalCodeAsync(string postalCode, CancellationToken cancellationToken);

    /// <summary>
    /// <see langword="true"/> si existe la colonia <paramref name="coloniaId"/> y es de ese <paramref name="cp"/>. Lo usa Identidad
    /// para validar la zona del perfil.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="coloniaId"/> no son 24 hexadecimales o <paramref name="cp"/> no son 5 dígitos; se lanza antes de consultar.</exception>
    Task<bool> NeighborhoodExistsAsync(string coloniaId, string cp, CancellationToken cancellationToken);

    /// <summary>
    /// Oficio activo con esa <paramref name="clave"/>: su nombre y su modalidad (<c>domicilio</c> o <c>taller</c>), o <see langword="null"/> si no existe o no está
    /// activo. Solo lectura; lo usa Requests para validar la solicitud (spec 020, R-02).
    /// </summary>
    /// <exception cref="ArgumentException">Clave vacía o de más de 16 caracteres; se lanza antes de consultar.</exception>
    Task<(string Nombre, string Modalidad)?> FindActiveTradeAsync(string clave, CancellationToken cancellationToken);

    /// <summary>
    /// Nombre y código postal de la colonia <paramref name="coloniaId"/>, o <see langword="null"/> si no existe. Solo lectura; lo usa Requests (spec 020, R-02).
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="coloniaId"/> no son 24 hexadecimales; se lanza antes de consultar.</exception>
    Task<(string Nombre, string Cp)?> FindNeighborhoodAsync(string coloniaId, CancellationToken cancellationToken);
}
