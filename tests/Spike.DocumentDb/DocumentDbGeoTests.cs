using AwesomeAssertions;
using Knock.Spike.DocumentDb.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;
using Xunit.Abstractions;

namespace Knock.Spike.DocumentDb;

/// <summary>P2: punto dentro de polígono con 2dsphere, respaldo por centro y fuera de cobertura.</summary>
public sealed class DocumentDbGeoTests(ITestOutputHelper output)
{
    public static TheoryData<string, string, double, double> InteriorPoints
    {
        get
        {
            var data = new TheoryData<string, string, double, double>();
            foreach (var target in new[] { SpikeTargets.Mongo, SpikeTargets.DocumentDbLocal })
            {
                foreach (var (colonia, lng, lat) in SpikeSeeds.InteriorPoints)
                {
                    data.Add(target, colonia, lng, lat);
                }
            }

            return data;
        }
    }

    [Theory]
    [LocalTargets]
    [AzureTarget]
    public async Task CreateIndexes_SeedPolygonsAndCenters_AreAccepted_TC_TT_BD_05_02(string target)
    {
        await using var spike = await SpikePool.CreateDatabaseAsync(target);

        await SpikeSeeds.CreateColoniasAsync(spike.Database);

        var colonias = spike.Database.GetCollection<BsonDocument>(SpikeSeeds.Colonias);
        (await colonias.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(12);
        var indexes = await (await colonias.Indexes.ListAsync()).ToListAsync();
        indexes.Select(i => i["name"].AsString).Should().Contain(["geom_2dsphere", "centro_2dsphere"]);
    }

    [Theory]
    [MemberData(nameof(InteriorPoints))]
    public async Task GeoIntersects_PointInsidePolygon_ReturnsOnlyThatNeighborhood_TC_TT_BD_05_02(
        string target, string expected, double lng, double lat)
    {
        await using var spike = await SeededAsync(target);

        var found = await SpikeQueries.IntersectingAsync(spike.Database, lng, lat);

        found.Should().Equal(expected);
    }

    [Theory]
    [LocalTargets]
    [AzureTarget]
    public async Task GeoIntersects_PointOnPolygonVertex_ReturnsThatNeighborhood_TC_TT_BD_05_02(string target)
    {
        await using var spike = await SeededAsync(target);

        // Segundo vértice del anillo de El Cielito en el seed (no pertenece a otro polígono).
        var found = await SpikeQueries.IntersectingAsync(spike.Database, -99.335268, 20.053768);

        found.Should().Equal("El Cielito");
    }

    [Theory]
    [LocalTargets]
    [AzureTarget]
    public async Task GeoIntersects_PointWhereSeedPolygonsOverlap_ReturnsBothNeighborhoods_TC_TT_BD_05_02(string target)
    {
        await using var spike = await SeededAsync(target);

        // Hallazgo del spike: el seed traslapa El Llano 1a Sección y Tultengo (también Centro–La Huerta,
        // Centro–San Lorenzo y El Cielito–San Lorenzo). La base devuelve las dos; TT-BD-06 debe decidir.
        var found = await SpikeQueries.IntersectingAsync(spike.Database, -99.323016, 20.065535);

        found.Should().BeEquivalentTo("El Llano 1a Sección", "Tultengo");
    }

    [Theory]
    [LocalTargets]
    [AzureTarget]
    public async Task GeoIntersectsAndNear_PointOutsideTula_ReturnNothing_TC_TT_BD_05_02(string target)
    {
        await using var spike = await SeededAsync(target);
        const double pachucaLng = -98.7333, pachucaLat = 20.1167;

        var intersecting = await SpikeQueries.IntersectingAsync(spike.Database, pachucaLng, pachucaLat);
        var near = await SpikeQueries.NearAsync(spike.Database, pachucaLng, pachucaLat);

        intersecting.Should().BeEmpty();
        near.Should().BeEmpty();
    }

    [Theory]
    [LocalTargets]
    [AzureTarget]
    public async Task Near_PointOutsidePolygonsNearSyntheticCenter_OrdersByDistance_TC_TT_BD_05_02(string target)
    {
        await using var spike = await SeededAsync(target);
        const double lng = -99.3405, lat = 20.0385;

        var intersecting = await SpikeQueries.IntersectingAsync(spike.Database, lng, lat);
        var near = await SpikeQueries.NearAsync(spike.Database, lng, lat);

        intersecting.Should().BeEmpty();
        near.Should().NotBeEmpty();
        near[0].Nombre.Should().Be("Mastrantos");
        near.Select(n => n.Meters).Should().BeInAscendingOrder();
        near.Should().OnlyContain(n => n.Meters <= SpikeQueries.FallbackMaxDistanceMeters);
        output.WriteLine($"[{target}] $near: {string.Join(", ", near.Select(n => $"{n.Nombre} {n.Meters:0} m"))}");
    }

    [Theory]
    [LocalTargets]
    [AzureTarget]
    public async Task Insert_SelfIntersectingPolygon_IsRejected_TC_TT_BD_05_02(string target)
    {
        await using var spike = await SeededAsync(target);

        var insert = () => SpikeQueries.InsertSelfIntersectingPolygonAsync(spike.Database);

        var rejection = await insert.Should().ThrowAsync<MongoException>();
        output.WriteLine($"[{target}] rechazo: {rejection.Which.GetType().Name}");
    }

    private static async Task<SpikeDatabase> SeededAsync(string target)
    {
        var spike = await SpikePool.CreateDatabaseAsync(target);
        await SpikeSeeds.CreateColoniasAsync(spike.Database);
        return spike;
    }
}
