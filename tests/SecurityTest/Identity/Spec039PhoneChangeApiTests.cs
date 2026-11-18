using System.Globalization;
using System.Net;
using AwesomeAssertions;
using Knock.SecurityTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.SecurityTest.Identity;

/// <summary>Spec 039: <c>POST /api/v1/me/cambio-numero</c> (USR-03) y <c>/confirmar</c> (USR-04). Host real en Production, MongoDB y Redis reales. Solo datos ficticios.</summary>
public class Spec039PhoneChangeApiTests
{
    private const string _check = "/api/v1/me/cambio-numero";

    private const string _confirm = "/api/v1/me/cambio-numero/confirmar";

    private static Task<HttpResponseMessage> ConfirmAsync(SecurityHosts hosts, string? token, object body) =>
        SecureHttp.PostJsonAsync(hosts.IdentityClient, _confirm, body, token);

    /// <summary>Abre una segunda sesión de la misma persona (otro dispositivo) y devuelve su <c>sid</c>.</summary>
    private static async Task<string> SecondSessionAsync(SecurityHosts hosts, SignedInPerson person)
    {
        using var response = await SecureHttp.PostJsonAsync(
            hosts.IdentityClient,
            "/api/v1/auth/firebase",
            new { firebaseIdToken = hosts.Identity.Firebase.Issue(person.Uid, person.Phone), deviceId = Guid.NewGuid().ToString("D"), aceptaAviso = true, dispositivo = "Otro teléfono" });
        response.EnsureSuccessStatusCode();
        return TokenForge.Claim((await SecureHttp.ReadJsonAsync(response)).GetProperty("accessToken").GetString()!, "sid");
    }

    private static Task<HttpResponseMessage> CheckAsync(SecurityHosts hosts, string? token, object body) =>
        SecureHttp.PostJsonAsync(hosts.IdentityClient, _check, body, token);

