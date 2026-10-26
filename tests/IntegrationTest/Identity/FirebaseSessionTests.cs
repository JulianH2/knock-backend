using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Knock.Api.Modules.Identity.Services;
using Knock.IntegrationTest.Support;
using Microsoft.IdentityModel.JsonWebTokens;
using Xunit;

namespace Knock.IntegrationTest.Identity;

public class FirebaseSessionTests
{
    // Número ficticio de la numeración reservada para ejemplos; solo existe en el emulador.
    private const string _testPhone = "+15555550101";

    [Fact]
    public async Task ExchangeValidIdToken_Returns200WithSession_TC_TT_API_02_01()
    {
        var (idToken, _) = await FirebaseEmulator.SignInWithPhoneAsync(_testPhone);
        using var factory = new KnockApiFactory();
        using var client = factory.CreateClient();

        using var response = await AuthHttp.ExchangeAsync(client, idToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var (accessToken, refreshToken) = await AuthHttp.ReadSessionAsync(response);
        accessToken.Should().NotBeNullOrWhiteSpace();
        refreshToken.Should().NotBeNullOrWhiteSpace();
        accessToken.Should().NotBe(refreshToken);
    }

    [Fact]
    public async Task SameUidTwice_ReturnsSameSub_TC_TT_API_02_09()
    {
        var (idToken, uid) = await FirebaseEmulator.SignInWithPhoneAsync(_testPhone);
        using var factory = new KnockApiFactory();
        using var client = factory.CreateClient();

        using var first = await AuthHttp.ExchangeAsync(client, idToken);
        using var second = await AuthHttp.ExchangeAsync(client, idToken);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var handler = new JsonWebTokenHandler();
        var firstSession = await AuthHttp.ReadSessionAsync(first);
        var secondSession = await AuthHttp.ReadSessionAsync(second);
        var firstSub = handler.ReadJsonWebToken(firstSession.AccessToken).GetClaim("sub").Value;
        var secondSub = handler.ReadJsonWebToken(secondSession.AccessToken).GetClaim("sub").Value;
        firstSub.Should().Be(secondSub);
        firstSub.Should().NotBe(uid, "el sub es el GUID derivado, no el uid de Firebase");
        Guid.TryParse(firstSub, out _).Should().BeTrue();
        firstSession.AccessToken.Should().NotBe(secondSession.AccessToken, "cada canje emite un jti nuevo");
    }

    [Fact]
    public async Task IssuedSubIsTheUuidV8OfTheUid_TC_TT_API_12_05()
    {
        var (idToken, uid) = await FirebaseEmulator.SignInWithPhoneAsync(_testPhone);
        using var factory = new KnockApiFactory();
        using var client = factory.CreateClient();

        using var first = await AuthHttp.ExchangeAsync(client, idToken);
        using var second = await AuthHttp.ExchangeAsync(client, idToken);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var body = JsonDocument.Parse(await first.Content.ReadAsStringAsync()))
        {
            body.RootElement.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(
                ["accessToken", "refreshToken"], "el contrato de la respuesta no cambia");
        }

        var firstSession = await AuthHttp.ReadSessionAsync(first);
        var secondSession = await AuthHttp.ReadSessionAsync(second);
        var handler = new JsonWebTokenHandler();
        var sub = handler.ReadJsonWebToken(firstSession.AccessToken).GetClaim("sub").Value;
        var expected = UserIdDerivation.Derive(uid).ToString("D");

        sub.Should().Be(expected, "el sub es el UUID v8 derivado del uid");
        sub[14].Should().Be('8', "el dígito de versión es 8");
        sub.Should().NotBe(uid).And.NotContain(uid);
        handler.ReadJsonWebToken(secondSession.AccessToken).GetClaim("sub").Value.Should().Be(sub, "el mismo usuario da el mismo sub");

        using var me = await AuthHttp.MeAsync(client, firstSession.AccessToken);
        me.StatusCode.Should().Be(HttpStatusCode.OK);
        (await me.Content.ReadAsStringAsync()).Should().Contain(expected, "GET /api/v1/me devuelve el mismo valor como id");
    }
}
