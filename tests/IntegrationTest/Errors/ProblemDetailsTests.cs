using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.IntegrationTest.Errors;

public class ProblemDetailsTests
{
    private static readonly Uri _boomUri = new("/api/v1/test/boom", UriKind.Relative);

    [Fact]
    public async Task UnhandledException_ReturnsProblemDetails_TC_TT_API_01_04()
    {
        using var factory = new KnockApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(_boomUri);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.TryGetProperty("type", out _).Should().BeTrue();
        body.RootElement.TryGetProperty("title", out _).Should().BeTrue();
        body.RootElement.GetProperty("status").GetInt32().Should().Be(500);
    }

    [Fact]
    public async Task Development_UnhandledException_IncludesDetail_TC_TT_API_01_06()
    {
        using var factory = new KnockApiFactory("Development");
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(_boomUri);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("detail").GetString().Should().Contain(TestController.SecretMessage);
    }
}
