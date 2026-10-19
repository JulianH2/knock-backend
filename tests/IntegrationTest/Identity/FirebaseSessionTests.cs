using System.Net;
using AwesomeAssertions;
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
}
