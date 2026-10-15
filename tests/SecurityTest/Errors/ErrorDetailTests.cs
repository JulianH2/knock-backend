using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.SecurityTest.Errors;

public class ErrorDetailTests
{
    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task NonDevelopment_UnhandledException_HidesDetail_TC_TT_API_01_05(string environment)
    {
        using var factory = new KnockApiFactory(environment);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/v1/test/boom", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var raw = await response.Content.ReadAsStringAsync();
        using var body = JsonDocument.Parse(raw);
        body.RootElement.TryGetProperty("detail", out _).Should().BeFalse();
        body.RootElement.TryGetProperty("stackTrace", out _).Should().BeFalse();
        raw.Should().NotContain(TestController.SecretMessage);
        raw.Should().NotContain(nameof(InvalidOperationException));
        raw.Should().NotContain(" at ");
    }
}
