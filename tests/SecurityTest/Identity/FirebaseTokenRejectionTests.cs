using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.SecurityTest.Identity;

public class FirebaseTokenRejectionTests
{
    private const string _testPhone = "+15555550103";

    private sealed record Rejection(HttpStatusCode Status, string? MediaType, string? Type, string? Title, string? Detail, string Raw);

    private static async Task<Rejection> ExchangeAsync(string idToken)
    {
        using var factory = new KnockApiFactory();
        using var client = factory.CreateClient();
        using var response = await AuthHttp.ExchangeAsync(client, idToken);
        var raw = await response.Content.ReadAsStringAsync();
        using var body = JsonDocument.Parse(raw);
        string? Read(string name) => body.RootElement.TryGetProperty(name, out var value) ? value.GetString() : null;
        return new Rejection(response.StatusCode, response.Content.Headers.ContentType?.MediaType, Read("type"), Read("title"), Read("detail"), raw);
    }

    private static void ShouldBeUniform401(Rejection rejection)
    {
        rejection.Status.Should().Be(HttpStatusCode.Unauthorized);
        rejection.MediaType.Should().Be("application/problem+json");
        rejection.Raw.Should().NotContain("accessToken").And.NotContain("refreshToken");
    }

    [Theory]
    [InlineData("not-a-jwt")]
    [InlineData("aaa.bbb.ccc")]
    [InlineData("eyJhbGciOiJub25lIn0.e30.")]
    public async Task TamperedIdToken_Returns401_TC_TT_API_02_02(string malformed)
    {
        ShouldBeUniform401(await ExchangeAsync(malformed));
    }

    [Fact]
    public async Task TruncatedIdToken_Returns401_TC_TT_API_02_02()
    {
        var (idToken, _) = await FirebaseEmulator.SignInWithPhoneAsync(_testPhone);

        ShouldBeUniform401(await ExchangeAsync(idToken[..^25]));
    }

    [Fact]
    public async Task ExpiredIdToken_Returns401_TC_TT_API_02_03()
    {
        await FirebaseEmulator.EnsureAvailableAsync();
        var now = DateTimeOffset.UtcNow;
        var expired = FirebaseEmulator.ForgeUnsignedIdToken("uid-prueba-0003", _testPhone, FirebaseEmulator.ProjectId, now.AddHours(-2), now.AddHours(-1));

        ShouldBeUniform401(await ExchangeAsync(expired));
    }

    [Fact]
    public async Task IdTokenWithoutPhoneOrWrongProject_Returns401SameAsInvalid_TC_TT_API_02_04()
    {
        await FirebaseEmulator.EnsureAvailableAsync();
        var now = DateTimeOffset.UtcNow;
        var baseline = await ExchangeAsync("not-a-jwt");
        var withoutPhone = await ExchangeAsync(await FirebaseEmulator.SignInAnonymouslyAsync());
        var forgedWithoutPhone = await ExchangeAsync(
            FirebaseEmulator.ForgeUnsignedIdToken("uid-prueba-0004", null, FirebaseEmulator.ProjectId, now.AddMinutes(-1), now.AddMinutes(59)));
        var otherProject = await ExchangeAsync(
            FirebaseEmulator.ForgeUnsignedIdToken("uid-prueba-0005", _testPhone, "otro-proyecto", now.AddMinutes(-1), now.AddMinutes(59)));

        foreach (var rejection in new[] { withoutPhone, forgedWithoutPhone, otherProject })
        {
            ShouldBeUniform401(rejection);
            rejection.Type.Should().Be(baseline.Type);
            rejection.Title.Should().Be(baseline.Title);
            rejection.Detail.Should().Be(baseline.Detail, "el 401 no revela el motivo");
        }
    }

    [Fact]
    public async Task ValidForgedToken_Control_IsAccepted_TC_TT_API_02_04()
    {
        // Control: un token sin firma bien formado, del proyecto correcto y con teléfono, vigente, se acepta en el emulador;
        // así los rechazos de arriba se deben a vencimiento, teléfono o proyecto y no a un armado erróneo del token.
        await FirebaseEmulator.EnsureAvailableAsync();
        var now = DateTimeOffset.UtcNow;
        var token = FirebaseEmulator.ForgeUnsignedIdToken("uid-prueba-0006", _testPhone, FirebaseEmulator.ProjectId, now.AddMinutes(-1), now.AddMinutes(59));
        using var factory = new KnockApiFactory();
        using var client = factory.CreateClient();

        using var response = await AuthHttp.ExchangeAsync(client, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
