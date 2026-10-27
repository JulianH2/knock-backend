using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.IntegrationTest.Validation;

public class ValidationFilterTests
{
    private const string _path = "/api/v1/test/validated";

    [Fact]
    public async Task InvalidBody_Returns422ProblemDetailsWithFields_TC_TT_API_03_01()
    {
        using var factory = new KnockApiFactory();
        using var client = factory.CreateClient();
        var before = TestController.ValidatedCalls;

        using var response = await TestHttp.SendAsync(
            client, HttpMethod.Post, _path, jsonBody: """{"phone":"abc","token":"","age":5}""");

        response.StatusCode.Should().Be((HttpStatusCode)422);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        using var body = await TestHttp.ReadJsonAsync(response);
        body.RootElement.GetProperty("status").GetInt32().Should().Be(422);
        body.RootElement.TryGetProperty("type", out _).Should().BeTrue();
        body.RootElement.GetProperty("title").GetString().Should().NotBeNullOrWhiteSpace();
        var errors = body.RootElement.GetProperty("errors");
        errors.TryGetProperty("phone", out var phone).Should().BeTrue("el campo se nombra en camelCase");
        phone.GetArrayLength().Should().BeGreaterThan(0);
        errors.TryGetProperty("token", out _).Should().BeTrue();
        errors.TryGetProperty("age", out _).Should().BeTrue();
        TestController.ValidatedCalls.Should().Be(before, "el controller no se ejecuta cuando la validación falla");
    }

    [Fact]
    public async Task ValidBody_ReachesTheController_TC_TT_API_03_01()
    {
        using var factory = new KnockApiFactory();
        using var client = factory.CreateClient();
        var before = TestController.ValidatedCalls;

        using var response = await TestHttp.SendAsync(
            client, HttpMethod.Post, _path, jsonBody: """{"phone":"+15555550100","token":"short","age":30}""");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        TestController.ValidatedCalls.Should().Be(before + 1);
    }

    [Fact]
    public async Task UnreadableBody_KeepsTheFrameworkBadRequest_TC_TT_API_03_01()
    {
        using var factory = new KnockApiFactory();
        using var client = factory.CreateClient();

        using var response = await TestHttp.SendAsync(client, HttpMethod.Post, _path, jsonBody: "{not json");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "el 400 de cuerpo ilegible no cambia");
    }
}
