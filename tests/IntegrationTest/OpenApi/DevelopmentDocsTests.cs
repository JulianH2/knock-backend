using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.IntegrationTest.OpenApi;

public class DevelopmentDocsTests
{
    [Fact]
    public async Task OpenApiAndScalar_StillServe_WithSecurityHeaders_TC_TT_API_06_18()
    {
        using var factory = new KnockApiFactory();
        using var client = factory.CreateClient();

        using var openApi = await TestHttp.GetAsync(client, "/openapi/v1.json");
        using var scalar = await TestHttp.GetAsync(client, "/scalar/v1");
        using var api = await TestHttp.GetAsync(client, "/api/v1/test/ping");

        openApi.StatusCode.Should().Be(HttpStatusCode.OK);
        scalar.StatusCode.Should().Be(HttpStatusCode.OK);
        foreach (var docs in new[] { openApi, scalar })
        {
            SecurityHttp.HeaderValues(docs, "X-Content-Type-Options").Should().ContainSingle().Which.Should().Be("nosniff");
            SecurityHttp.HeaderValues(docs, "X-Frame-Options").Should().ContainSingle().Which.Should().Be("DENY");
            SecurityHttp.HeaderValues(docs, "Referrer-Policy").Should().ContainSingle().Which.Should().Be("no-referrer");
            SecurityHttp.HeaderValues(docs, "Content-Security-Policy").Should().BeEmpty("la CSP estricta no se aplica a la documentación en Development");
        }

        // El resto de la API sí lleva la CSP estricta: la excepción no se extiende.
        SecurityHttp.ShouldCarrySecurityHeaders(api, "fuera de la documentación");
    }
}
