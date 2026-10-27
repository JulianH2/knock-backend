using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Microsoft.IdentityModel.JsonWebTokens;
using Xunit;

namespace Knock.IntegrationTest.Identity;

public class MeEndpointTests
{
    private const string _testPhone = "+15555550102";

    [Fact]
    public async Task Me_WithoutToken_Returns401_TC_TT_API_02_05()
    {
        using var factory = new KnockApiFactory();
        using var client = factory.CreateClient();

        using var response = await AuthHttp.MeAsync(client, accessToken: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Me_WithIssuedToken_Returns200AndInternalControllerIsDiscovered_TC_TT_API_02_06()
    {
        var (idToken, _) = await FirebaseEmulator.SignInWithPhoneAsync(_testPhone);
        using var factory = new KnockApiFactory();
        using var client = factory.CreateClient();
        using var exchange = await AuthHttp.ExchangeAsync(client, idToken);
        var (accessToken, _) = await AuthHttp.ReadSessionAsync(exchange);

        using var response = await AuthHttp.MeAsync(client, accessToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var sub = new JsonWebTokenHandler().ReadJsonWebToken(accessToken).GetClaim("sub").Value;
        body.RootElement.GetProperty("id").GetString().Should().Be(sub);
        body.RootElement.EnumerateObject().Should().HaveCount(1, "la respuesta solo trae el campo id");

        // El controller que atiende la ruta es internal y aun así se descubrió (ControllerFeatureProvider de Hosting/).
        var controllers = typeof(Program).Assembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("Knock.Api.Modules.Identity", StringComparison.Ordinal) == true
                && t.Name.EndsWith("Controller", StringComparison.Ordinal))
            .ToList();
        controllers.Should().NotBeEmpty();
        controllers.Should().OnlyContain(t => !t.IsPublic, "los controllers de Identity son internal");
    }
}
