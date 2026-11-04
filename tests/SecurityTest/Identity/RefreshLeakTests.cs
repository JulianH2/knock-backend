using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Knock.Api.Modules.Identity.Services;
using Knock.IntegrationTest.Support;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Knock.SecurityTest.Identity;

/// <summary>HU-003 casos 05 y 12: el rechazo no distingue motivos ni repite el valor, y ningún secreto llega a los registros. Solo datos ficticios.</summary>
public class RefreshLeakTests
{
    private const string _phone = "+525555550311";

    private static string RandomToken() => Base64UrlEncoder.Encode(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

    private static async Task<string> BodyAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(text);
        return string.Join('|', document.RootElement.EnumerateObject()
            .Where(p => p.Name is "type" or "title" or "status" or "detail")
            .Select(p => $"{p.Name}={p.Value}"));
    }

    [Fact]
    public async Task UnknownOrMalformedToken_SameBodyNoEcho_TC_HU_003_05()
    {
        using var factory = await IdentityTestDatabase.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var opened = await SessionFixtures.OpenAsync(client, _phone);
        using var renewed = await AuthHttp.RefreshAsync(client, opened.RefreshToken, opened.DeviceId);
        using var reused = await AuthHttp.RefreshAsync(client, opened.RefreshToken, opened.DeviceId);
        using var unknown = await AuthHttp.RefreshAsync(client, RandomToken(), opened.DeviceId);
        var before = await SessionFixtures.AllRefreshTokensAsync(factory);

        using var unknownAgain = await AuthHttp.RefreshAsync(client, RandomToken(), opened.DeviceId);
        var malformed = "esto-no-es-un-refresh-token-valido-xyz";
        using var invalid = await AuthHttp.RefreshAsync(client, malformed, opened.DeviceId);

        renewed.StatusCode.Should().Be(HttpStatusCode.OK);
        reused.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        unknown.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await BodyAsync(unknown)).Should().Be(await BodyAsync(reused), "desconocido y reuso tienen el mismo cuerpo");
        (await BodyAsync(unknownAgain)).Should().Be(await BodyAsync(unknown));
        (await SessionFixtures.AllRefreshTokensAsync(factory)).Should().BeEquivalentTo(before, "un desconocido no toca la base");
        invalid.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await invalid.Content.ReadAsStringAsync()).Should().NotContain(malformed, "el valor enviado no se repite");
    }

    [Fact]
    public async Task NoSecretsInLogs_TC_HU_003_12()
    {
        var secrets = new List<string>();
        var logs = await ConsoleCapture.RunAsync(async () =>
        {
            using var factory = await IdentityTestDatabase.CreateFactoryAsync();
            using var client = factory.CreateClient();
            var opened = await SessionFixtures.OpenAsync(client, _phone);
            secrets.AddRange(Secrets(opened.AccessToken, opened.RefreshToken, opened.DeviceId));

            using (var renewed = await AuthHttp.RefreshAsync(client, opened.RefreshToken, opened.DeviceId))
            {
                var (newestAccess, newestRefresh) = await AuthHttp.ReadSessionAsync(renewed);
                secrets.AddRange(Secrets(newestAccess, newestRefresh, opened.DeviceId));
            }

            var unknown = RandomToken();
            secrets.AddRange(Secrets(unknown));
            (await AuthHttp.RefreshAsync(client, opened.RefreshToken, opened.DeviceId)).Dispose();

            (await AuthHttp.RefreshAsync(client, unknown, opened.DeviceId)).Dispose();

            (await AuthHttp.RefreshAsync(client, "no-es-un-token", opened.DeviceId)).Dispose();

            (await AuthHttp.RefreshAsync(client, RandomToken(), Guid.NewGuid().ToString("D"))).Dispose();
        });

        logs.Should().Contain("/api/v1/auth/refresh", "la captura debe incluir las peticiones (si no, la prueba sería vacía)");
        secrets.Where(secret => logs.Contains(secret, StringComparison.Ordinal)).Select(secret => secret.Length > 12 ? secret[..12] + "…" : secret)
            .Should().BeEmpty("ningún log de la renovación lleva refresh token, huella, deviceId ni access token");
    }

    /// <summary>Cada forma en que un token o una huella podría aparecer en un registro.</summary>
    private static IEnumerable<string> Secrets(params string[] values)
    {
        foreach (var value in values)
        {
            yield return value;
            if (value.Length == 43)
            {
                var fingerprint = RefreshTokenFingerprint.Compute(value);
                yield return Convert.ToBase64String(fingerprint);
                yield return Convert.ToHexStringLower(fingerprint);
                yield return Convert.ToHexString(fingerprint);
            }
        }
    }
}
