using Knock.Api.Modules.Catalog.Data;
using Knock.Api.Modules.Catalog.Dtos;

namespace Knock.Api.Modules.Catalog.Services;

/// <summary>Consultas de solo lectura de los oficios del catálogo para la API HTTP del módulo.</summary>
internal sealed class OficioQueries(CatalogStore store)
{
    /// <summary>Oficios activos ordenados por <c>nombreBusqueda</c> (sin mayúsculas ni acentos) y, a igual valor, por clave.</summary>
    /// <remarks>
    /// Regla de spec TT-PRU-01 §8 D4 tras la migración a MongoDB (TT-BD-06): usa el campo normalizado e indexado, por lo que ya no
    /// aplica el límite de acentos de <c>COLLATE "C"</c>. Solo <see cref="CatalogStore"/> toca <c>IMongoCollection</c>.
    /// </remarks>
    public async Task<IReadOnlyList<OficioResponse>> ListActiveAsync(CancellationToken cancellationToken) =>
        [.. (await store.FindActiveOficiosAsync(cancellationToken)).Select(o => new OficioResponse(o.Clave, o.Nombre, o.Modalidad))];
}
