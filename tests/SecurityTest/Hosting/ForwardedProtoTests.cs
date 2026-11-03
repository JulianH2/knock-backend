using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.SecurityTest.Hosting;

public class ForwardedProtoTests
{
    private const string _hsts = "Strict-Transport-Security";
    private const string _plainHost = "http://api.example.test";

    private static readonly Dictionary<string, string> _claimsHttps = new() { ["X-Forwarded-Proto"] = "https" };

    [Fact]
    public async Task OnlyTrustedProxiesCanSetTheScheme_TC_TT_API_06_07()
    {
        using var factory = new KnockApiFactory(environment: "Production", settings: SecurityHttp.WithTrustedProxy());
        using var client = SecurityHttp.Client(factory, _plainHost);

        using var fromProxy = await TestHttp.GetAsync(client, "/api/v1/test/ping", SecurityHttp.TrustedProxyIp, _claimsHttps);
        using var fromAnyone = await TestHttp.GetAsync(client, "/api/v1/test/ping", SecurityHttp.UntrustedIp, _claimsHttps);
        using var standardHeader = await TestHttp.GetAsync(
            client, "/api/v1/test/ping", SecurityHttp.UntrustedIp, new Dictionary<string, string> { ["Forwarded"] = "proto=https" });
        using var standardHeaderFromProxy = await TestHttp.GetAsync(
            client, "/api/v1/test/ping", SecurityHttp.TrustedProxyIp, new Dictionary<string, string> { ["Forwarded"] = "proto=https" });

        SecurityHttp.HeaderValues(fromProxy, _hsts).Should().NotBeEmpty("el proxy de confianza hace que la API vea HTTPS");
        SecurityHttp.HeaderValues(fromAnyone, _hsts).Should().BeEmpty("quien no es proxy de confianza no fija el esquema");
        SecurityHttp.HeaderValues(standardHeader, _hsts).Should().BeEmpty();
        SecurityHttp.HeaderValues(standardHeaderFromProxy, _hsts).Should().BeEmpty("solo se procesa X-Forwarded-*, no Forwarded");
    }

    [Fact]
    public async Task WithoutConfiguredProxies_NobodyIsTrusted_TC_TT_API_06_07()
    {
        using var factory = new KnockApiFactory(environment: "Production");
        using var client = SecurityHttp.Client(factory, _plainHost);

        using var response = await TestHttp.GetAsync(client, "/api/v1/test/ping", SecurityHttp.TrustedProxyIp, _claimsHttps);

        SecurityHttp.HeaderValues(response, _hsts).Should().BeEmpty("sin proxies configurados el reenvío está en None");
    }
}
