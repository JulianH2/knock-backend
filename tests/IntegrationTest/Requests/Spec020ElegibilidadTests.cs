using System.Net;
using AwesomeAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Requests;

/// <summary>Spec 020, R-23 y CA-23: elegibilidad base del técnico para ver y cotizar una solicitud (MongoDB real). El resultado no depende de <c>Oleadas:Habilitado</c> (spec 028).</summary>
public sealed class Spec020ElegibilidadTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Base_Con_Y_Sin_Oleadas_CA_020_23(bool oleadas)
    {
        await using var host = await Spec020Host.StartAsync(new Dictionary<string, string?> { ["Oleadas:Habilitado"] = oleadas ? "true" : "false" });
        using var created = await host.CreateAsync(host.ValidBody());
        var id = (await Spec020Host.ReadAsync(created)).GetProperty("id").GetString()!;
        var elsewhere = await host.SeedColoniaAsync("Lejana", "42999");
        var eligible = await SeedAsync(host, "activo", false, Spec020Host.Plumbing, host.ColoniaId);
        var otherTrade = await SeedAsync(host, "activo", false, Spec020Host.Workshop, host.ColoniaId);
        var otherZone = await SeedAsync(host, "activo", false, Spec020Host.Plumbing, elsewhere);
        var suspended = await SeedAsync(host, "suspendido", false, Spec020Host.Plumbing, host.ColoniaId);
        var hidden = await SeedAsync(host, "activo", true, Spec020Host.Plumbing, host.ColoniaId);

        var results = new List<(string Name, HttpStatusCode Detail, HttpStatusCode Quote)>();
        foreach (var (name, sub) in new[] { ("elegible", eligible), ("otro oficio", otherTrade), ("otra zona", otherZone), ("suspendido", suspended), ("oculto", hidden) })
        {
            using var detail = await host.SendAsync(HttpMethod.Get, "/api/v1/solicitudes/" + id, sub);
            using var quote = await host.SendAsync(HttpMethod.Post, $"/api/v1/solicitudes/{id}/cotizaciones", sub, Quote());
            results.Add((name, detail.StatusCode, quote.StatusCode));
        }

        results.Single(r => r.Name == "elegible").Detail.Should().Be(HttpStatusCode.OK);
        results.Single(r => r.Name == "elegible").Quote.Should().Be(HttpStatusCode.Created);
        foreach (var rejected in results.Where(r => r.Name != "elegible"))
        {
            rejected.Detail.Should().Be(HttpStatusCode.NotFound, "{0} no ve la solicitud", rejected.Name);
            rejected.Quote.Should().Be(HttpStatusCode.NotFound, "{0} no puede cotizarla", rejected.Name);
        }

        var quotes = await host.Database.GetCollection<BsonDocument>("cotizaciones").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        quotes.Should().Be(1, "solo cotizó el técnico elegible");
    }

    private static async Task<string> SeedAsync(Spec020Host host, string state, bool hidden, string trade, ObjectId colonia)
    {
        var sub = Guid.NewGuid().ToString("D");
        await host.SeedTechnicianAsync(sub, state, hidden, [trade], colonia);
        return sub;
    }

    private static Dictionary<string, object?> Quote() => new()
    {
        ["tipoPrecio"] = "fijo",
        ["precio"] = 100,
        ["dia"] = Spec020Host.Tomorrow(),
        ["hora"] = "12:00",
    };
}
