using System.Net;
using AwesomeAssertions;
using Knock.SecurityTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.SecurityTest.Identity;

/// <summary>Spec 039: <c>GET</c> y <c>PUT /api/v1/me/consentimientos</c> (USR-07 y USR-08). Host real en Production, MongoDB y Redis reales. Solo datos ficticios.</summary>
public class Spec039ConsentApiTests
{
    private const string _path = "/api/v1/me/consentimientos";

    private static Task<HttpResponseMessage> GetAsync(SecurityHosts hosts, string? token) =>
        SecureHttp.SendAsync(hosts.IdentityClient, HttpMethod.Get, _path, bearer: token);

    private static Task<HttpResponseMessage> PutAsync(SecurityHosts hosts, string? token, object body) =>
        SecureHttp.SendAsync(hosts.IdentityClient, HttpMethod.Put, _path, System.Net.Http.Json.JsonContent.Create(body), token);

    [Fact]
    public async Task Get_ListsTheFiveConsentsWithVersionAndDate_CA_039_17()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);

        using var response = await GetAsync(hosts, person.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        SecureHttp.AssertNotCacheable(response);
        var body = await SecureHttp.ReadJsonAsync(response);
        body.EnumerateArray().Select(e => e.GetProperty("tipo").GetString()).Should().Equal("aviso_privacidad", "terminos", "promociones", "encuestas", "recomendaciones");
        var notice = body.EnumerateArray().First();
        notice.GetProperty("vigente").GetBoolean().Should().BeTrue();
        notice.GetProperty("obligatorio").GetBoolean().Should().BeTrue();
        notice.GetProperty("version").GetString().Should().NotBeNullOrEmpty();
        notice.GetProperty("fecha").GetDateTime().Should().BeAfter(DateTime.UtcNow.AddHours(-1));
    }

    [Fact]
    public async Task GetWithoutTokenOrWithAnExpiredOne_Returns401_CA_039_18()
    {
        var hosts = await SecurityHosts.SharedAsync();

        using var anonymous = await GetAsync(hosts, null);
        using var expired = await GetAsync(hosts, TokenForge.Expired());

        await SecureHttp.AssertProblemAsync(anonymous, HttpStatusCode.Unauthorized);
        await SecureHttp.AssertProblemAsync(expired, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PutActivatesAndWithdrawsAnOptional_AndTheListReflectsItAtOnce_CA_039_19()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);

        using var activated = await PutAsync(hosts, person.AccessToken, new { tipo = "promociones", vigente = true });
        using var withdrawn = await PutAsync(hosts, person.AccessToken, new { tipo = "promociones", vigente = false });
        using var listed = await GetAsync(hosts, person.AccessToken);

        activated.StatusCode.Should().Be(HttpStatusCode.OK);
        (await SecureHttp.ReadJsonAsync(activated)).GetProperty("vigente").GetBoolean().Should().BeTrue();
        withdrawn.StatusCode.Should().Be(HttpStatusCode.OK);
        var promotions = (await SecureHttp.ReadJsonAsync(listed)).EnumerateArray().Single(e => e.GetProperty("tipo").GetString() == "promociones");
        promotions.GetProperty("vigente").GetBoolean().Should().BeFalse();
        promotions.GetProperty("version").GetString().Should().NotBeNullOrEmpty();
        promotions.GetProperty("fecha").GetDateTime().Should().BeAfter(DateTime.UtcNow.AddHours(-1));
        var stored = (await hosts.Identity.Users.Find(new BsonDocument("sub", person.Sub)).SingleAsync())["consentimientos"].AsBsonArray;
        stored.Count(c => c["documento"].AsString == "promociones").Should().Be(1, "cambiar dos veces no deja dos entradas");
    }

    [Fact]
    public async Task PutWithoutTokenOrWithAnExpiredOne_Returns401_CA_039_20()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var body = new { tipo = "promociones", vigente = true };

        using var anonymous = await PutAsync(hosts, null, body);
        using var expired = await PutAsync(hosts, TokenForge.Expired(), body);

        await SecureHttp.AssertProblemAsync(anonymous, HttpStatusCode.Unauthorized);
        await SecureHttp.AssertProblemAsync(expired, HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("aviso_privacidad")]
    [InlineData("terminos")]
    [InlineData("marketing")]
    [InlineData("")]
    public async Task AMandatoryOrUnknownType_Returns422AndChangesNothing_CA_039_21(string type)
    {
        var hosts = await SecurityHosts.SharedAsync();
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        var before = await hosts.Identity.Users.Find(new BsonDocument("sub", person.Sub)).SingleAsync();

        using var response = await PutAsync(hosts, person.AccessToken, new { tipo = type, vigente = false });

        await SecureHttp.AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, "marketing");
        (await SecureHttp.ReadJsonAsync(response)).GetProperty("errors").TryGetProperty("tipo", out _).Should().BeTrue();
        (await hosts.Identity.Users.Find(new BsonDocument("sub", person.Sub)).SingleAsync()).ToJson().Should().Be(before.ToJson());
    }

    [Fact]
    public async Task MissingVigente_Returns422_CA_039_21()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);

        using var response = await PutAsync(hosts, person.AccessToken, new { tipo = "promociones" });

        await SecureHttp.AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity);
        (await SecureHttp.ReadJsonAsync(response)).GetProperty("errors").TryGetProperty("vigente", out _).Should().BeTrue();
    }

    [Fact]
    public async Task UnreadableBody_Returns400_CA_039_22()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);

        using var response = await SecureHttp.SendRawJsonAsync(hosts.IdentityClient, HttpMethod.Put, _path, "{\"tipo\":", person.AccessToken);
        using var wrongType = await SecureHttp.SendRawJsonAsync(hosts.IdentityClient, HttpMethod.Put, _path, "{\"tipo\":\"promociones\",\"vigente\":\"si\"}", person.AccessToken);

        await SecureHttp.AssertProblemAsync(response, HttpStatusCode.BadRequest);
        wrongType.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WithdrawingPromotions_LeavesVigenteFalseWithDateAndVersion_CA_039_36()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        (await PutAsync(hosts, person.AccessToken, new { tipo = "promociones", vigente = true })).Dispose();

        using var response = await PutAsync(hosts, person.AccessToken, new { tipo = "promociones", vigente = false });

        var body = await SecureHttp.ReadJsonAsync(response);
        body.GetProperty("vigente").GetBoolean().Should().BeFalse();
        body.GetProperty("version").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("fecha").GetDateTime().Should().BeAfter(DateTime.UtcNow.AddHours(-1));
    }

    [Fact]
    public async Task OnePersonNeverChangesTheConsentsOfAnother_EvenSendingTheirSub_CA_039_40()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var a = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        var b = await hosts.Identity.SignInAsync(hosts.IdentityClient);

        using var response = await PutAsync(hosts, a.AccessToken, new { tipo = "encuestas", vigente = true, sub = b.Sub, usuarioId = b.Sub });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var other = (await hosts.Identity.Users.Find(new BsonDocument("sub", b.Sub)).SingleAsync())["consentimientos"].AsBsonArray;
        other.Select(c => c["documento"].AsString).Should().BeEquivalentTo("aviso_privacidad", "terminos");
        var own = (await hosts.Identity.Users.Find(new BsonDocument("sub", a.Sub)).SingleAsync())["consentimientos"].AsBsonArray;
        own.Select(c => c["documento"].AsString).Should().Contain("encuestas");
    }
}
