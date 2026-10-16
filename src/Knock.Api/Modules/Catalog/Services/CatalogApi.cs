using Knock.Api.Modules.Catalog.Data;
using Knock.Api.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Knock.Api.Modules.Catalog.Services;

internal sealed class CatalogApi(CatalogDbContext context) : ICatalogApi
{
    private const int _centerFallbackMeters = 2000;

    public string ModuleName => "Catalog";

    /// <remarks>
    /// Orden: interior (<c>ST_Contains</c>), borde (<c>ST_Covers</c>), centro más cercano a 2 km o menos (<c>ST_DWithin</c>).
    /// Empates por nombre normalizado y luego id. El punto solo viaja como parámetro: no se guarda ni se registra.
    /// </remarks>
    public async Task<string?> FindNeighborhoodAtAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        var point = CatalogCoordinates.ToPoint(latitude, longitude);
        var names = await context.Database.SqlQuery<string>(
            $"""
            SELECT c.nombre AS "Value"
            FROM catalog.colonias AS c
            CROSS JOIN (SELECT {point}::geometry AS g) AS p
            WHERE ST_Covers(c.geom::geometry, p.g) OR ST_DWithin(c.centro, p.g::geography, {_centerFallbackMeters})
            ORDER BY
                CASE WHEN ST_Contains(c.geom::geometry, p.g) THEN 0 WHEN ST_Covers(c.geom::geometry, p.g) THEN 1 ELSE 2 END,
                CASE WHEN ST_Covers(c.geom::geometry, p.g) THEN 0 ELSE ST_Distance(c.centro, p.g::geography) END,
                lower(c.nombre) COLLATE "C",
                c.id
            LIMIT 1
            """).ToListAsync(cancellationToken);
        return names.Count == 0 ? null : names[0];
    }

    public async Task<IReadOnlyList<string>> FindNeighborhoodsByPostalCodeAsync(
        string postalCode,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(postalCode);
        return await context.Colonias
            .AsNoTracking()
            .Where(c => c.Cp == postalCode)
            .OrderBy(c => c.Nombre)
            .Select(c => c.Nombre)
            .ToListAsync(cancellationToken);
    }
}
