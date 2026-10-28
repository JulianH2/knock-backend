using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Knock.Api.Modules.Catalog.Dtos;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.IntegrationTest.OpenApi;

public sealed class NeighborhoodAtPointOpenApiTests
{
    [Fact]
    public async Task OpenApiDocumentsPointLookupContract_TC_TT_API_07_13()
    {
        using var factory = new KnockApiFactory("Development");
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        // El generador documenta toda la API con la plantilla de versión (`/api/v{version}/…`), igual que las rutas existentes.
        var operation = root.GetProperty("paths").GetProperty("/api/v{version}/colonias/en-punto").GetProperty("get");

        var parameters = operation.GetProperty("parameters").EnumerateArray()
            .Where(p => p.GetProperty("in").GetString() != "path")
            .ToList();
        parameters.Select(p => p.GetProperty("name").GetString()).Should().BeEquivalentTo("lat", "lon");
        foreach (var parameter in parameters)
        {
            var name = parameter.GetProperty("name").GetString();
            parameter.GetProperty("in").GetString().Should().Be("query", name);
            parameter.GetProperty("required").GetBoolean().Should().BeTrue(name);
            parameter.GetProperty("description").GetString().Should().Contain(name == "lat" ? "-90 to 90" : "-180 to 180");
            var schema = parameter.GetProperty("schema");
            TypeOf(schema).Should().Be("string", $"{name} se documenta como string, no como array ni número");
            schema.GetProperty("pattern").GetString().Should().Be(NeighborhoodAtPointQuery.Pattern);
            schema.GetProperty("maxLength").GetInt32().Should().Be(16);
        }

        var responses = operation.GetProperty("responses");
        var ok = Resolve(root, responses.GetProperty("200").GetProperty("content").GetProperty("application/json").GetProperty("schema"));
        ok.GetProperty("required").EnumerateArray().Select(r => r.GetString()).Should().Contain("enCobertura");
        TypeOf(ok.GetProperty("properties").GetProperty("enCobertura")).Should().Be("boolean");
        var colonia = ok.GetProperty("properties").GetProperty("colonia");
        IsNullable(root, colonia).Should().BeTrue("colonia es null fuera de cobertura");
        var coloniaSchema = Resolve(root, colonia);
        coloniaSchema.GetProperty("properties").EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("id", "nombre", "cp", "municipio");
        foreach (var property in coloniaSchema.GetProperty("properties").EnumerateObject())
        {
            TypeOf(property.Value).Should().Be("string", property.Name);
        }

        foreach (var status in new[] { "401", "422", "429" })
        {
            responses.GetProperty(status).GetProperty("content").TryGetProperty("application/problem+json", out _).Should().BeTrue(status);
        }
    }

    /// <summary>Tipo del esquema sin el <c>"null"</c> que agrega OpenAPI 3.1 a los nulables.</summary>
    private static string? TypeOf(JsonElement schema)
    {
        if (!schema.TryGetProperty("type", out var type))
        {
            return null;
        }

        return type.ValueKind == JsonValueKind.Array
            ? type.EnumerateArray().Select(t => t.GetString()).Single(t => t != "null")
            : type.GetString();
    }

    private static bool IsNullable(JsonElement root, JsonElement schema) =>
        (schema.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.Array && type.EnumerateArray().Any(t => t.GetString() == "null"))
        || (schema.TryGetProperty("oneOf", out var oneOf) && oneOf.EnumerateArray().Any(s => s.TryGetProperty("type", out var t) && t.GetString() == "null"))
        || (schema.TryGetProperty("anyOf", out var anyOf) && anyOf.EnumerateArray().Any(s => s.TryGetProperty("type", out var t) && t.GetString() == "null"))
        || (schema.TryGetProperty("$ref", out _) && IsNullable(root, Resolve(root, schema)));

    /// <summary>Sigue <c>$ref</c> (también dentro de <c>oneOf</c>/<c>anyOf</c> con <c>null</c>) hasta el esquema con propiedades.</summary>
    private static JsonElement Resolve(JsonElement root, JsonElement schema)
    {
        if (schema.TryGetProperty("$ref", out var reference))
        {
            var current = root;
            foreach (var segment in reference.GetString()!.TrimStart('#', '/').Split('/'))
            {
                current = current.GetProperty(segment);
            }

            return Resolve(root, current);
        }

        foreach (var combinator in new[] { "oneOf", "anyOf" })
        {
            if (schema.TryGetProperty(combinator, out var options))
            {
                return Resolve(root, options.EnumerateArray().First(o => o.TryGetProperty("$ref", out _) || o.TryGetProperty("properties", out _)));
            }
        }

        return schema;
    }
}
