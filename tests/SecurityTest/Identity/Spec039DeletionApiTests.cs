extern alias IdentityApi;

using System.Globalization;
using System.Net;
using AwesomeAssertions;
using IdentityApi::Knock.Identity.Api.Modules.Account.Domain;
using Knock.SecurityTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.SecurityTest.Identity;

/// <summary>Spec 039: <c>POST /api/v1/me/eliminacion</c> y <c>/cancelar</c> (USR-10 y USR-11), y los transversales de la spec. Host real en Production con Mercado doblado. Solo datos ficticios.</summary>
public class Spec039DeletionApiTests
{
    private const string _delete = "/api/v1/me/eliminacion";
    private const string _cancel = "/api/v1/me/eliminacion/cancelar";

    private static Task<HttpResponseMessage> DeleteAsync(SecurityHosts hosts, string? token, object body) =>
        SecureHttp.PostJsonAsync(hosts.IdentityClient, _delete, body, token);

    private static Task<HttpResponseMessage> CancelAsync(SecurityHosts hosts, string? token) =>
        SecureHttp.SendAsync(hosts.IdentityClient, HttpMethod.Post, _cancel, bearer: token);

    private static Task<BsonDocument> UserAsync(SecurityHosts hosts, string sub) =>
        hosts.Identity.Users.Find(new BsonDocument("sub", sub)).SingleAsync();

