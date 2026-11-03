using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.IntegrationTest.Hosting;

public class CorsTests
{
    private static readonly Dictionary<string, string> _panelOrigin = new() { ["Origin"] = SecurityHttp.PanelOrigin };

    [Fact]
    public async Task AllowedOrigin_GetsExactOriginAndNoCredentials_TC_TT_API_06_09()
    {
        using var factory = new KnockApiFactory(settings: SecurityHttp.WithCors(SecurityHttp.PanelOrigin));
        using var client = factory.CreateClient();

        using var response = await TestHttp.GetAsync(client, "/api/v1/test/ping", headers: _panelOrigin);

        SecurityHttp.HeaderValues(response, "Access-Control-Allow-Origin").Should().ContainSingle().Which.Should().Be(SecurityHttp.PanelOrigin, "el origen exacto, nunca *");
        SecurityHttp.HeaderValues(response, "Vary").Should().Contain(value => value.Contains("Origin", StringComparison.Ordinal));
        var exposed = SecurityHttp.HeaderValues(response, "Access-Control-Expose-Headers").Should().ContainSingle().Subject;
        exposed.Split(',', StringSplitOptions.TrimEntries).Should().BeEquivalentTo("X-Correlation-Id", "Retry-After", "ETag");
        SecurityHttp.HeaderValues(response, "Access-Control-Allow-Credentials").Should().BeEmpty();
    }

    [Fact]
    public async Task Preflight_IsAnsweredWithTheConfiguredLists_TC_TT_API_06_10()
    {
        using var factory = new KnockApiFactory(settings: SecurityHttp.WithCors(SecurityHttp.PanelOrigin));
        using var client = factory.CreateClient();

        // Ruta que solo acepta POST y exige sesión en el resto de la API: el preflight no pide autenticación ni da 405.
        using var preflight = await SendPreflightAsync(client, "/api/v1/auth/firebase", "POST", "authorization, content-type");
        using var protectedRoute = await SendPreflightAsync(client, "/api/v1/me", "GET", "authorization");

        foreach (var response in new[] { preflight, protectedRoute })
        {
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            SecurityHttp.HeaderValues(response, "Access-Control-Allow-Origin").Should().ContainSingle().Which.Should().Be(SecurityHttp.PanelOrigin);
            SecurityHttp.HeaderValues(response, "Access-Control-Allow-Methods").Should().ContainSingle().Which.Should().Be("GET, POST, PUT, PATCH, DELETE");
            SecurityHttp.HeaderValues(response, "Access-Control-Allow-Headers").Should().ContainSingle().Which.Should().Be("Authorization, Content-Type, X-Correlation-Id, Idempotency-Key, If-Match");
            SecurityHttp.HeaderValues(response, "Access-Control-Max-Age").Should().ContainSingle().Which.Should().Be("600");
            SecurityHttp.HeaderValues(response, "Access-Control-Allow-Credentials").Should().BeEmpty();
        }

        using var wrongMethod = await SendPreflightAsync(client, "/api/v1/me", "TRACE", "authorization");
        using var wrongHeader = await SendPreflightAsync(client, "/api/v1/me", "GET", "authorization, x-evil");
        foreach (var denied in new[] { wrongMethod, wrongHeader })
        {
            denied.Headers.Where(h => h.Key.StartsWith("Access-Control-Allow-", StringComparison.OrdinalIgnoreCase)).Should().BeEmpty("fuera de la lista no se concede");
        }
    }

    [Fact]
    public async Task ErrorResponses_AlsoCarryCorsForTheAllowedOrigin_TC_TT_API_06_12()
    {
        var settings = SecurityHttp.Merge(SecurityHttp.WithCors(SecurityHttp.PanelOrigin), TestHttp.Limits(globalPermits: 1, authPermits: 1000));
        using var factory = new KnockApiFactory(settings: settings);
        using var client = factory.CreateClient();
        var layers = await SecurityHttp.LayersAsync(client, _panelOrigin);

        try
        {
            foreach (var (layer, expected, response) in layers.Where(l => l.Expected is HttpStatusCode.Unauthorized or (HttpStatusCode)422 or HttpStatusCode.TooManyRequests or HttpStatusCode.InternalServerError))
            {
                response.StatusCode.Should().Be(expected, layer);
                SecurityHttp.HeaderValues(response, "Access-Control-Allow-Origin").Should().ContainSingle().Which.Should().Be(SecurityHttp.PanelOrigin, $"{layer}: el panel debe poder leer el error");
            }
        }
        finally
        {
            SecurityHttp.Dispose(layers);
        }
    }

    private static async Task<HttpResponseMessage> SendPreflightAsync(HttpClient client, string path, string method, string requestHeaders)
    {
        var headers = new Dictionary<string, string>
        {
            ["Origin"] = SecurityHttp.PanelOrigin,
            ["Access-Control-Request-Method"] = method,
            ["Access-Control-Request-Headers"] = requestHeaders,
        };
        return await TestHttp.SendAsync(client, HttpMethod.Options, path, headers: headers);
    }
}
