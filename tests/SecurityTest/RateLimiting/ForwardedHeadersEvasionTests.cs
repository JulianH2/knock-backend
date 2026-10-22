using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.SecurityTest.RateLimiting;

public class ForwardedHeadersEvasionTests
{
    private const string _ping = "/api/v1/test/ping";
    private const string _proxy = "203.0.113.60";

    [Fact]
    public async Task SpoofedForwardedFor_DoesNotEvadeLimit_TC_TT_API_03_06()
    {
        using var factory = new KnockApiFactory(settings: TestHttp.Limits(globalPermits: 3, authPermits: 1000));
        using var client = factory.CreateClient();
        var statuses = new List<HttpStatusCode>();

        for (var i = 1; i <= 5; i++)
        {
            var headers = new Dictionary<string, string>
            {
                ["X-Forwarded-For"] = $"10.20.30.{i}",
                ["Forwarded"] = $"for=10.20.40.{i}",
            };
            using var response = await TestHttp.GetAsync(client, _ping, _proxy, headers);
            statuses.Add(response.StatusCode);
        }

        statuses.Take(3).Should().OnlyContain(s => s == HttpStatusCode.OK);
        statuses.Skip(3).Should().OnlyContain(s => s == HttpStatusCode.TooManyRequests, "la partición es la IP de la conexión, no la del encabezado");
    }

    [Fact]
    public async Task TrustedProxy_UsesTheForwardedClientIp_TC_TT_API_03_06()
    {
        var settings = TestHttp.Limits(globalPermits: 2, authPermits: 1000);
        settings["ForwardedHeaders:KnownProxies:0"] = _proxy;
        using var factory = new KnockApiFactory(settings: settings);
        using var client = factory.CreateClient();
        var clientA = new Dictionary<string, string> { ["X-Forwarded-For"] = "198.51.100.71" };
        var clientB = new Dictionary<string, string> { ["X-Forwarded-For"] = "198.51.100.72" };

        for (var i = 0; i < 2; i++)
        {
            using var accepted = await TestHttp.GetAsync(client, _ping, _proxy, clientA);
            accepted.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using var limitedA = await TestHttp.GetAsync(client, _ping, _proxy, clientA);
        limitedA.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        using var otherClient = await TestHttp.GetAsync(client, _ping, _proxy, clientB);
        otherClient.StatusCode.Should().Be(HttpStatusCode.OK, "con proxy de confianza cada IP reenviada tiene su propio contador");
    }

    [Fact]
    public async Task TrustedNetwork_UsesTheForwardedClientIp_TC_TT_API_03_06()
    {
        var settings = TestHttp.Limits(globalPermits: 1, authPermits: 1000);
        settings["ForwardedHeaders:KnownNetworks:0"] = "203.0.113.0/24";
        using var factory = new KnockApiFactory(settings: settings);
        using var client = factory.CreateClient();

        using var firstClient = await TestHttp.GetAsync(client, _ping, _proxy, new Dictionary<string, string> { ["X-Forwarded-For"] = "198.51.100.81" });
        using var secondClient = await TestHttp.GetAsync(client, _ping, _proxy, new Dictionary<string, string> { ["X-Forwarded-For"] = "198.51.100.82" });
        using var firstAgain = await TestHttp.GetAsync(client, _ping, _proxy, new Dictionary<string, string> { ["X-Forwarded-For"] = "198.51.100.81" });

        firstClient.StatusCode.Should().Be(HttpStatusCode.OK);
        secondClient.StatusCode.Should().Be(HttpStatusCode.OK, "cada IP reenviada por una red de confianza tiene su contador");
        firstAgain.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task ProxyThatIsNotTheConfiguredOne_IsNotTrusted_TC_TT_API_03_06()
    {
        var settings = TestHttp.Limits(globalPermits: 2, authPermits: 1000);
        settings["ForwardedHeaders:KnownProxies:0"] = "203.0.113.99";
        using var factory = new KnockApiFactory(settings: settings);
        using var client = factory.CreateClient();
        var statuses = new List<HttpStatusCode>();

        for (var i = 1; i <= 4; i++)
        {
            using var response = await TestHttp.GetAsync(client, _ping, _proxy, new Dictionary<string, string> { ["X-Forwarded-For"] = $"198.51.100.{i}" });
            statuses.Add(response.StatusCode);
        }

        statuses.Should().Equal(HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.TooManyRequests, HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task NoConfiguredProxies_IgnoresForwardedFor_EvenFromLoopback_TC_TT_API_03_06()
    {
        using var factory = new KnockApiFactory(settings: TestHttp.Limits(globalPermits: 2, authPermits: 1000));
        using var client = factory.CreateClient();
        var statuses = new List<HttpStatusCode>();

        for (var i = 1; i <= 4; i++)
        {
            using var response = await TestHttp.GetAsync(client, _ping, "127.0.0.1", new Dictionary<string, string> { ["X-Forwarded-For"] = $"198.51.100.{i}" });
            statuses.Add(response.StatusCode);
        }

        statuses.Should().Equal(HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.TooManyRequests, HttpStatusCode.TooManyRequests);
    }
}
