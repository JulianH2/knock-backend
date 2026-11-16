using System.Net;
using AwesomeAssertions;
using Knock.SecurityTest.Support;
using Xunit;

namespace Knock.SecurityTest.Market;

/// <summary>Spec 010, R-03 y R-06: GET /api/v1/oficios lleva ETag débil y Cache-Control público con revalidación obligatoria (max-age=0, must-revalidate); If-None-Match vigente responde 304 sin cuerpo.</summary>
public class MarketETagTests
{
    private const string _oficios = "/api/v1/oficios";

    [Fact]
    public async Task Oficios_ReturnsWeakETagAndRevalidatingPublicCacheControl_CA_010_03()
    {
        var hosts = await SecurityHosts.SharedAsync();

        using var response = await SecureHttp.SendAsync(hosts.MarketClient, HttpMethod.Get, _oficios);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.ETag.Should().NotBeNull();
        response.Headers.ETag?.IsWeak.Should().BeTrue();
        response.Headers.CacheControl?.Public.Should().BeTrue();
        response.Headers.CacheControl?.MaxAge.Should().Be(TimeSpan.Zero);
        response.Headers.CacheControl?.MustRevalidate.Should().BeTrue();
    }

    [Fact]
    public async Task Oficios_IfNoneMatchReturns304WithoutBody_CA_010_03()
    {
        var hosts = await SecurityHosts.SharedAsync();

        using var first = await SecureHttp.SendAsync(hosts.MarketClient, HttpMethod.Get, _oficios);
        first.Headers.ETag.Should().NotBeNull();
        var etag = first.Headers.ETag.ToString();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_oficios, UriKind.Relative));
        request.Headers.TryAddWithoutValidation("If-None-Match", etag);
        using var notModified = await hosts.MarketClient.SendAsync(request);

        notModified.StatusCode.Should().Be(HttpStatusCode.NotModified);
        (await notModified.Content.ReadAsStringAsync()).Should().BeEmpty();
        notModified.Headers.ETag?.ToString().Should().Be(etag);
    }
}
