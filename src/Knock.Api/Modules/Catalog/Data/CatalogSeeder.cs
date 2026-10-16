using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Knock.Api.Modules.Catalog.Data;

/// <summary>
/// Aplica las semillas versionadas de <c>db/seeds</c>: oficios (<c>catalog/oficios.sql</c>) y colonias piloto
/// (<c>colonias_tula.geojson</c>). Es repetible: ambos hacen upsert por su clave única.
/// </summary>
internal sealed class CatalogSeeder(CatalogDbContext context)
{
    public async Task SeedAsync(string seedsDirectory, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seedsDirectory);
        var oficiosSql = await File.ReadAllTextAsync(
            Path.Combine(seedsDirectory, "catalog", "oficios.sql"), cancellationToken);
        await using var geoJson = File.OpenRead(Path.Combine(seedsDirectory, "colonias_tula.geojson"));
        using var document = await JsonDocument.ParseAsync(geoJson, cancellationToken: cancellationToken);

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await context.Database.ExecuteSqlRawAsync(oficiosSql, cancellationToken);
        foreach (var feature in document.RootElement.GetProperty("features").EnumerateArray())
        {
            await UpsertColoniaAsync(feature, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    /// <remarks>El centro de una colonia con polígono es <c>ST_PointOnSurface</c>: siempre cae dentro del polígono.</remarks>
    private Task<int> UpsertColoniaAsync(JsonElement feature, CancellationToken cancellationToken)
    {
        var properties = feature.GetProperty("properties");
        var nombre = properties.GetProperty("nombre").GetString();
        var cp = properties.GetProperty("cp").GetString();
        var municipio = properties.GetProperty("municipio").GetString();
        var piloto = properties.GetProperty("piloto").GetBoolean();
        var geometry = feature.GetProperty("geometry").GetRawText();
        return context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO catalog.colonias (nombre, cp, municipio, piloto, geom, centro)
            SELECT {nombre}, {cp}, {municipio}, {piloto}, shape::geography, ST_PointOnSurface(shape)::geography
            FROM (SELECT ST_Multi(ST_SetSRID(ST_GeomFromGeoJSON({geometry}), 4326)) AS shape) AS source
            ON CONFLICT (municipio, cp, nombre) DO UPDATE
            SET piloto = EXCLUDED.piloto, geom = EXCLUDED.geom, centro = EXCLUDED.centro
            """,
            cancellationToken);
    }
}
