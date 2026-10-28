using AwesomeAssertions;
using Knock.Api.Modules.Catalog;
using Knock.Api.Modules.Catalog.Domain;
using Knock.Api.Modules.Catalog.Services;
using Knock.DatabaseTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using Xunit;

namespace Knock.DatabaseTest.Catalog;

public sealed class NeighborhoodAtPointTests
{
    private const int _repetitions = 20;

    private static readonly Lazy<Task<CatalogDatabase>> _shared = CatalogDatabase.SharedSeeded();

    /// <summary>
    /// Un traslape del seed por fila: un punto interior del traslape (dentro de los dos polígonos, a ≥ 50 m de sus bordes) y un
    /// vértice del polígono que pierde que cae dentro del que gana (así el resultado no depende de si el motor cuenta el borde).
    /// </summary>
    public static TheoryData<string, double, double, double, double, string> Overlaps => new()
    {
        { "Centro–La Huerta", -99.34942, 20.05399, -99.345888, 20.055532, "Centro" },
        { "Centro–San Lorenzo", -99.33802, 20.05079, -99.337329, 20.052984, "Centro" },
        { "El Cielito–San Lorenzo", -99.33302, 20.04939, -99.33238, 20.052948, "El Cielito" },
        { "El Llano 1a Sección–Tultengo", -99.32272, 20.06559, -99.322428, 20.062463, "El Llano 1a Sección" },
    };

    [Fact]
    public async Task PointInsideEachPilotPolygonReturnsItsNeighborhood_TC_TT_BD_06_04()
    {
        foreach (var (colonia, longitude, latitude) in CatalogSeedData.InteriorPoints)
        {
            (await IntersectingCountAsync(longitude, latitude)).Should().Be(1, $"el punto de {colonia} está dentro de un solo polígono");
            (await FindAsync(latitude, longitude)).Should().Be(colonia);
        }
    }

    [Theory]
    [MemberData(nameof(Overlaps))]
    public async Task SharedBoundaryAndOverlapUseStableTieBreak_TC_TT_BD_06_05(
        string overlap,
        double insideLongitude,
        double insideLatitude,
        double vertexLongitude,
        double vertexLatitude,
        string expected)
    {
        (await IntersectingCountAsync(insideLongitude, insideLatitude)).Should().Be(2, $"el punto está dentro de los dos polígonos de {overlap}");

        for (var i = 0; i < _repetitions; i++)
        {
            (await FindAsync(insideLatitude, insideLongitude)).Should().Be(expected, $"gana el menor nombreBusqueda en {overlap}");
            (await FindAsync(vertexLatitude, vertexLongitude)).Should().Be(expected, $"vértice en el borde dentro de {overlap}");
        }
    }

    [Fact]
    public async Task PointOutsidePolygonsUsesNearestCenterWithinTwoKilometersOrOutOfCoverage_TC_TT_BD_06_06()
    {
        var near = CatalogSeedData.NearCenterOutsidePolygons;
        var (nearestName, nearestMeters) = NearestCenter(near.Longitude, near.Latitude);
        var beyond = CatalogSeedData.BeyondTwoKilometers;
        var other = CatalogSeedData.OtherCity;

        (await IntersectingCountAsync(near.Longitude, near.Latitude)).Should().Be(0);
        nearestMeters.Should().BeLessThanOrEqualTo(NeighborhoodLocator.CenterFallbackMeters);
        (await FindAsync(near.Latitude, near.Longitude)).Should().Be(nearestName);

        NearestCenter(beyond.Longitude, beyond.Latitude).Meters.Should().BeGreaterThan(NeighborhoodLocator.CenterFallbackMeters);
        (await FindAsync(beyond.Latitude, beyond.Longitude)).Should().BeNull("fuera de cobertura: el centro más cercano está a más de 2 km");
        (await FindAsync(other.Latitude, other.Longitude)).Should().BeNull("otra ciudad");
    }

    [Fact]
    public async Task InternalLookupReturnsFullNeighborhoodAndCatalogApiDelegatesToIt_TC_TT_BD_06_19()
    {
        var near = CatalogSeedData.NearCenterOutsidePolygons;
        List<(double Longitude, double Latitude, string? Expected)> points =
        [
            .. CatalogSeedData.InteriorPoints.Select(p => (p.Longitude, p.Latitude, (string?)p.Colonia)),
            .. Overlaps.Select(row => ((double)row[1], (double)row[2], (string?)row[5])),
            .. Overlaps.Select(row => ((double)row[3], (double)row[4], (string?)row[5])),
            (near.Longitude, near.Latitude, NearestCenter(near.Longitude, near.Latitude).Nombre),
            (CatalogSeedData.BeyondTwoKilometers.Longitude, CatalogSeedData.BeyondTwoKilometers.Latitude, null),
            (CatalogSeedData.OtherCity.Longitude, CatalogSeedData.OtherCity.Latitude, null),
        ];

        var locator = (await _shared.Value).Get<NeighborhoodLocator>();
        foreach (var (longitude, latitude, expected) in points)
        {
            var found = await locator.FindAtAsync(latitude, longitude, CancellationToken.None);
            var name = await FindAsync(latitude, longitude);

            found.Should().Be(expected is null ? null : Expected(expected));
            name.Should().Be(found?.Nombre, "ICatalogApi devuelve el nombre de la consulta interna");
        }
    }

    private static Colonia Expected(string nombre)
    {
        var document = CatalogSeedData.Colonia(nombre);
        var id = document["_id"].AsObjectId.ToString();
        id.Should().MatchRegex("^[0-9a-f]{24}$");
        return new Colonia(id, nombre, document["cp"].AsString, document["municipio"].AsString);
    }

    /// <summary>Centro sembrado más cercano (distancia de círculo máximo), calculado desde <c>colonias.json</c>.</summary>
    private static (string Nombre, double Meters) NearestCenter(double longitude, double latitude) =>
        CatalogSeedData.Colonias
            .Where(c => c.Contains("centro"))
            .Select(c =>
            {
                var coordinates = c["centro"]["coordinates"].AsBsonArray;
                return (c["nombre"].AsString, CatalogSeedData.HaversineMeters(
                    longitude, latitude, coordinates[0].ToDouble(), coordinates[1].ToDouble()));
            })
            .MinBy(c => c.Item2);

    private static async Task<string?> FindAsync(double latitude, double longitude) =>
        await (await _shared.Value).InScopeAsync<ICatalogApi, string?>(catalog => catalog.FindNeighborhoodAtAsync(latitude, longitude, CancellationToken.None));

    private static async Task<long> IntersectingCountAsync(double longitude, double latitude) =>
        await (await _shared.Value).Colonias.CountDocumentsAsync(Builders<BsonDocument>.Filter.GeoIntersects(
            "geom", GeoJson.Point(GeoJson.Geographic(longitude, latitude))));
}
