using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.SecurityTest.Hosting;

public class CorsPolicyTests
{
    /// <summary>Orígenes que se parecen al permitido (<c>https://panel.example.test</c>) pero no lo son.</summary>
    private static readonly string[] _lookalikes =
    [
        "null",
        "http://panel.example.test",
        "https://panel.example.test:8443",
        "https://sub.panel.example.test",
        "https://panel.example.test.evil.test",
        "https://evilpanel.example.test",
        "https://PANEL.example.test",
        "https://panel.example.test/",
        "*",
    ];

    private static async Task<List<HttpResponseMessage>> BothRequestsAsync(HttpClient client, string origin)
    {
        var simple = new Dictionary<string, string> { ["Origin"] = origin };
        var preflight = new Dictionary<string, string>
        {
            ["Origin"] = origin,
            ["Access-Control-Request-Method"] = "GET",
            ["Access-Control-Request-Headers"] = "authorization",
        };
        return
        [
            await TestHttp.GetAsync(client, "/api/v1/test/ping", headers: simple),
            await TestHttp.SendAsync(client, HttpMethod.Options, "/api/v1/test/ping", headers: preflight),
        ];
    }

    private static void ShouldHaveNoCors(IEnumerable<HttpResponseMessage> responses, string because)
    {
        foreach (var response in responses)
        {
            response.Headers.Where(h => h.Key.StartsWith("Access-Control-Allow-", StringComparison.OrdinalIgnoreCase))
                .Should().BeEmpty(because);
            SecurityHttp.HeaderValues(response, "Access-Control-Allow-Origin").Should().NotContain("*");
            SecurityHttp.HeaderValues(response, "Access-Control-Allow-Credentials").Should().BeEmpty();
        }
    }

    [Fact]
    public async Task UnlistedOrigins_AndEmptyConfig_GetNoCors_TC_TT_API_06_11()
    {
        using var configured = new KnockApiFactory(settings: SecurityHttp.WithCors(SecurityHttp.PanelOrigin));
        using var configuredClient = configured.CreateClient();
        using var off = new KnockApiFactory();
        using var offClient = off.CreateClient();

        foreach (var origin in _lookalikes)
        {
            var responses = await BothRequestsAsync(configuredClient, origin);
            ShouldHaveNoCors(responses, $"el origen «{origin}» no está en la lista");
            responses.ForEach(r => r.Dispose());
        }

        // CORS apagado (lista vacía): ni siquiera el origen del panel recibe encabezados.
        var offResponses = await BothRequestsAsync(offClient, SecurityHttp.PanelOrigin);
        ShouldHaveNoCors(offResponses, "con la lista vacía CORS está apagado");
        offResponses.ForEach(r => r.Dispose());
    }
}
