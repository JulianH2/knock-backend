using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.IntegrationTest.Hosting;

public class SecurityHeadersTests
{
    [Fact]
    public async Task OkResponse_CarriesTheSecurityHeaders_TC_TT_API_06_01()
    {
        using var factory = new KnockApiFactory();
        using var client = factory.CreateClient();

        using var response = await TestHttp.GetAsync(client, "/api/v1/test/ping");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        SecurityHttp.ShouldCarrySecurityHeaders(response, "una respuesta 200");
    }

    [Fact]
    public async Task ResponsesFromAnyLayer_CarryTheSecurityHeaders_TC_TT_API_06_02()
    {
        // Límite global de 1 por IP: cada capa usa su propia IP y solo la del 429 se repite.
        using var factory = new KnockApiFactory(settings: TestHttp.Limits(globalPermits: 1, authPermits: 1000));
        using var client = factory.CreateClient();
        var layers = await SecurityHttp.LayersAsync(client);

        try
        {
            foreach (var (layer, expected, response) in layers)
            {
                response.StatusCode.Should().Be(expected, layer);
                SecurityHttp.ShouldCarrySecurityHeaders(response, layer);
            }
        }
        finally
        {
            SecurityHttp.Dispose(layers);
        }
    }

    [Fact]
    public async Task CacheControl_DefaultsToNoStoreButRespectsTheEndpoint_TC_TT_API_06_03()
    {
        using var factory = new KnockApiFactory();
        using var client = factory.CreateClient();

        using var withoutOwn = await TestHttp.GetAsync(client, "/api/v1/test/ping");
        using var withOwn = await TestHttp.GetAsync(client, "/api/v1/test/cached");
        using var error = await TestHttp.GetAsync(client, "/api/v1/me");

        SecurityHttp.HeaderValues(withoutOwn, "Cache-Control").Should().ContainSingle().Which.Should().Be("no-store", "la respuesta no definía Cache-Control");
        var own = SecurityHttp.HeaderValues(withOwn, "Cache-Control").Should().ContainSingle("no se duplica").Subject;
        own.Split(',', StringSplitOptions.TrimEntries).Should().BeEquivalentTo("private", "no-cache");
        own.Should().NotContain("no-store", "se respeta el del endpoint");
        SecurityHttp.HeaderValues(error, "Cache-Control").Should().ContainSingle().Which.Should().Be("no-store", "los errores tampoco se cachean");
    }
}
