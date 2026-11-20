using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Identity;

/// <summary>Spec 040, T-04: <c>PUT /api/v1/me/direcciones/{id}</c> (DIR-03) de punta a punta con MongoDB real. Solo datos ficticios.</summary>
public class Spec040EditarTests
{
    private static string Uri(object id) => $"{AddressesHost.Route}/{id}";

    private static async Task<BsonDocument> StoredAsync(AddressesHost host, ObjectId id) =>
        await host.Addresses.Find(Builders<BsonDocument>.Filter.Eq("_id", id)).SingleAsync();

    [Fact]
    public async Task Owner_ChangesOnlyTheSentFields_Returns200AndKeepsTheRestEncrypted_CA_040_07()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var created = DateTime.UtcNow.AddDays(-3);
        var id = await host.SeedAsync(ana, "Calle Falsa 123", "Centro", "Portón verde", principal: true, createdAt: created);
        var before = await StoredAsync(host, id);

        using var response = await host.SendAsync(HttpMethod.Put, Uri(id), ana.Token, new { colonia = "Tultengo" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = JsonElement.Parse(await response.Content.ReadAsStringAsync());
        body.GetProperty("id").GetString().Should().Be(id.ToString());
        body.GetProperty("colonia").GetString().Should().Be("Tultengo");
        body.GetProperty("calleNumero").GetString().Should().Be("Calle Falsa 123");
        body.GetProperty("referencias").GetString().Should().Be("Portón verde");
        body.GetProperty("principal").GetBoolean().Should().BeTrue();
        var after = await StoredAsync(host, id);
        after["colonia"].AsString.Should().Be("Tultengo");
        after["calleNumero"].AsString.Should().Be(before["calleNumero"].AsString, "la calle no se envió y no cambia");
        after["creadoEn"].Should().Be(before["creadoEn"]);
        after["usuarioId"].AsObjectId.Should().Be(ana.UserId);
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
    }

    [Fact]
    public async Task StreetAndReferences_AreReplacedAndStayEncrypted_CA_040_07()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var id = await host.SeedAsync(ana, "Calle Falsa 123", "Centro", "Portón verde", principal: true);

        using var response = await host.SendAsync(HttpMethod.Put, Uri(id), ana.Token, new { calleNumero = "  Avenida Siempre Viva 742 ", referencias = "Timbre roto" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = JsonElement.Parse(await response.Content.ReadAsStringAsync());
        body.GetProperty("calleNumero").GetString().Should().Be("Avenida Siempre Viva 742");
        body.GetProperty("referencias").GetString().Should().Be("Timbre roto");
        var stored = await StoredAsync(host, id);
        stored.ToJson().Should().NotContain("Siempre").And.NotContain("Timbre").And.NotContain("Falsa");
    }

    [Fact]
    public async Task BlankReferences_ClearThem_CA_040_07()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var id = await host.SeedAsync(ana, "Calle Falsa 123", "Centro", "Portón verde", principal: true);

        using var response = await host.SendAsync(HttpMethod.Put, Uri(id), ana.Token, new { referencias = "" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonElement.Parse(await response.Content.ReadAsStringAsync()).GetProperty("referencias").ValueKind.Should().Be(JsonValueKind.Null);
        (await StoredAsync(host, id)).Contains("referencias").Should().BeFalse();
    }

    [Fact]
    public async Task EmptyChange_Returns200WithTheCurrentAddress_CA_040_07()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var id = await host.SeedAsync(ana, "Calle Falsa 123", "Centro", null, principal: true);
        var before = await StoredAsync(host, id);

        using var response = await host.SendAsync(HttpMethod.Put, Uri(id), ana.Token, new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonElement.Parse(await response.Content.ReadAsStringAsync()).GetProperty("calleNumero").GetString().Should().Be("Calle Falsa 123");
        (await StoredAsync(host, id)).ToJson().Should().Be(before.ToJson());
    }

    [Fact]
    public async Task WithoutTokenOrWithAnExpiredToken_Returns401AndChangesNothing_CA_040_08()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var id = await host.SeedAsync(ana, "Calle Falsa 123", "Centro", null, principal: true);
        var before = await StoredAsync(host, id);

        using var none = await host.SendAsync(HttpMethod.Put, Uri(id), null, new { colonia = "Tultengo" });
        using var expired = await host.SendAsync(HttpMethod.Put, Uri(id), AddressesHost.Token(ana.Sub, TimeSpan.FromMinutes(-5)), new { colonia = "Tultengo" });

        none.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        expired.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await StoredAsync(host, id)).ToJson().Should().Be(before.ToJson());
    }

    // La spec 040 pide 403 para quien no es dueña (CA-09) y 404 para una dirección ajena sin revelar que existe (R-14 y CA-22). Aquí se aplica el 404: toda consulta filtra por la
    // cuenta del token, así que la dirección de otra persona no existe para quien la pide. Queda anotado en «Riesgos y dudas abiertas» de la spec.
    [Fact]
    public async Task PersonWhoDoesNotOwnTheAddress_GetsTheSame404AsAMissingOneAndChangesNothing_CA_040_09()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var beto = await host.NewPersonAsync();
        var id = await host.SeedAsync(ana, "Calle Falsa 123", "Centro", null, principal: true);
        var before = await StoredAsync(host, id);

        using var foreign = await host.SendAsync(HttpMethod.Put, Uri(id), beto.Token, new { calleNumero = "Calle de Beto 999", colonia = "Tultengo" });
        using var missing = await host.SendAsync(HttpMethod.Put, Uri(ObjectId.GenerateNewId()), beto.Token, new { colonia = "Tultengo" });

        foreign.StatusCode.Should().Be(HttpStatusCode.NotFound);
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await foreign.Content.ReadAsStringAsync()).Should().NotContain("Falsa").And.NotContain(id.ToString());
        (await StoredAsync(host, id)).ToJson().Should().Be(before.ToJson());
    }

    [Theory]
    [InlineData("calleNumero", "Calle 1")]
    [InlineData("calleNumero", "       ")]
    [InlineData("colonia", "Colonia Que No Existe")]
    [InlineData("colonia", "Centro'; DROP TABLE direcciones;--")]
    [InlineData("referencias", "REFERENCIA-LARGA")]
    public async Task InvalidValue_Returns422WithTheFieldAndDoesNotRepeatItNorChangeAnything_CA_040_10(string field, string value)
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var id = await host.SeedAsync(ana, "Calle Falsa 123", "Centro", "Portón verde", principal: true);
        var before = await StoredAsync(host, id);
        var body = new Dictionary<string, object?> { [field] = value == "REFERENCIA-LARGA" ? new string('r', 201) : value };

        using var response = await host.SendAsync(HttpMethod.Put, Uri(id), ana.Token, body);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var raw = await response.Content.ReadAsStringAsync();
        JsonElement.Parse(raw).GetProperty("errors").TryGetProperty(field, out _).Should().BeTrue();
        ProblemBody.Reasons(raw).Should().NotContain("DROP TABLE").And.NotContain("Colonia Que No Existe").And.NotContain(new string('r', 201));
        (await StoredAsync(host, id)).ToJson().Should().Be(before.ToJson());
    }

    [Theory]
    [InlineData("no-es-un-id")]
    [InlineData("123")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzz")]
    public async Task MalformedId_Returns422WithErrorsIdAndDoesNotEchoIt_CA_040_10(string id)
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();

        using var response = await host.SendAsync(HttpMethod.Put, Uri(id), ana.Token, new { colonia = "Centro" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var raw = await response.Content.ReadAsStringAsync();
        JsonElement.Parse(raw).GetProperty("errors").TryGetProperty("id", out _).Should().BeTrue();
        ProblemBody.Reasons(raw).Should().NotContain(id);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("""{"calleNumero":123}""")]
    [InlineData("""{"colonia":{"$ne":null}}""")]
    [InlineData("""{"principal":"si"}""")]
    public async Task UnreadableBody_Returns400AndChangesNothing_CA_040_11(string json)
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var id = await host.SeedAsync(ana, "Calle Falsa 123", "Centro", null, principal: true);
        var before = await StoredAsync(host, id);

        using var response = await host.SendRawAsync(HttpMethod.Put, Uri(id), ana.Token, json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().NotContain("System.").And.NotContain("LineNumber");
        (await StoredAsync(host, id)).ToJson().Should().Be(before.ToJson());
    }

    [Fact]
    public async Task AddressThatDoesNotExist_Returns404ProblemDetailsAndChangesNothing_CA_040_12()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var id = await host.SeedAsync(ana, "Calle Falsa 123", "Centro", null, principal: true);

        using var response = await host.SendAsync(HttpMethod.Put, Uri(ObjectId.GenerateNewId()), ana.Token, new { colonia = "Tultengo" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = JsonElement.Parse(await response.Content.ReadAsStringAsync());
        problem.GetProperty("status").GetInt32().Should().Be(404);
        problem.TryGetProperty("correlationId", out _).Should().BeTrue();
        problem.TryGetProperty("traceId", out _).Should().BeTrue();
        (await StoredAsync(host, id))["colonia"].AsString.Should().Be("Centro");
    }

    [Fact]
    public async Task CatalogThatDoesNotAnswer_Returns503AndChangesNothing_CA_040_10()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var id = await host.SeedAsync(ana, "Calle Falsa 123", "Centro", null, principal: true);
        host.Colonias.Down = true;

        using var response = await host.SendAsync(HttpMethod.Put, Uri(id), ana.Token, new { colonia = "Tultengo" });

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        (await StoredAsync(host, id))["colonia"].AsString.Should().Be("Centro");
    }

    [Fact]
    public async Task EditWithoutColonia_DoesNotAskTheCatalog_CA_040_07()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var id = await host.SeedAsync(ana, "Calle Falsa 123", "Centro", null, principal: true);
        host.Colonias.Down = true;

        using var response = await host.SendAsync(HttpMethod.Put, Uri(id), ana.Token, new { calleNumero = "Avenida Siempre Viva 742" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        host.Colonias.Calls.Should().Be(0);
    }
}
