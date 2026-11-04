using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Knock.Api.Modules.Identity.Data;
using Knock.IntegrationTest.Support;
using Microsoft.IdentityModel.JsonWebTokens;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Identity;

/// <summary>HU-003: <c>POST /api/v1/auth/refresh</c> con la base real (documentdb-local) y el emulador de Firebase. Solo datos ficticios.</summary>
public class RefreshTests
{
    private const string _phone = "+525555550301";
    private const string _otherPhone = "+525555550302";

    private static string MalformedButWellShapedToken() => Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

    [Fact]
    public async Task ValidRefresh_ReturnsNewPair_TC_HU_003_01()
    {
        using var factory = await IdentityTestDatabase.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var opened = await SessionFixtures.OpenAsync(client, _phone);

        using var response = await AuthHttp.RefreshAsync(client, opened.RefreshToken, opened.DeviceId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
        {
            body.RootElement.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(
                ["accessToken", "refreshToken", "esNuevo", "perfilCompleto"], "el mismo esquema que el canje (HU-003 §4)");
            body.RootElement.GetProperty("esNuevo").GetBoolean().Should().BeFalse();
            body.RootElement.GetProperty("perfilCompleto").GetBoolean().Should().BeFalse();
        }

        var (access, refresh) = await AuthHttp.ReadSessionAsync(response);
        refresh.Should().NotBe(opened.RefreshToken, "cada uso entrega un refresh distinto");
        var handler = new JsonWebTokenHandler();
        var before = handler.ReadJsonWebToken(opened.AccessToken);
        var after = handler.ReadJsonWebToken(access);
        after.GetClaim("sub").Value.Should().Be(before.GetClaim("sub").Value, "es la misma persona");
        after.GetClaim("jti").Value.Should().NotBe(before.GetClaim("jti").Value, "cada access token trae su jti");
        (after.ValidTo - after.ValidFrom).Should().BeCloseTo(TimeSpan.FromMinutes(15), TimeSpan.FromSeconds(5));

        using var me = await AuthHttp.MeAsync(client, access);
        me.StatusCode.Should().Be(HttpStatusCode.OK, "el access token nuevo sirve");
    }

    [Fact]
    public async Task ReusedToken_RevokesWholeFamily_TC_HU_003_03()
    {
        using var factory = await IdentityTestDatabase.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var opened = await SessionFixtures.OpenAsync(client, _phone);
        using var first = await AuthHttp.RefreshAsync(client, opened.RefreshToken, opened.DeviceId);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var (_, newest) = await AuthHttp.ReadSessionAsync(first);

        using var reused = await AuthHttp.RefreshAsync(client, opened.RefreshToken, opened.DeviceId);

        reused.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var documents = await SessionFixtures.AllRefreshTokensAsync(factory);
        documents.Should().HaveCount(2);
        documents.Should().OnlyContain(d => !d[IdentityStore.RevokedAtField].IsBsonNull && d[IdentityStore.RevocationReasonField].AsString == "reuso");
        using var newestAfter = await AuthHttp.RefreshAsync(client, newest, opened.DeviceId);
        newestAfter.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "el refresh más nuevo de la familia también dejó de servir");
    }

    [Fact]
    public async Task ExpiredToken_Returns401_TC_HU_003_04()
    {
        using var factory = await IdentityTestDatabase.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var opened = await SessionFixtures.OpenAsync(client, _phone);
        await SessionFixtures.RefreshTokens(factory).UpdateManyAsync(
            FilterDefinition<BsonDocument>.Empty,
            Builders<BsonDocument>.Update.Set(IdentityStore.ExpiresAtField, new BsonDateTime(DateTime.UtcNow.AddMinutes(-1))));

        using var response = await AuthHttp.RefreshAsync(client, opened.RefreshToken, opened.DeviceId);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var documents = await SessionFixtures.AllRefreshTokensAsync(factory);
        documents.Should().ContainSingle("no se creó ningún documento");
        documents[0].GetValue(IdentityStore.ReplacedByField, BsonNull.Value).IsBsonNull.Should().BeTrue("el vencido no se consume ni se revoca");
        documents[0].GetValue(IdentityStore.RevokedAtField, BsonNull.Value).IsBsonNull.Should().BeTrue();
    }

