using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.IntegrationTest.OpenApi;

/// <summary>
/// HU-002 caso 14: el contrato se genera del código y declara <c>PUT /me</c> y los campos nuevos de <c>GET /me</c>. La spec lo ubica en
/// <c>ContractTest</c>, un proyecto que todavía no existe (lo crea TT-CI-02, KNOCK-137): hasta entonces vive aquí, junto a las demás pruebas del documento.
/// </summary>
public sealed class ProfileOpenApiTests
{
    private static readonly string[] _acceptedPaths = ["/api/v1/me", "/api/v{version}/me"];

    [Fact]
    public async Task PutMeAndGetMeAreDeclared_TC_HU_002_14()
    {
        using var factory = new KnockApiFactory("Development");
        using var client = factory.CreateClient();

        using var response = await TestHttp.GetAsync(client, "/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = await TestHttp.ReadJsonAsync(response);
        var root = document.RootElement;
        var paths = root.GetProperty("paths");
        var path = _acceptedPaths.SingleOrDefault(p => paths.TryGetProperty(p, out _));
        path.Should().NotBeNull("el documento debe incluir la ruta /me");

        var put = paths.GetProperty(path).GetProperty("put").GetProperty("responses");
        put.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("200", "400", "401", "403", "413", "422", "429");

        var request = Resolve(root, paths.GetProperty(path).GetProperty("put").GetProperty("requestBody").GetProperty("content")
            .GetProperty("application/json").GetProperty("schema"));
        request.GetProperty("properties").EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("nombre");
        request.TryGetProperty("required", out _).Should().BeFalse("un nombre ausente responde 422 por campo, no el 400 de MVC");

        var get = Resolve(root, paths.GetProperty(path).GetProperty("get").GetProperty("responses").GetProperty("200")
            .GetProperty("content").GetProperty("application/json").GetProperty("schema"));
        get.GetProperty("properties").EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("id", "nombre", "perfilCompleto");
        get.TryGetProperty("required", out var required).Should().BeTrue();
        required.EnumerateArray().Select(e => e.GetString()).Should().Equal(["id"], "los campos nuevos son opcionales: el cambio es compatible");
    }

    /// <summary>Sigue un <c>$ref</c> local (<c>#/components/schemas/…</c>) si lo hay.</summary>
    private static JsonElement Resolve(JsonElement root, JsonElement schema)
    {
        if (!schema.TryGetProperty("$ref", out var reference))
        {
            return schema;
        }

        var target = root;
        foreach (var segment in reference.GetString()!.TrimStart('#', '/').Split('/'))
        {
            target = target.GetProperty(segment);
        }

        return target;
    }
}
