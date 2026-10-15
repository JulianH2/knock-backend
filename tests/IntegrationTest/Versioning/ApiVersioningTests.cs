using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.IntegrationTest.Versioning;

public class ApiVersioningTests
{
    [Fact]
    public async Task V1Route_ResolvesVersion1_TC_TT_API_01_10()
    {
        using var factory = new KnockApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/v1/test/ping", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("api-supported-versions").Should().Contain("1.0");
    }

    [Fact]
    public async Task UnknownVersionRoute_IsNotServed_TC_TT_API_01_10()
    {
        using var factory = new KnockApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/v9/test/ping", UriKind.Relative));

        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }
}
