using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.IntegrationTest.Catalog;

public sealed class NeighborhoodAtPointRateLimitTests
{
    private const int _permits = 3;

    [Fact]
    public async Task GlobalLimitAppliesToPointLookup_Returns429WithoutCallingCatalog_TC_TT_API_07_11()
    {
        var catalog = new CountingNeighborhoodLocator();
        using var factory = catalog.CreateFactory(settings: TestHttp.Limits(globalPermits: _permits, authPermits: 1000, window: "00:01:00"));
        using var client = factory.CreateClient();
        var query = PointLookupHttp.Query("20.0505", "-99.3495");

        for (var i = 1; i <= _permits; i++)
        {
            using var accepted = await PointLookupHttp.GetAsync(factory, client, query, "198.51.100.71");
            accepted.StatusCode.Should().Be(HttpStatusCode.OK, $"la petición {i} está dentro del límite");
        }

        using var limited = await PointLookupHttp.GetAsync(factory, client, query, "198.51.100.71");
        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests, "la ruta no está exenta del límite global");
        limited.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        limited.Headers.RetryAfter.Should().NotBeNull();
        catalog.Calls.Should().Be(_permits, "el 429 sale antes de llamar al catálogo");

        using var otherIp = await PointLookupHttp.GetAsync(factory, client, query, "198.51.100.72");
        otherIp.StatusCode.Should().Be(HttpStatusCode.OK, "otra IP no comparte el contador");
    }
}