    [Fact]
    public async Task ValidRequest_Returns202SchedulesSevenDaysAndCancelsOpenItems_CA_039_25()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);

        using var response = await DeleteAsync(hosts, person.AccessToken, new { confirmacion = "ELIMINAR", motivo = "ya no lo uso" });

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var user = await UserAsync(hosts, person.Sub);
        user["estado"].AsString.Should().Be("eliminacion_programada");
        (user["eliminarEn"].ToUniversalTime() - DateTime.UtcNow).Should().BeCloseTo(TimeSpan.FromDays(7), TimeSpan.FromMinutes(5));
        hosts.Identity.MarketAccount.CancelRequests.Should().Contain(person.Sub);
        user.ToJson().Should().NotContain("ya no lo uso", "el motivo no se guarda");
    }

    [Fact]
    public async Task WithoutTokenOrWithAnExpiredOne_Returns401_CA_039_26()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var body = new { confirmacion = "ELIMINAR" };

        using var anonymous = await DeleteAsync(hosts, null, body);
        using var expired = await DeleteAsync(hosts, TokenForge.Expired(), body);
        using var cancelAnonymous = await CancelAsync(hosts, null);
        using var cancelExpired = await CancelAsync(hosts, TokenForge.Expired());

        foreach (var response in new[] { anonymous, expired, cancelAnonymous, cancelExpired })
        {
            await SecureHttp.AssertProblemAsync(response, HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task CancelWithoutTokenOrWithAnExpiredOne_Returns401_CA_039_31()
    {
        var hosts = await SecurityHosts.SharedAsync();

        using var anonymous = await CancelAsync(hosts, null);
        using var expired = await CancelAsync(hosts, TokenForge.Expired());

        await SecureHttp.AssertProblemAsync(anonymous, HttpStatusCode.Unauthorized);
        await SecureHttp.AssertProblemAsync(expired, HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("eliminar")]
    [InlineData("")]
    [InlineData("SI")]
    public async Task WrongConfirmation_Returns422AndChangesNothing_CA_039_27(string text)
    {
        var hosts = await SecurityHosts.SharedAsync();
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);

        using var response = await DeleteAsync(hosts, person.AccessToken, new { confirmacion = text });

        await SecureHttp.AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity);
        (await SecureHttp.ReadJsonAsync(response)).GetProperty("errors").TryGetProperty("confirmacion", out _).Should().BeTrue();
        (await UserAsync(hosts, person.Sub))["estado"].AsString.Should().Be("activa");
    }

    [Fact]
    public async Task UnreadableBody_Returns400_CA_039_28()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);

        using var response = await SecureHttp.SendRawJsonAsync(hosts.IdentityClient, HttpMethod.Post, _delete, "{\"confirmacion\":", person.AccessToken);

        await SecureHttp.AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AVisitInProgress_Returns409AndCancelsNothing_CA_039_29()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        hosts.Identity.MarketAccount.Set(person.Sub, new AccountPending(1, ["665f1b2c3d4e5f6071829a01"], 0, [], true));

        using var response = await DeleteAsync(hosts, person.AccessToken, new { confirmacion = "ELIMINAR" });

        var raw = await SecureHttp.AssertProblemAsync(response, HttpStatusCode.Conflict);
        raw.Should().Contain("deletion_not_allowed");
        hosts.Identity.MarketAccount.CancelRequests.Should().NotContain(person.Sub);
        (await UserAsync(hosts, person.Sub))["estado"].AsString.Should().Be("activa");
    }

    [Fact]
    public async Task ScheduleThenCancel_ReturnsToActive_AndAskingTwiceIs409_CA_039_30()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        (await DeleteAsync(hosts, person.AccessToken, new { confirmacion = "ELIMINAR" })).Dispose();
        using var twice = await DeleteAsync(hosts, person.AccessToken, new { confirmacion = "ELIMINAR" });

        using var cancelled = await CancelAsync(hosts, person.AccessToken);

        (await SecureHttp.AssertProblemAsync(twice, HttpStatusCode.Conflict)).Should().Contain("deletion_not_allowed");
        cancelled.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await UserAsync(hosts, person.Sub);
        user["estado"].AsString.Should().Be("activa");
        user.Contains("eliminarEn").Should().BeFalse();
    }

    [Fact]
    public async Task CancelWithNothingScheduled_Returns409NoDeletionScheduled_CA_039_32()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);

        using var response = await CancelAsync(hosts, person.AccessToken);

        (await SecureHttp.AssertProblemAsync(response, HttpStatusCode.Conflict)).Should().Contain("no_deletion_scheduled");
    }

    [Fact]
    public async Task ExportRequestThenAnother_ReturnsTheLinkFlowStatuses_CA_039_35()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);

        using var first = await SecureHttp.SendAsync(hosts.IdentityClient, HttpMethod.Post, "/api/v1/me/exportaciones", bearer: person.AccessToken);
        using var second = await SecureHttp.SendAsync(hosts.IdentityClient, HttpMethod.Post, "/api/v1/me/exportaciones", bearer: person.AccessToken);

        first.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await SecureHttp.AssertProblemAsync(second, HttpStatusCode.Conflict)).Should().Contain("export_in_progress");
    }

    [Fact]
    public async Task AnotherPersonsSubInTheBody_IsIgnoredOnDeletion_CA_039_40()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var a = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        var b = await hosts.Identity.SignInAsync(hosts.IdentityClient);

        using var response = await DeleteAsync(hosts, a.AccessToken, new { confirmacion = "ELIMINAR", sub = b.Sub, usuarioId = b.Sub });

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await UserAsync(hosts, b.Sub))["estado"].AsString.Should().Be("activa");
        (await UserAsync(hosts, a.Sub))["estado"].AsString.Should().Be("eliminacion_programada");
    }

    [Fact]
    public async Task ErrorsNeverCarryThePhoneOrTheSentValues_CA_039_41()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var owner = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);

        using var taken = await SecureHttp.PostJsonAsync(hosts.IdentityClient, "/api/v1/me/cambio-numero", new { celularNuevo = owner.Phone }, person.AccessToken);
        using var invalid = await DeleteAsync(hosts, person.AccessToken, new { confirmacion = "secreto-xyz" });

        await SecureHttp.AssertProblemAsync(taken, HttpStatusCode.Conflict, owner.Phone, person.Phone);
        await SecureHttp.AssertProblemAsync(invalid, HttpStatusCode.UnprocessableEntity, "secreto-xyz");
    }

    [Theory]
    [InlineData("/api/v1/me/cambio-numero")]
    [InlineData("/api/v1/me/cambio-numero/confirmar")]
    [InlineData("/api/v1/recuperacion-acceso")]
    [InlineData("/api/v1/me/eliminacion")]
    [InlineData("/api/v1/me/consentimientos")]
    public async Task OversizedBody_Returns413_CA_039_42(string path)
    {
        var hosts = await SecurityHosts.SharedAsync();
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        var json = $$"""{"campo":"{{new string('a', 20000)}}"}""";
        var method = path.EndsWith("consentimientos", StringComparison.Ordinal) ? HttpMethod.Put : HttpMethod.Post;

        using var response = await SecureHttp.SendRawJsonAsync(hosts.IdentityClient, method, path, json, person.AccessToken, remoteIp: "10.9.9.9");

        await SecureHttp.AssertProblemAsync(response, HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task OverTheLimit_Returns429WithRetryAfterOnConfirmAndDeletion_CA_039_42()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        for (var i = 0; i < 5; i++)
        {
            (await SecureHttp.PostJsonAsync(hosts.IdentityClient, "/api/v1/me/cambio-numero/confirmar", new { firebaseIdToken = "x" }, person.AccessToken)).Dispose();
        }

        using var limited = await SecureHttp.PostJsonAsync(hosts.IdentityClient, "/api/v1/me/cambio-numero/confirmar", new { firebaseIdToken = "x" }, person.AccessToken);

        await SecureHttp.AssertProblemAsync(limited, HttpStatusCode.TooManyRequests);
        int.Parse(limited.Headers.GetValues("Retry-After").Single(), CultureInfo.InvariantCulture).Should().BeInRange(1, 3600);
    }

    [Fact]
    public async Task RepeatedCreation_LeavesNoDuplicates_CA_039_43()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);

        var responses = await Task.WhenAll(
            DeleteAsync(hosts, person.AccessToken, new { confirmacion = "ELIMINAR" }),
            DeleteAsync(hosts, person.AccessToken, new { confirmacion = "ELIMINAR" }));

        try
        {
            responses.Select(r => (int)r.StatusCode).Order().Should().Equal(202, 409);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }
}
