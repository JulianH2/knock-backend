using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.IntegrationTest.Hosting;

public class HstsTests
{
    private const string _hsts = "Strict-Transport-Security";

    [Fact]
    public async Task HttpsRequestInProduction_CarriesHsts_NoPreload_TC_TT_API_06_05()
    {
        using var factory = new KnockApiFactory(environment: "Production");
        using var client = SecurityHttp.Client(factory);

        using var response = await TestHttp.GetAsync(client, "/api/v1/test/ping");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var value = SecurityHttp.HeaderValues(response, _hsts).Should().ContainSingle().Subject;
        value.Should().Be("max-age=31536000; includeSubDomains");
        value.Should().NotContain("preload");

        // El valor sale de Security:Hsts, no del código.
        var settings = new Dictionary<string, string?> { ["Security:Hsts:MaxAge"] = "00:10:00", ["Security:Hsts:IncludeSubDomains"] = "false" };
        using var custom = new KnockApiFactory(environment: "Production", settings: settings);
        using var customClient = SecurityHttp.Client(custom);
        using var customResponse = await TestHttp.GetAsync(customClient, "/api/v1/test/ping");
        SecurityHttp.HeaderValues(customResponse, _hsts).Should().ContainSingle().Which.Should().Be("max-age=600");
    }

    [Fact]
    public async Task NoHstsInDevelopmentNorOverPlainHttp_TC_TT_API_06_06()
    {
        using var development = new KnockApiFactory();
        using var developmentClient = SecurityHttp.Client(development);
        using var inDevelopment = await TestHttp.GetAsync(developmentClient, "/api/v1/test/ping");

        using var production = new KnockApiFactory(environment: "Production");
        using var plainClient = SecurityHttp.Client(production, "http://api.example.test");
        using var overHttp = await TestHttp.GetAsync(plainClient, "/api/v1/test/ping");

        SecurityHttp.HeaderValues(inDevelopment, _hsts).Should().BeEmpty("en Development no hay HSTS aunque sea HTTPS");
        SecurityHttp.HeaderValues(overHttp, _hsts).Should().BeEmpty("por HTTP plano no hay HSTS");
    }
}
