using System.Net;
using AwesomeAssertions;
using Knock.SecurityTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.SecurityTest.Identity;

/// <summary>Spec 039: <c>POST /api/v1/me/exportaciones</c> (USR-06). Host real en Production, MongoDB y Redis reales. Solo datos ficticios.</summary>
public class Spec039ExportApiTests
{
    private const string _path = "/api/v1/me/exportaciones";

    private static Task<HttpResponseMessage> ExportAsync(SecurityHosts hosts, string? token) =>
        SecureHttp.SendAsync(hosts.IdentityClient, HttpMethod.Post, _path, bearer: token);

    [Fact]
    public async Task ValidRequest_Returns202AndMarksTheExportInProgress_CA_039_14()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);

        using var response = await ExportAsync(hosts, person.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var user = await hosts.Identity.Users.Find(new BsonDocument("sub", person.Sub)).SingleAsync();
        user.Contains("exportacionSolicitadaEn").Should().BeTrue();
    }

    [Fact]
    public async Task WithoutTokenOrWithAnExpiredOne_Returns401_CA_039_15()
    {
        var hosts = await SecurityHosts.SharedAsync();

        using var anonymous = await ExportAsync(hosts, null);
        using var expired = await ExportAsync(hosts, TokenForge.Expired());

        await SecureHttp.AssertProblemAsync(anonymous, HttpStatusCode.Unauthorized);
        await SecureHttp.AssertProblemAsync(expired, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ASecondRequestWhileTheFirstRuns_Returns409ExportInProgress_CA_039_16()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        (await ExportAsync(hosts, person.AccessToken)).Dispose();

        using var second = await ExportAsync(hosts, person.AccessToken);

        var raw = await SecureHttp.AssertProblemAsync(second, HttpStatusCode.Conflict);
        raw.Should().Contain("export_in_progress");
    }

    [Fact]
    public async Task TwoSimultaneousRequests_LeaveOneAnswer202AndOne409_CA_039_43()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);

        var responses = await Task.WhenAll(ExportAsync(hosts, person.AccessToken), ExportAsync(hosts, person.AccessToken));

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
