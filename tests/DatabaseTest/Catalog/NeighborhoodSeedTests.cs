using AwesomeAssertions;
using Knock.Api.Modules.Catalog;
using Knock.Api.Modules.Catalog.Domain;
using Knock.Api.Modules.Catalog.Services;
using Knock.DatabaseTest.Support;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.DatabaseTest.Catalog;

public sealed class NeighborhoodSeedTests
{
    /// <summary>Corrimiento sintético (~30 m) para probar puntos junto a la ubicación aproximada.</summary>
    private const double _offset = 0.0003;

    [Fact]
    public async Task SeedLoadsTwelveNeighborhoodsWithFixedIdsAndPolygonlessOnesOnlyByPostalCode_TC_TT_BD_06_11()
    {
        await using var first = await CatalogDatabase.CreateAsync(seeded: true);
        await using var second = await CatalogDatabase.CreateAsync(seeded: true);

        var firstIds = await AssertSeededNeighborhoodsAsync(first);
        var secondIds = await AssertSeededNeighborhoodsAsync(second);

        secondIds.Should().Equal(firstIds, "los _id fijos son iguales en cualquier base");
    }

    private static async Task<List<(string Nombre, ObjectId Id)>> AssertSeededNeighborhoodsAsync(CatalogDatabase database)
    {
        var colonias = await database.Colonias.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        var extract = (await File.ReadAllLinesAsync(Path.Combine(CatalogTestDatabase.SeedsDirectory, "..", "sepomex", "tula_colonias.csv")))
            .Skip(1)
            .Select(line => line.Split(','))
            .Select(row => (Cp: row[0], Nombre: row[1], Municipio: row[3]));

        colonias.Select(c => (c["cp"].AsString, c["nombre"].AsString, c["municipio"].AsString)).Should().BeEquivalentTo(extract);
        foreach (var colonia in colonias)
        {
            var nombre = colonia["nombre"].AsString;
            var json = CatalogSeedData.Colonia(nombre);
            colonia["_id"].Should().Be(json["_id"]);
            colonia["nombreBusqueda"].AsString.Should().Be(SearchNameNormalizer.Normalize(nombre));
            if (CatalogSeedData.WithoutPolygon.Contains(nombre))
            {
                colonia["piloto"].AsBoolean.Should().BeFalse();
                colonia.Names.Should().NotContain(["geom", "centro"], $"{nombre} no tiene polígono: los campos no existen (no null)");
                continue;
            }

            colonia["piloto"].AsBoolean.Should().BeTrue();
            colonia["centro"].Should().Be(json["centro"], "el centro es el exportado de PostGIS");
            var centerInsideOwnPolygon = new BsonDocument
            {
                { "_id", colonia["_id"] },
                { "centro", new BsonDocument("$geoWithin", new BsonDocument("$geometry", colonia["geom"])) },
            };
            (await database.Colonias.CountDocumentsAsync(centerInsideOwnPolygon)).Should().Be(1, $"el centro de {nombre} cae dentro de su polígono");
        }

        colonias.Count(c => c["piloto"].AsBoolean).Should().Be(8);
        var byPostalCode = await database.InScopeAsync<ICatalogApi, IReadOnlyList<string>>(
            catalog => catalog.FindNeighborhoodsByPostalCodeAsync("42803", CancellationToken.None));
        byPostalCode.Should().Contain(CatalogSeedData.WithoutPolygon);

        foreach (var (_, longitude, latitude) in CatalogSeedData.PolygonlessApproximateLocations)
        {
            foreach (var (dLongitude, dLatitude) in new[] { (0d, 0d), (_offset, 0d), (-_offset, 0d), (0d, _offset), (0d, -_offset) })
            {
                var lat = latitude + dLatitude;
                var lon = longitude + dLongitude;
                var name = await database.InScopeAsync<ICatalogApi, string?>(catalog => catalog.FindNeighborhoodAtAsync(lat, lon, CancellationToken.None));
                var found = await database.Get<NeighborhoodLocator>().FindAtAsync(lat, lon, CancellationToken.None);

                CatalogSeedData.WithoutPolygon.Should().NotContain(name);
                CatalogSeedData.WithoutPolygon.Should().NotContain(found?.Nombre);
            }
        }

        return colonias.Select(c => (c["nombre"].AsString, c["_id"].AsObjectId)).OrderBy(c => c.Item1, StringComparer.Ordinal).ToList();
    }
}
