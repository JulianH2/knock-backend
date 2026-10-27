using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.SecurityTest.Errors;

public class DomainErrorDetailTests
{
    [Theory]
    [InlineData("Production", "BadRequest")]
    [InlineData("Production", "Forbidden")]
    [InlineData("Production", "NotFound")]
    [InlineData("Production", "Conflict")]
    [InlineData("Production", "Unprocessable")]
    [InlineData("Staging", "Conflict")]
    [InlineData("Staging", "NotFound")]
    public async Task NonDevelopment_DomainException_HidesInternals_TC_TT_API_03_14(string environment, string kind)
    {
        using var factory = new KnockApiFactory(environment);
        using var client = factory.CreateClient();

        using var response = await TestHttp.GetAsync(client, $"/api/v1/test/domain/{kind}");

        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var raw = await response.Content.ReadAsStringAsync();
        using var body = System.Text.Json.JsonDocument.Parse(raw);
        body.RootElement.TryGetProperty("detail", out _).Should().BeFalse();
        body.RootElement.TryGetProperty("stackTrace", out _).Should().BeFalse();
        raw.Should().NotContain(TestController.SecretMessage);
        raw.Should().NotContain("sensitive-token-value");
        raw.Should().NotContain("+15555550100");
        raw.Should().NotContain("DomainException");
        raw.Should().NotContain(" at ");
    }
}