    [Fact]
    public async Task OtherDevice_Returns401AndRevokes_TC_HU_003_06()
    {
        using var factory = await IdentityTestDatabase.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var opened = await SessionFixtures.OpenAsync(client, _phone);

        using var response = await AuthHttp.RefreshAsync(client, opened.RefreshToken, Guid.NewGuid().ToString("D"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var documents = await SessionFixtures.AllRefreshTokensAsync(factory);
        documents.Should().OnlyContain(d => d[IdentityStore.RevocationReasonField].AsString == "dispositivo" && !d[IdentityStore.RevokedAtField].IsBsonNull);
        using var again = await AuthHttp.RefreshAsync(client, opened.RefreshToken, opened.DeviceId);
        again.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "la familia quedó revocada");
    }

    [Fact]
    public async Task SuspendedAccount_Returns403NoTokens_TC_HU_003_07()
    {
        using var factory = await IdentityTestDatabase.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var opened = await SessionFixtures.OpenAsync(client, _phone);
        await SessionFixtures.Users(factory).UpdateManyAsync(
            FilterDefinition<BsonDocument>.Empty, Builders<BsonDocument>.Update.Set(IdentityStore.StateField, "suspendido"));

        using var response = await AuthHttp.RefreshAsync(client, opened.RefreshToken, opened.DeviceId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().NotContain("accessToken").And.NotContain("refreshToken");
        using var body = JsonDocument.Parse(raw);
        body.RootElement.GetProperty("code").GetString().Should().Be("account_suspended");
        var documents = await SessionFixtures.AllRefreshTokensAsync(factory);
        documents.Should().OnlyContain(d => d[IdentityStore.RevocationReasonField].AsString == "suspension");
    }

    [Fact]
    public async Task Responses_AreNoStoreAndProblemDetails_TC_HU_003_09()
    {
        using var factory = await IdentityTestDatabase.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var opened = await SessionFixtures.OpenAsync(client, _otherPhone);

        using var ok = await AuthHttp.RefreshAsync(client, opened.RefreshToken, opened.DeviceId);
        using var unauthorized = await AuthHttp.RefreshAsync(client, MalformedButWellShapedToken(), opened.DeviceId);
        using var invalid = await AuthHttp.RefreshAsync(client, "no-es-un-token", opened.DeviceId);

        foreach (var response in new[] { ok, unauthorized, invalid })
        {
            response.Headers.CacheControl?.NoStore.Should().BeTrue("ninguna respuesta con tokens o errores de sesión se guarda en cachés");
            response.Headers.Contains("X-Correlation-Id").Should().BeTrue();
        }

        foreach (var problem in new[] { unauthorized, invalid })
        {
            problem.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
            using var body = JsonDocument.Parse(await problem.Content.ReadAsStringAsync());
            body.RootElement.TryGetProperty("traceId", out _).Should().BeTrue();
            body.RootElement.TryGetProperty("correlationId", out _).Should().BeTrue();
            body.RootElement.TryGetProperty("accessToken", out _).Should().BeFalse();
        }

        invalid.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task EleventhRequestPerMinute_Returns429_TC_HU_003_10()
    {
        const string ip = "203.0.113.77";
        using var factory = await IdentityTestDatabase.CreateFactoryAsync(settings: TestHttp.Limits(globalPermits: 1000, authPermits: 10));
        using var client = factory.CreateClient();
        var device = Guid.NewGuid().ToString("D");
        var body = JsonSerializer.Serialize(new { refreshToken = MalformedButWellShapedToken(), deviceId = device });

        for (var i = 1; i <= 10; i++)
        {
            using var accepted = await TestHttp.SendAsync(client, HttpMethod.Post, "/api/v1/auth/refresh", ip, jsonBody: body);
            accepted.StatusCode.Should().Be(HttpStatusCode.Unauthorized, $"la petición {i} entra al servicio y se rechaza");
        }

        using var limited = await TestHttp.SendAsync(client, HttpMethod.Post, "/api/v1/auth/refresh", ip, jsonBody: body);

        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        limited.Headers.RetryAfter?.Delta?.TotalSeconds.Should().BeGreaterThan(0);
        (await SessionFixtures.AllRefreshTokensAsync(factory)).Should().BeEmpty("el 429 llega antes de consultar la base");
    }
}
