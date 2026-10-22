using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.IntegrationTest.RateLimiting;

public class AuthRateLimitTests
{
    private const string _exchange = "/api/v1/auth/firebase";

    [Fact]
    public async Task AuthLimitExceeded_Returns429WithRetryAfter_TC_TT_API_03_03()
    {
        using var factory = new KnockApiFactory(settings: TestHttp.Limits(globalPermits: 1000, authPermits: 10));
        using var client = factory.CreateClient();

        for (var i = 1; i <= 10; i++)
        {
            // Cuerpo vacío: 400 sin llamar a Firebase.
            using var accepted = await TestHttp.PostEmptyAsync(client, _exchange, "203.0.113.10");
            accepted.StatusCode.Should().Be(HttpStatusCode.BadRequest, $"la petición {i} está dentro del límite");
        }

        using var limited = await TestHttp.PostEmptyAsync(client, _exchange, "203.0.113.10");

        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        limited.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        limited.Headers.RetryAfter.Should().NotBeNull();
        limited.Headers.RetryAfter.Delta!.Value.TotalSeconds.Should().BeGreaterThan(0);
        using var body = await TestHttp.ReadJsonAsync(limited);
        body.RootElement.GetProperty("status").GetInt32().Should().Be(429);
    }

    [Fact]
    public async Task AuthLimit_DoesNotLimitOtherEndpoints_TC_TT_API_03_03()
    {
        using var factory = new KnockApiFactory(settings: TestHttp.Limits(globalPermits: 1000, authPermits: 2));
        using var client = factory.CreateClient();
        for (var i = 0; i < 3; i++)
        {
            using var exchange = await TestHttp.PostEmptyAsync(client, _exchange, "203.0.113.11");
        }

        using var ping = await TestHttp.GetAsync(client, "/api/v1/test/ping", "203.0.113.11");

        ping.StatusCode.Should().Be(HttpStatusCode.OK, "la política estricta solo aplica a /auth");
    }
}
