using System.Net;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Identity;

/// <summary>Spec 040, T-07: la persona sale siempre del token, los registros no llevan datos personales, límites de peticiones y de cuerpo, y sin duplicados por repetición (R-15 a R-18).</summary>
public class Spec040TransversalesTests
{
    private const string _street = "Calle Secretisima 123";
    private const string _colonia = "Bosques del Valle";
    private const string _references = "Referencia Secreta";

    private static string Uri(object id) => $"{AddressesHost.Route}/{id}";

    [Fact]
    public async Task IdentifiersOfAnotherPersonInTheBodyRouteQueryOrHeaders_AreIgnored_CA_040_23()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var beto = await host.NewPersonAsync();
        var ofBeto = await host.SeedAsync(beto, "Calle de Beto 999", "Centro", null, principal: true);
        var ofAna = await host.SeedAsync(ana, "Calle Falsa 123", "Centro", null, principal: true);

        using var created = await host.SendRawAsync(
            HttpMethod.Post,
            AddressesHost.Route + $"?usuarioId={beto.UserId}&sub={beto.Sub}",
            ana.Token,
            $$"""{"calleNumero":"Avenida Siempre Viva 742","colonia":"Centro","usuarioId":"{{beto.UserId}}","sub":"{{beto.Sub}}","id":"{{ofBeto}}","creadoEn":"2000-01-01T00:00:00Z"}""");
        using var edited = await host.SendRawAsync(HttpMethod.Put, Uri(ofAna), ana.Token, $$"""{"colonia":"Tultengo","usuarioId":"{{beto.UserId}}","sub":"{{beto.Sub}}"}""");
        using var listing = new HttpRequestMessage(HttpMethod.Get, new Uri($"{AddressesHost.Route}?usuarioId={beto.UserId}&sub={beto.Sub}", UriKind.Relative));
        listing.Headers.TryAddWithoutValidation("Authorization", "Bearer " + ana.Token);
        listing.Headers.TryAddWithoutValidation("X-User-Id", beto.Sub);
        listing.Headers.TryAddWithoutValidation("X-Sub", beto.Sub);
        using var listed = await host.Client.SendAsync(listing);

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        edited.StatusCode.Should().Be(HttpStatusCode.OK);
        var newId = ObjectId.Parse(JsonElement.Parse(await created.Content.ReadAsStringAsync()).GetProperty("id").GetString()!);
        newId.Should().NotBe(ofBeto, "el identificador de la dirección lo genera el servidor");
        var stored = await host.Addresses.Find(Builders<BsonDocument>.Filter.Eq("_id", newId)).SingleAsync();
        stored["usuarioId"].AsObjectId.Should().Be(ana.UserId);
        stored["creadoEn"].ToUniversalTime().Should().BeAfter(DateTime.UtcNow.AddMinutes(-5));
        (await host.Addresses.Find(Builders<BsonDocument>.Filter.Eq("_id", ofBeto)).SingleAsync())["usuarioId"].AsObjectId.Should().Be(beto.UserId);
        (await host.Addresses.Find(Builders<BsonDocument>.Filter.Eq("_id", ofAna)).SingleAsync())["usuarioId"].AsObjectId.Should().Be(ana.UserId);
        var raw = await listed.Content.ReadAsStringAsync();
        raw.Should().NotContain("Beto").And.NotContain(beto.Sub);
        JsonElement.Parse(raw).GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task Logs_NeverCarryTheAddressTheSubOrTheTokenInAnyOutcome_CA_040_24()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var otherId = ObjectId.GenerateNewId();
        var fullPerson = await host.NewPersonAsync();
        for (var index = 0; index < 10; index++)
        {
            await host.SeedAsync(fullPerson, $"Calle Rellena {100 + index}", "Centro", null, principal: index == 0);
        }

