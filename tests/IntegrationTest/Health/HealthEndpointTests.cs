using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.IntegrationTest.Health;

public class HealthEndpointTests
{
    [Fact]
    public async Task Get_Health_Returns200_TC_TT_API_01_01()
    {
        using var factory = new KnockApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_HealthReady_Returns200_TC_TT_API_01_02()
    {
        await ReadinessChecks.AssertReadyReturns200Async();
    }

    [Fact]
    public async Task Get_HealthReady_BlobNotConfigured_Returns503_TC_TT_API_01_03()
    {
        var mongo = await DocumentDbFixture.SharedAsync();
        using var factory = new KnockApiFactory(
            settings: new Dictionary<string, string?> { ["ConnectionStrings:KnockMongo"] = mongo.ConnectionString });
        using var client = factory.CreateClient();

        using var ready = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));

        ready.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable, "con MongoDB disponible, solo Blob (sin configurar) impide readiness");
    }

    [Fact]
    public async Task Get_HealthReady_FailingCheck_Returns503_TC_TT_API_01_03()
    {
        using var factory = new KnockApiFactory(failingReadinessCheck: true);
        using var client = factory.CreateClient();

        using var ready = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));
        using var live = await client.GetAsync(new Uri("/health", UriKind.Relative));

        ready.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        live.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
