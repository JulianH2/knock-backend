using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using MongoDB.Driver;
using Xunit;

namespace Knock.SecurityTest.Catalog;

public sealed class NeighborhoodAtPointFailureTests
{
    private const string _lat = "20.0512347";
    private const string _lon = "-99.3456781";
    private static readonly string[] _forms = ["20.0512347", "99.3456781", "20.0512", "99.3457"];

    [Fact]
    public async Task CatalogUnavailable_Returns500WithoutCoordinatesOrConnectionDetails_TC_TT_API_07_12()
    {
        await using var mongo = await DocumentDbFixture.StartAsync();
        var url = new MongoUrlBuilder(mongo.ConnectionString) { ServerSelectionTimeout = TimeSpan.FromSeconds(2) };
        var host = $"{url.Server.Host}:{url.Server.Port}";
        string body = string.Empty;
        HttpStatusCode status = default;
        string? mediaType = null;
        var output = await ConsoleCapture.RunAsync(async () =>
        {
            var (factory, _) = await PointLookupHttp.CreateSeededFactoryAsync(url.ToString(), "Production");
            using var owned = factory;
            using var client = factory.CreateClient();
            await mongo.StopAsync();

            using var response = await PointLookupHttp.GetAsync(factory, client, PointLookupHttp.Query(_lat, _lon));
            status = response.StatusCode;
            mediaType = response.Content.Headers.ContentType?.MediaType;
            body = await response.Content.ReadAsStringAsync();
        });

        status.Should().Be(HttpStatusCode.InternalServerError);
        mediaType.Should().Be("application/problem+json");
        output.Should().Contain("\"StatusCode\":500", "el error se registró; si no, la prueba del log pasaría en vacío");
        body.Should().NotContainEquivalentOf("stackTrace").And.NotContain("at Knock.").And.NotContain("Mongo");
        body.Should().NotContain(url.Password).And.NotContain(url.Username + ":").And.NotContain(host).And.NotContain("mongodb://");
        output.Should().NotContain(url.Password, "el log no lleva la cadena de conexión");
        output.Should().NotContain(host, "ni el host del servidor");
        foreach (var form in _forms)
        {
            body.Should().NotContain(form);
            output.Should().NotContain(form, "el log del error no lleva las coordenadas");
        }
    }
}
