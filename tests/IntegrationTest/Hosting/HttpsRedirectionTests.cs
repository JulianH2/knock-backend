using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.IntegrationTest.Hosting;

public class HttpsRedirectionTests
{
    private const string _redirectPort = "Security:Https:RedirectPort";
    private const string _plainHost = "http://api.example.test";

    [Fact]
    public async Task RedirectsOnlyWhenConfiguredAndNeverLoopsBehindTheIngress_TC_TT_API_06_08()
    {
        var settings = SecurityHttp.Merge(SecurityHttp.WithTrustedProxy(), new Dictionary<string, string?> { [_redirectPort] = "8443" });
        using var configured = new KnockApiFactory(environment: "Production", settings: settings);
        using var client = SecurityHttp.Client(configured, _plainHost);

        // HTTP directo (sin proxy de confianza): 308 hacia HTTPS con la misma ruta y query, y con los encabezados de seguridad.
        using var direct = await TestHttp.GetAsync(client, "/api/v1/test/ping?x=1", SecurityHttp.UntrustedIp);
        direct.StatusCode.Should().Be(HttpStatusCode.PermanentRedirect);
        direct.Headers.Location?.ToString().Should().Be("https://api.example.test:8443/api/v1/test/ping?x=1");
        SecurityHttp.ShouldCarrySecurityHeaders(direct, "la redirección");

        // Por el proxy de confianza con X-Forwarded-Proto: https: la API ya ve HTTPS y no redirige (sin bucle).
        var forwarded = new Dictionary<string, string> { ["X-Forwarded-Proto"] = "https" };
        using var behindIngress = await TestHttp.GetAsync(client, "/api/v1/test/ping?x=1", SecurityHttp.TrustedProxyIp, forwarded);
        behindIngress.StatusCode.Should().Be(HttpStatusCode.OK);

        // Un cliente cualquiera que dice ser HTTPS no evita la redirección.
        using var spoofed = await TestHttp.GetAsync(client, "/api/v1/test/ping", SecurityHttp.UntrustedIp, forwarded);
        spoofed.StatusCode.Should().Be(HttpStatusCode.PermanentRedirect);
    }

    [Fact]
    public async Task WithoutRedirectPort_NeverRedirectsNorFails_TC_TT_API_06_08()
    {
        using var factory = new KnockApiFactory(environment: "Production");
        using var client = SecurityHttp.Client(factory, _plainHost);

        using var response = await TestHttp.GetAsync(client, "/api/v1/test/ping");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "sin RedirectPort (Container Apps) no se redirige");
    }

    [Fact]
    public async Task InDevelopment_NeverRedirects_TC_TT_API_06_08()
    {
        using var factory = new KnockApiFactory(settings: new Dictionary<string, string?> { [_redirectPort] = "8443" });
        using var client = SecurityHttp.Client(factory, _plainHost);

        using var response = await TestHttp.GetAsync(client, "/api/v1/test/ping");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