        var output = await Knock.IntegrationTest.Support.ConsoleCapture.RunAsync(async () =>
        {
            using var created = await host.SendAsync(HttpMethod.Post, AddressesHost.Route, ana.Token, new { calleNumero = _street, colonia = _colonia, referencias = _references });
            var id = JsonElement.Parse(await created.Content.ReadAsStringAsync()).GetProperty("id").GetString();
            using var duplicate = await host.SendAsync(HttpMethod.Post, AddressesHost.Route, ana.Token, new { calleNumero = _street, colonia = _colonia, referencias = _references });
            using var invalid = await host.SendAsync(HttpMethod.Post, AddressesHost.Route, ana.Token, new { calleNumero = "corta " + _references, colonia = "Colonia Inventada Secreta" });
            using var unreadable = await host.SendRawAsync(HttpMethod.Post, AddressesHost.Route, ana.Token, "{\"calleNumero\":\"" + _street + "\"");
            using var edit = await host.SendAsync(HttpMethod.Put, Uri(id!), ana.Token, new { calleNumero = _street + " bis", referencias = _references + " bis" });
            using var missing = await host.SendAsync(HttpMethod.Put, Uri(otherId), ana.Token, new { calleNumero = _street });
            using var full = await host.SendAsync(HttpMethod.Post, AddressesHost.Route, fullPerson.Token, new { calleNumero = _street + " once", colonia = _colonia });
            host.Colonias.Down = true;
            using var down = await host.SendAsync(HttpMethod.Post, AddressesHost.Route, ana.Token, new { calleNumero = _street + " caido", colonia = _colonia });
            host.Colonias.Down = false;
            using var list = await host.SendAsync(HttpMethod.Get, AddressesHost.Route, ana.Token);
            using var delete = await host.SendAsync(HttpMethod.Delete, Uri(id!), ana.Token);
            using var anonymous = await host.SendAsync(HttpMethod.Get, AddressesHost.Route, null);
        });

        output.Should().NotBeNullOrWhiteSpace();
        output.Should().NotContain("Secretisima").And.NotContain("Secreta").And.NotContain(_colonia).And.NotContain("Inventada").And.NotContain("Rellena")
            .And.NotContain(ana.Sub).And.NotContain(fullPerson.Sub).And.NotContain(ana.Token).And.NotContain(ana.UserId.ToString());
    }

    [Fact]
    public async Task AboveTheRequestLimit_Returns429WithRetryAfterAndNoCounters_CA_040_25()
    {
        await using var host = await AddressesHost.StartAsync(new Dictionary<string, string?> { ["RateLimiting:Global:PermitLimit"] = "3" });
        var ana = await host.NewPersonAsync();
        for (var index = 0; index < 3; index++)
        {
            (await host.SendAsync(HttpMethod.Get, AddressesHost.Route, ana.Token)).Dispose();
        }

        using var response = await host.SendAsync(HttpMethod.Get, AddressesHost.Route, ana.Token);

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter!.Delta!.Value.TotalSeconds.Should().BePositive();
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    public async Task OversizedBody_Returns413AndSavesNothing_CA_040_25(string method)
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var id = await host.SeedAsync(ana, "Calle Falsa 123", "Centro", null, principal: true);
        var uri = method == "POST" ? AddressesHost.Route : Uri(id);
        var body = $$"""{"calleNumero":"{{new string('a', 5000)}}","colonia":"Centro"}""";

        using var response = await host.SendRawAsync(new HttpMethod(method), uri, ana.Token, body);

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        (await host.Addresses.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(1);
    }

    [Fact]
    public async Task SameCreationSentTwice_LeavesASingleAddressBecauseOfTheStateRule_CA_040_26()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var body = new { calleNumero = "Calle Falsa 123", colonia = "Centro" };

        using var first = await host.SendAsync(HttpMethod.Post, AddressesHost.Route, ana.Token, body);
        using var second = await host.SendAsync(HttpMethod.Post, AddressesHost.Route, ana.Token, new { calleNumero = "  calle FALSA 123 ", colonia = " Centro " });

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        JsonElement.Parse(await second.Content.ReadAsStringAsync()).GetProperty("code").GetString().Should().Be("address_duplicate");
        (await host.Addresses.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(1);
    }

    [Fact]
    public async Task SameCreationSentAtTheSameTime_LeavesASingleAddress_CA_040_26()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();

        var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ =>
            host.SendAsync(HttpMethod.Post, AddressesHost.Route, ana.Token, new { calleNumero = "Calle Falsa 123", colonia = "Centro" })));

        responses.Count(response => response.StatusCode == HttpStatusCode.Created).Should().Be(1);
        responses.Count(response => response.StatusCode == HttpStatusCode.Conflict).Should().Be(5);
        (await host.Addresses.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(1);
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [Fact]
    public async Task IdempotencyKeyHeader_IsNotUsed_TheStateRuleDecides_CA_040_26()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var statuses = new List<HttpStatusCode>();
        foreach (var key in new[] { "clave-1", "clave-2", "clave-1" })
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(AddressesHost.Route, UriKind.Relative))
            {
                Content = new StringContent("""{"calleNumero":"Calle Falsa 123","colonia":"Centro"}""", Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ana.Token);
            request.Headers.TryAddWithoutValidation("Idempotency-Key", key);
            using var response = await host.Client.SendAsync(request);
            statuses.Add(response.StatusCode);
        }

        statuses.Should().Equal(HttpStatusCode.Created, HttpStatusCode.Conflict, HttpStatusCode.Conflict);
        (await host.Addresses.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(1);
    }
}
