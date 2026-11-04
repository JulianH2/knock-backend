using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.IntegrationTest.OpenApi;

/// <summary>
/// HU-003 caso 14 y HU-004 caso 13: el contrato generado declara los códigos de <c>/auth/refresh</c> y <c>/auth/logout</c>. Vive aquí porque el
/// proyecto <c>tests/ContractTest</c> lo crea TT-CI-02 (KNOCK-137) y todavía no existe.
/// </summary>
public sealed class AuthSessionOpenApiTests
{
    private static async Task<JsonElement> OperationAsync(string path)
    {
        using var factory = new KnockApiFactory("Development");
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("paths").GetProperty(path).GetProperty("post").Clone();
    }

    private static void ShouldDeclareProblemDetails(JsonElement responses, string code)
    {
        responses.TryGetProperty(code, out var declared).Should().BeTrue($"el contrato declara {code}");
        declared.GetProperty("content").EnumerateObject()
            .Select(media => media.Value.GetProperty("schema").GetProperty("$ref").GetString())
            .Should().OnlyContain(reference => reference.EndsWith("/ProblemDetails", StringComparison.Ordinal), $"{code} es un ProblemDetails");
    }

    [Fact]
    public async Task RefreshIsDeclared_TC_HU_003_14()
    {
        var operation = await OperationAsync("/api/v{version}/auth/refresh");

        var responses = operation.GetProperty("responses");
        responses.EnumerateObject().Select(r => r.Name).Should().BeEquivalentTo("200", "400", "401", "403", "413", "422", "429");
        foreach (var code in new[] { "400", "401", "403", "413", "422", "429" })
        {
            ShouldDeclareProblemDetails(responses, code);
        }

        responses.GetProperty("200").GetProperty("content").EnumerateObject()
            .Select(media => media.Value.GetProperty("schema").GetProperty("$ref").GetString())
            .Should().OnlyContain(reference => reference.EndsWith("/SessionResponse", StringComparison.Ordinal), "el mismo esquema que el canje");
    }

    [Fact]
    public async Task LogoutIsDeclared_TC_HU_004_13()
    {
        var operation = await OperationAsync("/api/v{version}/auth/logout");

        var responses = operation.GetProperty("responses");
        responses.EnumerateObject().Select(r => r.Name).Should().BeEquivalentTo("204", "400", "413", "422", "429");
        responses.GetProperty("204").TryGetProperty("content", out _).Should().BeFalse("el 204 no tiene cuerpo");
        foreach (var code in new[] { "400", "413", "422", "429" })
        {
            ShouldDeclareProblemDetails(responses, code);
        }
    }
}
