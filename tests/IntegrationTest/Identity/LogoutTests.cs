using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Knock.Api.Modules.Identity.Data;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using Xunit;

namespace Knock.IntegrationTest.Identity;

/// <summary>HU-004: <c>POST /api/v1/auth/logout</c> con la base real (documentdb-local) y el emulador de Firebase. Sin Redis. Solo datos ficticios.</summary>
public class LogoutTests
{
    private const string _phone = "+525555550401";
    private const string _otherPhone = "+525555550402";

    [Fact]
    public async Task ValidRefresh_Returns204AndRevokesFamily_TC_HU_004_01()
    {
        using var factory = await IdentityTestDatabase.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var opened = await SessionFixtures.OpenAsync(client, _phone);
        using var renewed = await AuthHttp.RefreshAsync(client, opened.RefreshToken, opened.DeviceId);
        var (_, newest) = await AuthHttp.ReadSessionAsync(renewed);

        using var response = await AuthHttp.LogoutAsync(client, newest);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
        var documents = await SessionFixtures.AllRefreshTokensAsync(factory);
        documents.Should().HaveCount(2);
        documents.Should().OnlyContain(d => !d[IdentityStore.RevokedAtField].IsBsonNull && d[IdentityStore.RevocationReasonField].AsString == "logout");
    }

    [Fact]
    public async Task RefreshAfterLogout_Returns401_TC_HU_004_02()
    {
        using var factory = await IdentityTestDatabase.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var opened = await SessionFixtures.OpenAsync(client, _phone);
        using var renewed = await AuthHttp.RefreshAsync(client, opened.RefreshToken, opened.DeviceId);
        var (_, newest) = await AuthHttp.ReadSessionAsync(renewed);
        using var logout = await AuthHttp.LogoutAsync(client, newest);
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var withNewest = await AuthHttp.RefreshAsync(client, newest, opened.DeviceId);
        using var withOldest = await AuthHttp.RefreshAsync(client, opened.RefreshToken, opened.DeviceId);

        withNewest.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "el más nuevo de la familia ya no renueva");
        withOldest.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SecondLogout_IsIdempotent_TC_HU_004_03()
    {
        using var factory = await IdentityTestDatabase.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var opened = await SessionFixtures.OpenAsync(client, _phone);
        using var first = await AuthHttp.LogoutAsync(client, opened.RefreshToken);
        var before = await SessionFixtures.AllRefreshTokensAsync(factory);

        using var second = await AuthHttp.LogoutAsync(client, opened.RefreshToken);

        first.StatusCode.Should().Be(HttpStatusCode.NoContent);
        second.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var after = await SessionFixtures.AllRefreshTokensAsync(factory);
        after.Should().BeEquivalentTo(before, "repetir el cierre no cambia ningún documento");
    }

    [Fact]
    public async Task OtherDeviceKeepsSession_TC_HU_004_06()
    {
        using var factory = await IdentityTestDatabase.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var phone = await SessionFixtures.OpenAsync(client, _phone);
        var tablet = await SessionFixtures.OpenAsync(client, _phone);

        using var logout = await AuthHttp.LogoutAsync(client, phone.RefreshToken);
        using var renewedTablet = await AuthHttp.RefreshAsync(client, tablet.RefreshToken, tablet.DeviceId);
        using var renewedPhone = await AuthHttp.RefreshAsync(client, phone.RefreshToken, phone.DeviceId);

        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);
        renewedTablet.StatusCode.Should().Be(HttpStatusCode.OK, "la otra familia sigue viva");
        renewedPhone.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WithoutRedisAccessTokenLivesUntilExpiry_TC_HU_004_09()
    {
        using var factory = await IdentityTestDatabase.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var opened = await SessionFixtures.OpenAsync(client, _phone);

        using var logout = await AuthHttp.LogoutAsync(client, opened.RefreshToken, opened.AccessToken);
        using var me = await AuthHttp.MeAsync(client, opened.AccessToken);

        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);
        me.StatusCode.Should().Be(HttpStatusCode.OK, "sin lista de revocados el access token vale hasta vencer (riesgo aceptado, constitución §12)");
    }

    [Fact]
    public async Task Responses_AreNoStoreAndProblemDetails_TC_HU_004_10()
    {
        using var factory = await IdentityTestDatabase.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var opened = await SessionFixtures.OpenAsync(client, _otherPhone);

        using var ok = await AuthHttp.LogoutAsync(client, opened.RefreshToken);
        using var invalid = await AuthHttp.LogoutAsync(client, "no-es-un-token");

        foreach (var response in new[] { ok, invalid })
        {
            response.Headers.CacheControl?.NoStore.Should().BeTrue();
            response.Headers.Contains("X-Correlation-Id").Should().BeTrue();
        }

        invalid.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        invalid.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        using var body = JsonDocument.Parse(await invalid.Content.ReadAsStringAsync());
        body.RootElement.TryGetProperty("traceId", out _).Should().BeTrue();
        body.RootElement.TryGetProperty("correlationId", out _).Should().BeTrue();
        body.RootElement.GetProperty("errors").TryGetProperty("refreshToken", out _).Should().BeTrue();
    }

    [Fact]
    public async Task EleventhRequestPerMinute_Returns429_TC_HU_004_11()
    {
        const string ip = "203.0.113.88";
        using var factory = await IdentityTestDatabase.CreateFactoryAsync(settings: TestHttp.Limits(globalPermits: 1000, authPermits: 10));
        using var client = factory.CreateClient();
        var body = JsonSerializer.Serialize(new { refreshToken = Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(new byte[32]) });

        for (var i = 1; i <= 10; i++)
        {
            using var accepted = await TestHttp.SendAsync(client, HttpMethod.Post, "/api/v1/auth/logout", ip, jsonBody: body);
            accepted.StatusCode.Should().Be(HttpStatusCode.NoContent, $"la petición {i} está dentro del límite");
        }

        using var limited = await TestHttp.SendAsync(client, HttpMethod.Post, "/api/v1/auth/logout", ip, jsonBody: body);

        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        limited.Headers.RetryAfter?.Delta?.TotalSeconds.Should().BeGreaterThan(0);
    }
}
