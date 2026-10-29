using System.Globalization;
using AwesomeAssertions;
using Knock.Api.Modules.Catalog;
using Knock.Api.Modules.Catalog.Data;
using Knock.Api.Modules.Catalog.Services;
using Knock.DatabaseTest.Support;
using MongoDB.Bson;
using Xunit;

namespace Knock.DatabaseTest.Catalog;

public sealed class CatalogCoordinatesTests
{
    [Fact]
    public async Task PointUsesLongitudeLatitudeOrderAndRejectsOutOfRangeBeforeQuerying_TC_TT_BD_06_07()
    {
        await using var database = await CatalogDatabase.CreateAsync(seeded: false);
        // Dos cuadrados sintéticos de 0.01°: cada uno cae donde el otro caería con los ejes invertidos.
        await database.InScopeAsync<CatalogSeeder>(seeder => seeder.SeedAsync(
            [],
            [Square("Alfa Sintética", longitude: 10.00, latitude: 20.00), Square("Beta Sintética", longitude: 20.00, latitude: 10.00)],
            CancellationToken.None));

        (await FindAsync(database, latitude: 20.005, longitude: 10.005)).Should().Be("Alfa Sintética");
        (await FindAsync(database, latitude: 10.005, longitude: 20.005)).Should().Be("Beta Sintética");

        foreach (var (latitude, longitude, value) in new[]
        {
            (91d, 0d, 91d), (0d, -181d, -181d), (double.NaN, 0d, double.NaN), (0d, double.NaN, double.NaN),
            (double.PositiveInfinity, 0d, double.PositiveInfinity), (0d, double.NegativeInfinity, double.NegativeInfinity),
        })
        {
            database.ClearCommands();
            var api = () => FindAsync(database, latitude, longitude);
            var locator = () => database.Get<NeighborhoodLocator>().FindAtAsync(latitude, longitude, CancellationToken.None);

            var apiError = await api.Should().ThrowExactlyAsync<ArgumentOutOfRangeException>();
            var locatorError = await locator.Should().ThrowExactlyAsync<ArgumentOutOfRangeException>();
            apiError.Which.Message.Should().NotContain(Math.Abs(value).ToString(CultureInfo.InvariantCulture));
            locatorError.Which.Message.Should().NotContain(Math.Abs(value).ToString(CultureInfo.InvariantCulture));
            database.Commands.Should().BeEmpty("la validación ocurre antes de enviar cualquier comando");
        }
    }

    private static Task<string?> FindAsync(CatalogDatabase database, double latitude, double longitude) =>
        database.InScopeAsync<ICatalogApi, string?>(catalog => catalog.FindNeighborhoodAtAsync(latitude, longitude, CancellationToken.None));

    private static BsonDocument Square(string nombre, double longitude, double latitude)
    {
        const double size = 0.01;
        var ring = new BsonArray
        {
            new BsonArray { longitude, latitude },
            new BsonArray { longitude + size, latitude },
            new BsonArray { longitude + size, latitude + size },
            new BsonArray { longitude, latitude + size },
            new BsonArray { longitude, latitude },
        };
        return new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "nombre", nombre },
            { "cp", "99999" },
            { "municipio", "Sintético" },
            { "piloto", true },
            { "aproximado", true },
            { "geom", new BsonDocument { { "type", "MultiPolygon" }, { "coordinates", new BsonArray { new BsonArray { ring } } } } },
            { "centro", new BsonDocument { { "type", "Point" }, { "coordinates", new BsonArray { longitude + (size / 2), latitude + (size / 2) } } } },
        };
    }
}
