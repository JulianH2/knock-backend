using AwesomeAssertions;
using Knock.Spike.DocumentDb.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using Xunit;
using Xunit.Abstractions;

namespace Knock.Spike.DocumentDb;

/// <summary>P2: punto dentro de polígono con 2dsphere, respaldo por centro y fuera de cobertura.</summary>
public sealed class DocumentDbGeoTests(ITestOutputHelper output)
{
    private const double _fallbackMaxDistanceMeters = 2000;

    // Puntos sintéticos interiores, a ≥ 60 m de cualquier borde y fuera de las zonas donde el seed se traslapa.
    public static TheoryData<string, string, double, double> InteriorPoints => new()
    {
        { SpikeTargets.Mongo, "Centro", -99.3495, 20.0505 },
        { SpikeTargets.Mongo, "El Cielito", -99.3321, 20.0541 },
        { SpikeTargets.Mongo, "El Llano 1a Sección", -99.3243, 20.0616 },
        { SpikeTargets.Mongo, "El Llano 2a Sección", -99.3060, 20.0460 },
        { SpikeTargets.Mongo, "San Lorenzo", -99.3372, 20.0484 },
        { SpikeTargets.Mongo, "Tultengo", -99.32637, 20.06399 },
        { SpikeTargets.Mongo, "Iturbe", -99.3234, 20.0757 },
        { SpikeTargets.Mongo, "La Huerta", -99.3531, 20.0556 },
        { SpikeTargets.DocumentDbLocal, "Centro", -99.3495, 20.0505 },
        { SpikeTargets.DocumentDbLocal, "El Cielito", -99.3321, 20.0541 },
        { SpikeTargets.DocumentDbLocal, "El Llano 1a Sección", -99.3243, 20.0616 },
        { SpikeTargets.DocumentDbLocal, "El Llano 2a Sección", -99.3060, 20.0460 },
        { SpikeTargets.DocumentDbLocal, "San Lorenzo", -99.3372, 20.0484 },
        { SpikeTargets.DocumentDbLocal, "Tultengo", -99.32637, 20.06399 },
        { SpikeTargets.DocumentDbLocal, "Iturbe", -99.3234, 20.0757 },
        { SpikeTargets.DocumentDbLocal, "La Huerta", -99.3531, 20.0556 },
    };

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

        var found = await IntersectingAsync(spike.Database, lng, lat);

        found.Should().Equal(expected);
    }

    [Theory]
    [LocalTargets]
    [AzureTarget]
    public async Task GeoIntersects_PointOnPolygonVertex_ReturnsThatNeighborhood_TC_TT_BD_05_02(string target)
    {
        await using var spike = await SeededAsync(target);

        // Segundo vértice del anillo de El Cielito en el seed (no pertenece a otro polígono).
        var found = await IntersectingAsync(spike.Database, -99.335268, 20.053768);

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
        var found = await IntersectingAsync(spike.Database, -99.323016, 20.065535);

        found.Should().BeEquivalentTo("El Llano 1a Sección", "Tultengo");
    }

    [Theory]
    [LocalTargets]
    [AzureTarget]
    public async Task GeoIntersectsAndNear_PointOutsideTula_ReturnNothing_TC_TT_BD_05_02(string target)
    {
        await using var spike = await SeededAsync(target);
        const double pachucaLng = -98.7333, pachucaLat = 20.1167;

        var intersecting = await IntersectingAsync(spike.Database, pachucaLng, pachucaLat);
        var near = await NearAsync(spike.Database, pachucaLng, pachucaLat);

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

        var intersecting = await IntersectingAsync(spike.Database, lng, lat);
        var near = await NearAsync(spike.Database, lng, lat);

        intersecting.Should().BeEmpty();
        near.Should().NotBeEmpty();
        near[0].Nombre.Should().Be("Mastrantos");
        near.Select(n => n.Meters).Should().BeInAscendingOrder();
        near.Should().OnlyContain(n => n.Meters <= _fallbackMaxDistanceMeters);
        output.WriteLine($"[{target}] $near: {string.Join(", ", near.Select(n => $"{n.Nombre} {n.Meters:0} m"))}");
    }

    [Theory]
    [LocalTargets]
    [AzureTarget]
    public async Task Insert_SelfIntersectingPolygon_IsRejected_TC_TT_BD_05_02(string target)
    {
        await using var spike = await SeededAsync(target);
        var colonias = spike.Database.GetCollection<BsonDocument>(SpikeSeeds.Colonias);
        // Polígono «moño» sintético: sus aristas se cruzan.
        var bowTie = BsonDocument.Parse(
            "{ type: 'Polygon', coordinates: [[[-99.33, 20.05], [-99.32, 20.06], [-99.32, 20.05], [-99.33, 20.06], [-99.33, 20.05]]] }");

        var insert = () => colonias.InsertOneAsync(new BsonDocument { { "nombre", "Inválida" }, { "geom", bowTie } });

        var rejection = await insert.Should().ThrowAsync<MongoException>();
        output.WriteLine($"[{target}] rechazo: {rejection.Which.GetType().Name}");
    }

    private static async Task<SpikeDatabase> SeededAsync(string target)
    {
        var spike = await SpikePool.CreateDatabaseAsync(target);
        await SpikeSeeds.CreateColoniasAsync(spike.Database);
        return spike;
    }

    private static async Task<List<string>> IntersectingAsync(IMongoDatabase db, double lng, double lat)
    {
        var filter = Builders<BsonDocument>.Filter.GeoIntersects("geom", GeoJson.Point(GeoJson.Geographic(lng, lat)));
        var docs = await db.GetCollection<BsonDocument>(SpikeSeeds.Colonias).Find(filter).ToListAsync();
        return docs.Select(d => d["nombre"].AsString).ToList();
    }

    private static async Task<List<(string Nombre, double Meters)>> NearAsync(IMongoDatabase db, double lng, double lat)
    {
        var filter = Builders<BsonDocument>.Filter.Near(
            "centro", GeoJson.Point(GeoJson.Geographic(lng, lat)), maxDistance: _fallbackMaxDistanceMeters);
        var docs = await db.GetCollection<BsonDocument>(SpikeSeeds.Colonias).Find(filter).ToListAsync();
        return docs.Select(d =>
        {
            var c = d["centro"]["coordinates"].AsBsonArray;
            return (d["nombre"].AsString, HaversineMeters(lng, lat, c[0].ToDouble(), c[1].ToDouble()));
        }).ToList();
    }

    private static double HaversineMeters(double lng1, double lat1, double lng2, double lat2)
    {
        const double earthRadius = 6_371_008.8;
        static double Rad(double d) => d * Math.PI / 180;
        var a = Math.Pow(Math.Sin(Rad(lat2 - lat1) / 2), 2)
            + (Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) * Math.Pow(Math.Sin(Rad(lng2 - lng1) / 2), 2));
        return 2 * earthRadius * Math.Asin(Math.Sqrt(a));
    }
}
