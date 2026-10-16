using AwesomeAssertions;
using Knock.Api.Modules.Catalog;
using Knock.Api.Modules.Catalog.Data;
using Knock.Api.Modules.Catalog.Domain;
using Knock.DatabaseTest.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Knock.DatabaseTest;

public sealed class CatalogNeighborhoodQueriesTests : IAsyncLifetime
{
    private readonly CatalogDatabaseFixture _database = new();

    public Task InitializeAsync() => _database.InitializeAsync();

    public Task DisposeAsync() => _database.DisposeAsync();

    [Fact]
    public async Task PointInsidePolygonReturnsContainingNeighborhood_TC_TT_BD_02_01()
    {
        // Coordenada sintética dentro del polígono piloto aproximado de Centro (42800).
        const double latitude = 20.051573;
        const double longitude = -99.344554;

        var neighborhood = await FindAsync(latitude, longitude);
        var containing = await CountAsync(latitude, longitude, "ST_Contains");

        neighborhood.Should().Be("Centro");
        containing.Should().Be(1, "el punto está en el interior de un solo polígono");
    }

    [Fact]
    public async Task PointOutsideTulaCoverageReturnsOutOfCoverage_TC_TT_BD_02_02()
    {
        // Punto sintético en otra ciudad (zona de Pachuca), a decenas de km de Tula.
        const double latitude = 20.100000;
        const double longitude = -98.750000;

        var neighborhood = await FindAsync(latitude, longitude);
        var covering = await CountAsync(latitude, longitude, "ST_Covers");
        var centersWithin2Km = await CountCentersWithinAsync(latitude, longitude, 2000);

        neighborhood.Should().BeNull("fuera de cobertura");
        covering.Should().Be(0);
        centersWithin2Km.Should().Be(0, "tampoco aplica el respaldo por centro");
    }

    [Fact]
    public async Task PointWithoutContainingPolygonReturnsNearestCenterWithinTwoKilometers_TC_TT_BD_02_04()
    {
        // A ~0.4 km de «Respaldo Cercana» y ~1.2 km de «Respaldo Lejana»; ningún polígono lo cubre.
        var nearest = await FindAsync(10.000, 20.004);
        var centersWithin2Km = await CountCentersWithinAsync(10.000, 20.004, 2000);
        // A ~2.7 km del centro más cercano: fuera del radio.
        var beyondRadius = await FindAsync(10.000, 20.040);

        nearest.Should().Be("Respaldo Cercana");
        centersWithin2Km.Should().Be(2, "ambos centros están en el radio y gana el más cercano");
        beyondRadius.Should().BeNull();
    }

    [Fact]
    public async Task PostalCodeReturnsOnlyMatchingNeighborhoods_TC_TT_BD_02_05()
    {
        await using var scope = _database.Services.CreateAsyncScope();
        var catalog = scope.ServiceProvider.GetRequiredService<ICatalogApi>();

        var centro = await catalog.FindNeighborhoodsByPostalCodeAsync("42800", CancellationToken.None);
        var cielito = await catalog.FindNeighborhoodsByPostalCodeAsync("42803", CancellationToken.None);

        centro.Should().Equal("Centro");
        cielito.Should().Equal(
            "El Cielito", "El Llano 1a Sección", "El Llano 2a Sección", "Iturbe", "La Huerta", "San Lorenzo", "Tultengo");
        cielito.Should().NotContain("Centro");
    }

    [Fact]
    public async Task SharedBoundaryUsesCoversAndStableTieBreak_TC_TT_BD_02_07()
    {
        // Sobre el borde compartido (longitud 30.01) de «Beta Borde» y «alfa Borde».
        const double latitude = 10.005;
        const double longitude = 30.01;

        var first = await FindAsync(latitude, longitude);
        var second = await FindAsync(latitude, longitude);
        var containing = await CountAsync(latitude, longitude, "ST_Contains");
        var covering = await CountAsync(latitude, longitude, "ST_Covers");

        containing.Should().Be(0, "en el borde no hay coincidencia estricta");
        covering.Should().Be(2);
        first.Should().Be("alfa Borde", "gana el menor nombre normalizado, antes que el centro sobre el borde");
        second.Should().Be(first);
    }

    private async Task<string?> FindAsync(double latitude, double longitude)
    {
        await using var scope = _database.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ICatalogApi>()
            .FindNeighborhoodAtAsync(latitude, longitude, CancellationToken.None);
    }

    private async Task<int> CountAsync(double latitude, double longitude, string predicate)
    {
        var point = CatalogCoordinates.ToPoint(latitude, longitude);
        await using var scope = _database.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var query = predicate == "ST_Contains"
            ? context.Database.SqlQuery<int>(
                $"""SELECT count(*)::int AS "Value" FROM catalog.colonias WHERE ST_Contains(geom::geometry, {point}::geometry)""")
            : context.Database.SqlQuery<int>(
                $"""SELECT count(*)::int AS "Value" FROM catalog.colonias WHERE ST_Covers(geom::geometry, {point}::geometry)""");
        return (await query.ToListAsync())[0];
    }

    private async Task<int> CountCentersWithinAsync(double latitude, double longitude, int meters)
    {
        var point = CatalogCoordinates.ToPoint(latitude, longitude);
        await using var scope = _database.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var counts = await context.Database.SqlQuery<int>(
            $"""SELECT count(*)::int AS "Value" FROM catalog.colonias WHERE ST_DWithin(centro, {point}::geography, {meters})""")
            .ToListAsync();
        return counts[0];
    }
}
