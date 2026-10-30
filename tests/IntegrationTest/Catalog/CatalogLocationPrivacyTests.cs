using System.Globalization;
using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.Api.Modules.Catalog;
using Knock.Api.Modules.Catalog.Services;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Catalog;

public sealed class CatalogLocationPrivacyTests
{
    // Coordenada sintética y reconocible dentro del polígono piloto aproximado de Centro (db/seeds/colonias_tula.geojson).
    private const double _latitude = 20.050517;
    private const double _longitude = -99.349483;
    private const string _canary = "catalog-privacy-capture-canary";

    private static readonly Action<ILogger, Exception?> _logCanary = LoggerMessage.Define(LogLevel.Warning, new EventId(1), _canary);

    [Fact]
    public async Task NeighborhoodLookupDoesNotPersistOrLogCoordinates_TC_TT_BD_06_09()
    {
        var mongo = await DocumentDbFixture.SharedAsync();
        var databaseName = "market_privacy_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(4));
        using var factory = new KnockApiFactory(settings: new Dictionary<string, string?>
        {
            ["ConnectionStrings:KnockMongo"] = mongo.ConnectionString,
            ["Mongo:Databases:Catalog"] = databaseName,
        });
        await CatalogTestDatabase.SeedAsync(factory.Services);
        var client = factory.Services.GetRequiredService<IMongoClient>();
        var database = client.GetDatabase(databaseName);
        var before = await CatalogTestDatabase.ReadStorageSnapshotAsync(database);

        string? fromApi = null;
        string? fromInternal = null;
        var output = await ConsoleCapture.RunAsync(async () =>
        {
            await using var scope = factory.Services.CreateAsyncScope();
            fromApi = await scope.ServiceProvider.GetRequiredService<ICatalogApi>()
                .FindNeighborhoodAtAsync(_latitude, _longitude, CancellationToken.None);
            fromInternal = (await scope.ServiceProvider.GetRequiredService<NeighborhoodLocator>()
                .FindAtAsync(_latitude, _longitude, CancellationToken.None))?.Nombre;
            _logCanary(scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Knock.Test"), null);
        });

        fromApi.Should().Be("Centro");
        fromInternal.Should().Be("Centro");
        output.Should().Contain(_canary, "la captura recibe los logs de la API; si no, la prueba pasaría en vacío");
        output.Should().NotContain(_latitude.ToString("0.######", CultureInfo.InvariantCulture));
        output.Should().NotContain(Math.Abs(_longitude).ToString("0.######", CultureInfo.InvariantCulture));
        client.Settings.ClusterConfigurator.Should().BeNull("sin suscriptores de eventos de comandos que registren su cuerpo");
        (await CatalogTestDatabase.ReadStorageSnapshotAsync(database)).Should().Be(before);
        await client.DropDatabaseAsync(databaseName);
    }
}
