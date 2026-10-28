using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Xunit;

namespace Knock.SecurityTest.Catalog;

public sealed class NeighborhoodAtPointLogPrivacyTests
{
    private const string _lat = "20.0512347";
    private const string _lon = "-99.3456781";
    private static readonly string[] _forms = ["20.0512347", "99.3456781", "20.0512", "99.3457"];

    /// <summary>Con los niveles de <c>appsettings.json</c> (Production) y <c>appsettings.Development.json</c> (Development), sin bajarlos.</summary>
    [Theory]
    [InlineData("Production")]
    [InlineData("Development")]
    public async Task Logs_NeverContainCoordinatesOrQueryString_TC_TT_API_07_08(string environment)
    {
        var mongo = await DocumentDbFixture.SharedAsync();
        var statuses = new List<HttpStatusCode>();
        string before = string.Empty, after = string.Empty;
        var output = await ConsoleCapture.RunAsync(async () =>
        {
            var (factory, databaseName) = await PointLookupHttp.CreateSeededFactoryAsync(
                mongo.ConnectionString, environment, TestHttp.Limits(globalPermits: 2, authPermits: 1000, window: "00:01:00"));
            using var _ = factory;
            using var client = factory.CreateClient();
            var database = factory.Services.GetRequiredService<IMongoClient>().GetDatabase(databaseName);
            before = await CatalogTestDatabase.ReadStorageSnapshotAsync(database);

            foreach (var lon in new[] { _lon, "-199.3456781", _lon })
            {
                using var response = await PointLookupHttp.GetAsync(factory, client, PointLookupHttp.Query(_lat, lon), "203.0.113.81");
                statuses.Add(response.StatusCode);
            }

            var failing = new CountingNeighborhoodLocator { Fail = true };
            using var failingFactory = failing.CreateFactory(environment);
            using var failingClient = failingFactory.CreateClient();
            using var error = await PointLookupHttp.GetAsync(failingFactory, failingClient, PointLookupHttp.Query(_lat, _lon));
            statuses.Add(error.StatusCode);

            after = await CatalogTestDatabase.ReadStorageSnapshotAsync(database);
            await factory.Services.GetRequiredService<IMongoClient>().DropDatabaseAsync(databaseName);
        });

        statuses.Should().Equal(HttpStatusCode.OK, (HttpStatusCode)422, HttpStatusCode.TooManyRequests, HttpStatusCode.InternalServerError);
        foreach (var status in new[] { 200, 422, 429, 500 })
        {
            output.Should().Contain($"\"StatusCode\":{status}", $"el log de petición del {status} se capturó; si no, la prueba pasaría en vacío");
        }

        output.Should().Contain("\"RequestPath\":\"/api/v1/colonias/en-punto\"", "se registra la plantilla de la ruta");
        output.Should().NotContain("en-punto?").And.NotContain("lat=").And.NotContain("lon=", "ni la query string");
        foreach (var form in _forms)
        {
            output.Should().NotContain(form, $"ninguna línea de log lleva la coordenada ({environment})");
        }

        after.Should().Be(before, "ninguna colección gana documentos");
    }
}
