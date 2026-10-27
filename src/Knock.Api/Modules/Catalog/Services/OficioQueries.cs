using Knock.Api.Modules.Catalog.Data;
using Knock.Api.Modules.Catalog.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Knock.Api.Modules.Catalog.Services;

/// <summary>Consultas de solo lectura de los oficios del catálogo para la API HTTP del módulo.</summary>
internal sealed class OficioQueries(CatalogDbContext context)
{
    /// <summary>Oficios activos ordenados por nombre sin importar mayúsculas y, a igual nombre, por clave.</summary>
    /// <remarks>
    /// Equivale a <c>ORDER BY lower(nombre) COLLATE "C", clave</c> (spec TT-PRU-01 §8 D4): no depende de la intercalación de la base.
    /// Límite conocido: con <c>"C"</c> un nombre que empiece con vocal acentuada («Ébano») queda después de la «z».
    /// Con la migración del catálogo a MongoDB (TT-BD-06) esta consulta se reimplementa conservando el orden resultante.
    /// </remarks>
    public async Task<IReadOnlyList<OficioResponse>> ListActiveAsync(CancellationToken cancellationToken) =>
        await context.Oficios
            .AsNoTracking()
            .Where(o => o.Activo)
            .OrderBy(o => EF.Functions.Collate(CatalogDbContext.Lower(o.Nombre), "C"))
            .ThenBy(o => o.Clave)
            .Select(o => new OficioResponse(o.Clave, o.Nombre, o.Modalidad))
            .ToListAsync(cancellationToken);
}
