using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.IntegrationTest.Catalog;

public sealed class NeighborhoodAtPointCacheTests
{
    [Fact]
    public async Task SameCoordinatesTwice_HitCatalogTwiceAndResponseIsNoStore_TC_TT_API_07_09()
    {
        var catalog = new CountingNeighborhoodLocator();
        using var factory = catalog.CreateFactory();
        using var client = factory.CreateClient();
        var query = PointLookupHttp.Query("20.0505", "-99.3495");

        using var first = await PointLookupHttp.GetAsync(factory, client, query);
        using var second = await PointLookupHttp.GetAsync(factory, client, query);

        foreach (var response in new[] { first, second })
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Headers.CacheControl?.NoStore.Should().BeTrue();
            response.Headers.CacheControl?.ToString().Should().Be("no-store");
            response.Headers.ETag.Should().BeNull();
            response.Content.Headers.LastModified.Should().BeNull();
        }

        catalog.Calls.Should().Be(2, "sin caché de salida, de memoria ni distribuida por coordenada");
    }
}
