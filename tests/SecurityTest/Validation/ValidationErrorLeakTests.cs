using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.SecurityTest.Validation;

public class ValidationErrorLeakTests
{
    [Fact]
    public async Task Invalid422_DoesNotEchoSubmittedValues_TC_TT_API_03_02()
    {
        using var factory = new KnockApiFactory("Production");
        using var client = factory.CreateClient();
        const string phone = "+1-SECRET-PHONE-5550123";
        const string token = "tok-SECRET-TOKEN-value-that-is-far-too-long";
        const string echo = "echo-SECRET-value";
        var body = $$"""{"phone":"{{phone}}","token":"{{token}}","age":7777,"echo":"{{echo}}"}""";

        using var response = await TestHttp.SendAsync(client, HttpMethod.Post, "/api/v1/test/validated", jsonBody: body);

        response.StatusCode.Should().Be((HttpStatusCode)422);
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().NotContain(phone).And.NotContain("SECRET-PHONE");
        raw.Should().NotContain(token).And.NotContain("SECRET-TOKEN");
        raw.Should().NotContain(echo).And.NotContain("SECRET-value");
        raw.Should().NotContain("7777", "ni siquiera los mensajes que el validador arma con el valor");

        using var json = System.Text.Json.JsonDocument.Parse(raw);
        var errors = json.RootElement.GetProperty("errors");
        foreach (var field in new[] { "phone", "token", "age", "echo" })
        {
            errors.TryGetProperty(field, out var messages).Should().BeTrue($"el campo {field} se nombra");
            messages.EnumerateArray().Should().OnlyContain(m => !string.IsNullOrWhiteSpace(m.GetString()), "cada campo trae su motivo");
        }
    }
}
