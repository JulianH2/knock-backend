using System.Net;
using AwesomeAssertions;
using Knock.Api.Modules.Identity.Services;
using Knock.IntegrationTest.Support;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Knock.SecurityTest.Identity;

/// <summary>HU-004 casos 04, 07 y 12: no se revela si el refresh existía, un access token ajeno no revoca nada y ningún secreto llega a los registros.</summary>
public class LogoutLeakTests
{
    private const string _phone = "+525555550411";
    private const string _otherPhone = "+525555550412";

    private static string RandomToken() => Base64UrlEncoder.Encode(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

    [Fact]
    public async Task UnknownToken_Returns204NoEnumeration_TC_HU_004_04()
    {
        using var factory = await IdentityTestDatabase.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var opened = await SessionFixtures.OpenAsync(client, _phone);
        var before = await SessionFixtures.AllRefreshTokensAsync(factory);

        using var unknown = await AuthHttp.LogoutAsync(client, RandomToken());
        var unknownBody = await unknown.Content.ReadAsStringAsync();
        using var known = await AuthHttp.LogoutAsync(client, opened.RefreshToken);

        unknown.StatusCode.Should().Be(HttpStatusCode.NoContent);
        known.StatusCode.Should().Be(HttpStatusCode.NoContent, "mismo estado exista o no el refresh");
        unknownBody.Should().BeEmpty();
        (await known.Content.ReadAsStringAsync()).Should().Be(unknownBody);
        var afterUnknown = await SessionFixtures.AllRefreshTokensAsync(factory);
        afterUnknown.Should().HaveCount(before.Count, "el desconocido no creó ni borró nada");
    }

    [Fact]
    public async Task MismatchedOwner_RevokesNothing_TC_HU_004_07()
    {
        using var factory = await IdentityTestDatabase.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var ana = await SessionFixtures.OpenAsync(client, _phone);
        var beto = await SessionFixtures.OpenAsync(client, _otherPhone);
        var before = await SessionFixtures.AllRefreshTokensAsync(factory);

        using var response = await AuthHttp.LogoutAsync(client, beto.RefreshToken, ana.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await SessionFixtures.AllRefreshTokensAsync(factory)).Should().BeEquivalentTo(before, "no se revocó ninguna de las dos familias");
        using var anaRenews = await AuthHttp.RefreshAsync(client, ana.RefreshToken, ana.DeviceId);
        using var betoRenews = await AuthHttp.RefreshAsync(client, beto.RefreshToken, beto.DeviceId);
        anaRenews.StatusCode.Should().Be(HttpStatusCode.OK);
        betoRenews.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task NoSecretsInLogs_TC_HU_004_12()
    {
        var secrets = new List<string>();
        var logs = await ConsoleCapture.RunAsync(async () =>
        {
            using var factory = await IdentityTestDatabase.CreateFactoryAsync();
            using var client = factory.CreateClient();
            var opened = await SessionFixtures.OpenAsync(client, _phone);
            var token = new JsonWebTokenHandler().ReadJsonWebToken(opened.AccessToken);
            var unknown = RandomToken();
            secrets.AddRange([opened.AccessToken, opened.RefreshToken, token.GetClaim("sub").Value, token.GetClaim("jti").Value, unknown]);
            foreach (var candidate in new[] { opened.RefreshToken, unknown })
            {
                var fingerprint = RefreshTokenFingerprint.Compute(candidate);
                secrets.AddRange([Convert.ToBase64String(fingerprint), Convert.ToHexStringLower(fingerprint), Convert.ToHexString(fingerprint)]);
            }

            (await AuthHttp.LogoutAsync(client, opened.RefreshToken, opened.AccessToken)).Dispose();

            (await AuthHttp.LogoutAsync(client, opened.RefreshToken)).Dispose();

            (await AuthHttp.LogoutAsync(client, unknown)).Dispose();

            (await AuthHttp.LogoutAsync(client, "no-es-un-token")).Dispose();
        });

        logs.Should().Contain("/api/v1/auth/logout", "la captura debe incluir las peticiones (si no, la prueba sería vacía)");
        secrets.Where(secret => logs.Contains(secret, StringComparison.Ordinal)).Select(secret => secret.Length > 12 ? secret[..12] + "…" : secret)
            .Should().BeEmpty("ningún log del cierre de sesión lleva refresh token, huella, sub, jti ni access token");
    }
}
