using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.IntegrationTest;

public class RateLimitTests
{
    private const string _me = "/api/v1/me";

    [Fact]
    public async Task PerIpLimitDoesNotDistinguishByUser_TC_TT_API_03_05()
    {
        using var factory = new KnockApiFactory(settings: TestHttp.Limits(globalPermits: 3, authPermits: 1000));
        using var client = factory.CreateClient();
        var userA = TestHttp.IssueToken(factory, Guid.NewGuid());
        var userB = TestHttp.IssueToken(factory, Guid.NewGuid());

        for (var i = 1; i <= 3; i++)
        {
            using var accepted = await TestHttp.GetAsync(client, _me, "192.0.2.50", bearerToken: userA);
            accepted.StatusCode.Should().Be(HttpStatusCode.OK, $"la petición {i} del usuario A está dentro del límite");
        }

        using var limitedA = await TestHttp.GetAsync(client, _me, "192.0.2.50", bearerToken: userA);
        limitedA.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        using var limitedB = await TestHttp.GetAsync(client, _me, "192.0.2.50", bearerToken: userB);
        limitedB.StatusCode.Should().Be(HttpStatusCode.TooManyRequests, "otro sub desde la misma IP comparte el límite: no hay límite por usuario");

        using var otherIp = await TestHttp.GetAsync(client, _me, "192.0.2.51", bearerToken: userB);
        otherIp.StatusCode.Should().Be(HttpStatusCode.OK, "el mismo usuario desde otra IP no está limitado");
    }
}
