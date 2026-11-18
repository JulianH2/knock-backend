extern alias IdentityApi;

using System.Net;
using AwesomeAssertions;
using IdentityApi::Knock.Identity.Api.Modules.Account.Domain;
using Knock.SecurityTest.Support;
using Xunit;

namespace Knock.SecurityTest.Identity;

/// <summary>Spec 039: <c>GET /api/v1/me/pendientes</c> (USR-09). Host real en Production con Mercado doblado (las rutas internas todavía no existen). Solo datos ficticios.</summary>
public class Spec039PendingApiTests
{
    private const string _path = "/api/v1/me/pendientes";

    private static Task<HttpResponseMessage> GetAsync(SecurityHosts hosts, string? token) =>
        SecureHttp.SendAsync(hosts.IdentityClient, HttpMethod.Get, _path, bearer: token);

    [Fact]
    public async Task ReturnsOnlyTheCountsAndIdsOfTheCaller_CA_039_23()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        var other = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        hosts.Identity.MarketAccount.Set(person.Sub, new AccountPending(2, ["665f1b2c3d4e5f6071829a01", "665f1b2c3d4e5f6071829a02"], 1, ["665f1b2c3d4e5f6071829b01"], false));
        hosts.Identity.MarketAccount.Set(other.Sub, new AccountPending(1, ["665f1b2c3d4e5f6071829c01"], 0, [], false));

        using var response = await GetAsync(hosts, person.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        SecureHttp.AssertNotCacheable(response);
        var body = await SecureHttp.ReadJsonAsync(response);
        body.GetProperty("citas").GetProperty("total").GetInt32().Should().Be(2);
        body.GetProperty("citas").GetProperty("ids").EnumerateArray().Select(e => e.GetString()).Should().Equal("665f1b2c3d4e5f6071829a01", "665f1b2c3d4e5f6071829a02");
        body.GetProperty("cotizaciones").GetProperty("total").GetInt32().Should().Be(1);
        (await response.Content.ReadAsStringAsync()).Should().NotContain("665f1b2c3d4e5f6071829c01").And.NotContain(other.Sub);
    }

    [Fact]
    public async Task WithoutTokenOrWithAnExpiredOne_Returns401_CA_039_24()
    {
        var hosts = await SecurityHosts.SharedAsync();

        using var anonymous = await GetAsync(hosts, null);
        using var expired = await GetAsync(hosts, TokenForge.Expired());

        await SecureHttp.AssertProblemAsync(anonymous, HttpStatusCode.Unauthorized);
        await SecureHttp.AssertProblemAsync(expired, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AnIdentifierOfAnotherPersonInTheQuery_IsIgnored_CA_039_40()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        var other = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        hosts.Identity.MarketAccount.Set(other.Sub, new AccountPending(1, ["665f1b2c3d4e5f6071829c01"], 0, [], false));

        using var response = await SecureHttp.SendAsync(hosts.IdentityClient, HttpMethod.Get, $"{_path}?sub={other.Sub}&usuarioId={other.Sub}", bearer: person.AccessToken);

        (await response.Content.ReadAsStringAsync()).Should().NotContain("665f1b2c3d4e5f6071829c01");
    }
}
