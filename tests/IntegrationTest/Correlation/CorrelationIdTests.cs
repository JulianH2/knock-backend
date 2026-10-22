using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.IntegrationTest.Correlation;

public class CorrelationIdTests
{
    private const string _header = "X-Correlation-Id";
    private const string _ping = "/api/v1/test/ping";

    [Fact]
    public async Task NoHeader_GeneratesAndReturnsId_TC_TT_API_03_08()
    {
        using var factory = new KnockApiFactory();
        using var client = factory.CreateClient();

        using var first = await TestHttp.GetAsync(client, _ping);
        using var second = await TestHttp.GetAsync(client, _ping);

        var firstId = first.Headers.GetValues(_header).Single();
        var secondId = second.Headers.GetValues(_header).Single();
        firstId.Should().MatchRegex("^[0-9a-f]{32}$", "GUID sin guiones");
        secondId.Should().MatchRegex("^[0-9a-f]{32}$");
        secondId.Should().NotBe(firstId, "se genera uno por petición");
    }

    [Theory]
    [InlineData("abc-123_X.y")]
    [InlineData("3f2b8c1e0d9a4c6f8e7d6c5b4a392817")]
    public async Task ValidHeader_IsPropagatedToResponse_TC_TT_API_03_09(string id)
    {
        using var factory = new KnockApiFactory();
        using var client = factory.CreateClient();

        using var response = await TestHttp.GetAsync(client, _ping, headers: new Dictionary<string, string> { [_header] = id });

        response.Headers.GetValues(_header).Should().ContainSingle().Which.Should().Be(id);
    }

    [Fact]
    public async Task ValidHeaderOfMaxLength_IsPropagated_TC_TT_API_03_09()
    {
        using var factory = new KnockApiFactory();
        using var client = factory.CreateClient();
        var id = new string('a', 64);

        using var response = await TestHttp.GetAsync(client, _ping, headers: new Dictionary<string, string> { [_header] = id });

        response.Headers.GetValues(_header).Should().ContainSingle().Which.Should().Be(id);
    }
}
