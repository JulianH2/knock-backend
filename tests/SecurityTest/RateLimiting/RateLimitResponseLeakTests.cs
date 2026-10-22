using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.SecurityTest.RateLimiting;

public class RateLimitResponseLeakTests
{
    private const string _ip = "203.0.113.70";

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task Response429_DoesNotRevealPartitionOrQuota_TC_TT_API_03_07(string environment)
    {
        using var factory = new KnockApiFactory(environment, settings: TestHttp.Limits(globalPermits: 7, authPermits: 1000));
        using var client = factory.CreateClient();
        for (var i = 0; i < 7; i++)
        {
            using var accepted = await TestHttp.GetAsync(client, "/api/v1/test/ping", _ip);
            accepted.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using var limited = await TestHttp.GetAsync(client, "/api/v1/test/ping", _ip);
        using var unauthorized = await TestHttp.GetAsync(client, "/api/v1/me", "203.0.113.71");

        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        var raw = await limited.Content.ReadAsStringAsync();
        raw.Should().NotContain(_ip).And.NotContain("203.0.113");
        raw.Should().NotContainEquivalentOf("partition").And.NotContainEquivalentOf("permit").And.NotContainEquivalentOf("limit");
        raw.Should().NotContain("global:").And.NotContain("auth:");
        raw.Should().NotContain("unknown");

        // Misma forma que los demás errores: ni un campo más que ProblemDetails + traceId + correlationId.
        using var body = System.Text.Json.JsonDocument.Parse(raw);
        var allowed = new[] { "type", "title", "status", "traceId", "correlationId" };
        body.RootElement.EnumerateObject().Select(p => p.Name).Should().BeSubsetOf(allowed);
        using var reference = await TestHttp.ReadJsonAsync(unauthorized);
        body.RootElement.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(
            reference.RootElement.EnumerateObject().Select(p => p.Name), "mismo conjunto de campos que el 401");

        var headerNames = limited.Headers.Select(h => h.Key).Concat(limited.Content.Headers.Select(h => h.Key)).ToList();
        headerNames.Should().NotContain(n => n.StartsWith("X-RateLimit", StringComparison.OrdinalIgnoreCase)
            || n.StartsWith("RateLimit", StringComparison.OrdinalIgnoreCase));
        limited.Headers.Contains("Retry-After").Should().BeTrue();
        string.Join(";", limited.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}")).Should().NotContain(_ip);
    }
}
