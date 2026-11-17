using System.Text.Json.Nodes;
using Knock.IntegrationTest.Support;
using Knock.Market.Api.Modules.Requests.Services;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Xunit;

namespace Knock.DatabaseTest.Demo;

/// <summary>KNOCK-246: la validación que hace el cargador de solicitudes y cotizaciones de demo antes de escribir (sin base de datos).</summary>
public sealed class DemoRequestSeedValidationTests
{
    private static readonly string _market = Path.Combine(CatalogTestDatabase.SeedsDirectory, "demo", "market");

    private static (BsonDocument[] Requests, BsonDocument[] Quotes) Load(Action<JsonArray, JsonArray>? change = null)
    {
        var requests = JsonNode.Parse(File.ReadAllText(Path.Combine(_market, "solicitudes.json")))!.AsArray();
        var quotes = JsonNode.Parse(File.ReadAllText(Path.Combine(_market, "cotizaciones.json")))!.AsArray();
        change?.Invoke(requests, quotes);
        return (Parse(requests), Parse(quotes));
    }

    private static BsonDocument[] Parse(JsonArray array) => [.. BsonSerializer.Deserialize<BsonArray>(array.ToJsonString()).Select(x => x.AsBsonDocument)];

    [Fact]
    public void TheSeedFilesAreValid()
    {
        var (requests, quotes) = Load();

        DemoRequestSeeder.Validate(requests, quotes);

        Assert.Equal(2, requests.Length);
        Assert.Equal(3, quotes.Length);
    }

    [Fact]
    public void ARangeQuoteHasNoPriceFieldAndIsValid()
    {
        var (requests, quotes) = Load();

        var range = Assert.Single(quotes, x => x["tipoPrecio"] == "rango");
        Assert.False(range.Contains("precio"));
        DemoRequestSeeder.Validate(requests, quotes);
    }

    [Theory]
    [InlineData("extra")]
    [InlineData("estado")]
    [InlineData("demo")]
    [InlineData("duracion")]
    [InlineData("descripcion")]
    [InlineData("oficio")]
    [InlineData("fotos")]
    public void AnInvalidRequestIsRejected(string problem)
    {
        var (requests, quotes) = Load((r, _) =>
        {
            var first = r[0]!.AsObject();
            switch (problem)
            {
                case "extra": first["telefono"] = "5500000001"; break;
                case "estado": first["estado"] = "cancelada"; break;
                case "demo": first["demo"] = false; break;
                case "duracion": first["venceEn"] = "2026-12-05T12:00:00Z"; break;
                case "descripcion": first["descripcion"] = "Llamar a un número real"; break;
                case "oficio": first["oficioClave"] = "xxx"; break;
                default: first["fotos"] = new JsonArray(); break;
            }
        });

        Assert.Throws<InvalidOperationException>(() => DemoRequestSeeder.Validate(requests, quotes));
    }

    [Theory]
    [InlineData("solicitudHuerfana")]
    [InlineData("tipo")]
    [InlineData("rangoInvertido")]
    [InlineData("precioConRango")]
    [InlineData("duplicada")]
    [InlineData("hora")]
    [InlineData("usuario")]
    public void AnInvalidQuoteIsRejected(string problem)
    {
        var (requests, quotes) = Load((_, q) =>
        {
            var first = q[0]!.AsObject();
            var range = q.First(x => x!["tipoPrecio"]!.GetValue<string>() == "rango")!.AsObject();
            switch (problem)
            {
                case "solicitudHuerfana": first["solicitudId"] = new JsonObject { ["$oid"] = "eb00000000000000000000ff" }; break;
                case "tipo": first["tipoPrecio"] = "gratis"; break;
                case "rangoInvertido": range["precioMin"] = 900; break;
                case "precioConRango": range["precio"] = 700; break;
                case "duplicada": q[1]!["tecnicoId"] = first["tecnicoId"]!.DeepClone(); q[1]!["solicitudId"] = first["solicitudId"]!.DeepClone(); break;
                case "hora": first["hora"] = "25:00"; break;
                default: first["tecnicoUsuarioId"] = "no-es-un-uuid"; break;
            }
        });

        Assert.Throws<InvalidOperationException>(() => DemoRequestSeeder.Validate(requests, quotes));
    }

    [Fact]
    public void TheErrorNeverRepeatsAValueOfTheFile()
    {
        var (requests, quotes) = Load((r, _) => r[0]!["descripcion"] = "Texto con un secreto 555-0100");

        var error = Assert.Throws<InvalidOperationException>(() => DemoRequestSeeder.Validate(requests, quotes));

        Assert.DoesNotContain("555-0100", error.ToString(), StringComparison.Ordinal);
    }
}
