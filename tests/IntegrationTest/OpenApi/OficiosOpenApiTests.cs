using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.IntegrationTest.OpenApi;

/// <summary>TT-PRU-01: el contrato se genera del código y documenta <c>GET /api/v1/oficios</c> (controller internal descubierto por MVC).</summary>
public sealed class OficiosOpenApiTests
{
    /// <summary>Sin Asp.Versioning.Mvc.ApiExplorer la ruta puede salir con la plantilla de versión (spec §8 D5): se aceptan las dos.</summary>
    private static readonly string[] _acceptedPaths = ["/api/v1/oficios", "/api/v{version}/oficios"];

    [Fact]
    public async Task GetOpenApi_Development_DocumentsOficiosOperation_TC_TT_PRU_01_06()
    {
        using var factory = new KnockApiFactory("Development");
        using var client = factory.CreateClient();

        using var response = await TestHttp.GetAsync(client, "/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = await TestHttp.ReadJsonAsync(response);
        var root = document.RootElement;
        var paths = root.GetProperty("paths");
        var path = _acceptedPaths.SingleOrDefault(p => paths.TryGetProperty(p, out _));
        path.Should().NotBeNull("el documento debe incluir la ruta de oficios");

        var schema = paths.GetProperty(path).GetProperty("get").GetProperty("responses").GetProperty("200")
            .GetProperty("content").GetProperty("application/json").GetProperty("schema");
        schema.GetProperty("type").GetString().Should().Be("array");
        var properties = Resolve(root, schema.GetProperty("items")).GetProperty("properties");
        properties.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("clave", "nombre", "modalidad");
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