    [Fact]
    public async Task FreeNumber_Returns204_CA_039_01()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);

        using var response = await CheckAsync(hosts, person.AccessToken, new { celularNuevo = SecurityIdentityFactory.NewPhone() });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task WithoutTokenOrWithAnExpiredOne_Returns401_CA_039_02()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var body = new { celularNuevo = SecurityIdentityFactory.NewPhone() };

        using var anonymous = await CheckAsync(hosts, null, body);
        using var expired = await CheckAsync(hosts, TokenForge.Expired(), body);

        await SecureHttp.AssertProblemAsync(anonymous, HttpStatusCode.Unauthorized);
        await SecureHttp.AssertProblemAsync(expired, HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("5512345678")]
    [InlineData("+15512345678")]
    [InlineData("+52551234567")]
    [InlineData("+52551234567x")]
    [InlineData("")]
    public async Task InvalidNumber_Returns422WithFieldErrorsAndDoesNotRepeatIt_CA_039_03(string phone)
    {
        var hosts = await SecurityHosts.SharedAsync();
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);

        using var response = await CheckAsync(hosts, person.AccessToken, new { celularNuevo = phone });

        var raw = await SecureHttp.AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, phone);
        (await SecureHttp.ReadJsonAsync(response)).GetProperty("errors").TryGetProperty("celularNuevo", out _).Should().BeTrue();
        raw.Should().NotBeNull();
    }

    [Fact]
    public async Task UnreadableBody_Returns400_CA_039_04()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);

        using var response = await SecureHttp.SendRawJsonAsync(hosts.IdentityClient, HttpMethod.Post, _check, "{\"celularNuevo\":", person.AccessToken);

        await SecureHttp.AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task NumberOfAnotherAccount_Returns409PhoneAlreadyRegistered_CA_039_05()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var owner = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);

        using var response = await CheckAsync(hosts, person.AccessToken, new { celularNuevo = owner.Phone });

        var raw = await SecureHttp.AssertProblemAsync(response, HttpStatusCode.Conflict, owner.Phone);
        raw.Should().Contain("phone_already_registered");
    }

    [Fact]
    public async Task FourthCheckInAnHour_Returns429WithRetryAfter_CA_039_33()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        var other = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        for (var attempt = 0; attempt < 3; attempt++)
        {
            using var allowed = await CheckAsync(hosts, person.AccessToken, new { celularNuevo = SecurityIdentityFactory.NewPhone() });
            allowed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using var limited = await CheckAsync(hosts, person.AccessToken, new { celularNuevo = SecurityIdentityFactory.NewPhone() });
        using var untouched = await CheckAsync(hosts, other.AccessToken, new { celularNuevo = SecurityIdentityFactory.NewPhone() });

        await SecureHttp.AssertProblemAsync(limited, HttpStatusCode.TooManyRequests);
        int.Parse(limited.Headers.GetValues("Retry-After").Single(), CultureInfo.InvariantCulture).Should().BeInRange(1, 3600);
        untouched.StatusCode.Should().Be(HttpStatusCode.NoContent, "el límite es por persona");
    }

    [Fact]
    public async Task ValidToken_Returns200ChangesThePhoneKeepsTheCurrentSessionAndClosesTheOthers_CA_039_06()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        var otherSid = await SecondSessionAsync(hosts, person);
        var before = await hosts.Identity.Users.Find(new BsonDocument("sub", person.Sub)).SingleAsync();
        var newPhone = SecurityIdentityFactory.NewPhone();

        using var response = await ConfirmAsync(hosts, person.AccessToken, new { firebaseIdToken = hosts.Identity.Firebase.Issue(person.Uid, newPhone) });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().NotContain(newPhone).And.NotContain(person.Phone);
        var after = await hosts.Identity.Users.Find(new BsonDocument("sub", person.Sub)).SingleAsync();
        after["celularHash"].Should().NotBe(before["celularHash"]);
        after["celularCifrado"].Should().NotBe(before["celularCifrado"]);
        var sessions = await hosts.Identity.Sessions.Find(new BsonDocument("usuarioId", after["_id"])).ToListAsync();
        sessions.Single(s => s["_id"].AsObjectId.ToString() == person.SessionId)["revocada"].AsBoolean.Should().BeFalse("la sesión actual se conserva");
        sessions.Single(s => s["_id"].AsObjectId.ToString() == otherSid)["revocada"].AsBoolean.Should().BeTrue();
        (await hosts.Identity.Redis.KeyExistsAsync("sesion-cancelada:" + otherSid)).Should().BeTrue("el gateway rechaza el sid cerrado");
        (await hosts.Identity.Redis.KeyExistsAsync("sesion-cancelada:" + person.SessionId)).Should().BeFalse();
        using var stillWorks = await SecureHttp.SendAsync(hosts.IdentityClient, HttpMethod.Get, "/api/v1/me", bearer: person.AccessToken);
        stillWorks.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ConfirmWithoutTokenOrWithAnExpiredOne_Returns401_CA_039_07()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var body = new { firebaseIdToken = "synthetic" };

        using var anonymous = await ConfirmAsync(hosts, null, body);
        using var expired = await ConfirmAsync(hosts, TokenForge.Expired(), body);

        await SecureHttp.AssertProblemAsync(anonymous, HttpStatusCode.Unauthorized);
        await SecureHttp.AssertProblemAsync(expired, HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-firebase-token-xyz")]
    public async Task MissingOrUnverifiableFirebaseToken_Returns422AndChangesNothing_CA_039_08(string firebaseToken)
    {
        var hosts = await SecurityHosts.SharedAsync();
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        var before = await hosts.Identity.Users.Find(new BsonDocument("sub", person.Sub)).SingleAsync();

        using var response = await ConfirmAsync(hosts, person.AccessToken, new { firebaseIdToken = firebaseToken });

        await SecureHttp.AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, "not-a-firebase-token-xyz");
        (await SecureHttp.ReadJsonAsync(response)).GetProperty("errors").TryGetProperty("firebaseIdToken", out _).Should().BeTrue();
        (await hosts.Identity.Users.Find(new BsonDocument("sub", person.Sub)).SingleAsync())["celularHash"].Should().Be(before["celularHash"]);
    }

    [Fact]
    public async Task ConfirmWithUnreadableBody_Returns400_CA_039_09()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);

        using var response = await SecureHttp.SendRawJsonAsync(hosts.IdentityClient, HttpMethod.Post, _confirm, "{\"firebaseIdToken\":", person.AccessToken);

        await SecureHttp.AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task NumberOfAnotherAccount_OnConfirm_Returns409AndKeepsEverything_CA_039_10()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var owner = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        var otherSid = await SecondSessionAsync(hosts, person);
        var before = await hosts.Identity.Users.Find(new BsonDocument("sub", person.Sub)).SingleAsync();

        using var response = await ConfirmAsync(hosts, person.AccessToken, new { firebaseIdToken = hosts.Identity.Firebase.Issue(person.Uid, owner.Phone) });

        var raw = await SecureHttp.AssertProblemAsync(response, HttpStatusCode.Conflict, owner.Phone);
        raw.Should().Contain("phone_already_registered");
        (await hosts.Identity.Users.Find(new BsonDocument("sub", person.Sub)).SingleAsync())["celularHash"].Should().Be(before["celularHash"]);
        (await hosts.Identity.Redis.KeyExistsAsync("sesion-cancelada:" + otherSid)).Should().BeFalse("no se cerró nada");
    }
}
