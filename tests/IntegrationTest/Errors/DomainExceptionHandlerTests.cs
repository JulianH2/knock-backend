using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.IntegrationTest.Errors;

public class DomainExceptionHandlerTests
{
    [Theory]
    [InlineData("BadRequest", 400)]
    [InlineData("Forbidden", 403)]
    [InlineData("NotFound", 404)]
    [InlineData("Conflict", 409)]
    [InlineData("Unprocessable", 422)]
    public async Task DomainException_MapsToCatalogStatus_TC_TT_API_03_13(string kind, int expectedStatus)
    {
        using var factory = new KnockApiFactory("Production");
        using var client = factory.CreateClient();

        using var response = await TestHttp.GetAsync(client, $"/api/v1/test/domain/{kind}");

        ((int)response.StatusCode).Should().Be(expectedStatus);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        using var body = await TestHttp.ReadJsonAsync(response);
        body.RootElement.GetProperty("status").GetInt32().Should().Be(expectedStatus, "status coherente con el HTTP");
        body.RootElement.GetProperty("title").GetString().Should().NotBeNullOrWhiteSpace();
        body.RootElement.TryGetProperty("type", out _).Should().BeTrue();
    }

    [Fact]
    public async Task UnknownException_StaysInternalServerError_TC_TT_API_03_13()
    {
        using var factory = new KnockApiFactory("Production");
        using var client = factory.CreateClient();

        using var response = await TestHttp.GetAsync(client, "/api/v1/test/boom");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }
}
