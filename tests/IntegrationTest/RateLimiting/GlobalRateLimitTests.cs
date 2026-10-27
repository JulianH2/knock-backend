using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.IntegrationTest.RateLimiting;

public class GlobalRateLimitTests
{
    private const string _ping = "/api/v1/test/ping";

    [Fact]
    public async Task GlobalLimitPerIp_Returns429OnlyForTheOffenderAndHealthIsExempt_TC_TT_API_03_04()
    {
        using var factory = new KnockApiFactory(settings: TestHttp.Limits(globalPermits: 5, authPermits: 1000, window: "00:00:03"));
        using var client = factory.CreateClient();

        for (var i = 1; i <= 5; i++)
        {
            using var accepted = await TestHttp.GetAsync(client, _ping, "198.51.100.1");
            accepted.StatusCode.Should().Be(HttpStatusCode.OK, $"la petición {i} está dentro del límite");
        }

        using var limited = await TestHttp.GetAsync(client, _ping, "198.51.100.1");
        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        using var otherIp = await TestHttp.GetAsync(client, _ping, "198.51.100.2");
        otherIp.StatusCode.Should().Be(HttpStatusCode.OK, "otra IP no comparte el contador");

        for (var i = 0; i < 20; i++)
        {
            using var health = await TestHttp.GetAsync(client, "/health", "198.51.100.1");
            health.StatusCode.Should().Be(HttpStatusCode.OK);
            using var ready = await TestHttp.GetAsync(client, "/health/ready", "198.51.100.1");
            ready.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests, "/health/ready está exento del global");
        }

        var recovered = false;
        for (var attempt = 0; attempt < 20 && !recovered; attempt++)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            using var again = await TestHttp.GetAsync(client, _ping, "198.51.100.1");
            recovered = again.StatusCode == HttpStatusCode.OK;
        }

        recovered.Should().BeTrue("la ventana siguiente vuelve a aceptar peticiones");
    }
}
