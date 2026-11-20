using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Identity;

/// <summary>Spec 040, T-03: <c>POST /api/v1/me/direcciones</c> (DIR-02) de punta a punta con MongoDB real. Solo datos ficticios.</summary>
public class Spec040CrearTests
{
    private static readonly object _valid = new { calleNumero = "Calle Falsa 123", colonia = "Centro", referencias = "Portón verde", principal = false };

    private static async Task<long> CountAsync(AddressesHost host) =>
        await host.Addresses.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);

    [Fact]
    public async Task ValidAddress_Returns201WithItsDataAndStoresItEncrypted_CA_040_03()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();

        using var response = await host.SendAsync(HttpMethod.Post, AddressesHost.Route, ana.Token, _valid);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = JsonElement.Parse(await response.Content.ReadAsStringAsync());
        body.GetProperty("calleNumero").GetString().Should().Be("Calle Falsa 123");
        body.GetProperty("colonia").GetString().Should().Be("Centro");
        body.GetProperty("referencias").GetString().Should().Be("Portón verde");
        body.GetProperty("principal").GetBoolean().Should().BeTrue("la primera dirección queda principal");
        body.GetProperty("id").GetString().Should().HaveLength(24);
        body.TryGetProperty("usuarioId", out _).Should().BeFalse();
        var stored = await host.Addresses.Find(Builders<BsonDocument>.Filter.Empty).SingleAsync();
        stored["usuarioId"].AsObjectId.Should().Be(ana.UserId);
        stored["_id"].AsObjectId.ToString().Should().Be(body.GetProperty("id").GetString());
        stored.ToJson().Should().NotContain("Falsa").And.NotContain("Portón");
        stored["colonia"].AsString.Should().Be("Centro");
        stored["creadoEn"].IsBsonDateTime.Should().BeTrue();
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
    }

    [Fact]
    public async Task ReferencesAreOptional_AndBlankOnesAreNotStored_CA_040_03()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();

        using var response = await host.SendAsync(HttpMethod.Post, AddressesHost.Route, ana.Token, new { calleNumero = "  Calle Falsa 123  ", colonia = " Centro ", referencias = "   " });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = JsonElement.Parse(await response.Content.ReadAsStringAsync());
        body.GetProperty("calleNumero").GetString().Should().Be("Calle Falsa 123");
        body.GetProperty("colonia").GetString().Should().Be("Centro");
        body.GetProperty("referencias").ValueKind.Should().Be(JsonValueKind.Null);
        (await host.Addresses.Find(Builders<BsonDocument>.Filter.Empty).SingleAsync()).Contains("referencias").Should().BeFalse();
    }

    [Fact]
    public async Task WithoutTokenOrWithAnExpiredToken_Returns401AndSavesNothing_CA_040_04()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();

        using var none = await host.SendAsync(HttpMethod.Post, AddressesHost.Route, null, _valid);
        using var expired = await host.SendAsync(HttpMethod.Post, AddressesHost.Route, AddressesHost.Token(ana.Sub, TimeSpan.FromMinutes(-5)), _valid);

        none.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        expired.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await CountAsync(host)).Should().Be(0);
    }

    [Theory]
    [InlineData("calleNumero", "Calle 1")]
    [InlineData("calleNumero", "       ")]
    [InlineData("calleNumero", "")]
    [InlineData("colonia", "Colonia Que No Existe")]
    [InlineData("colonia", "")]
    [InlineData("colonia", "Centro'; DROP TABLE direcciones;--")]
    [InlineData("colonia", "{\"$ne\":null}")]
    [InlineData("referencias", "REFERENCIA-LARGA")]
    public async Task InvalidValue_Returns422WithTheFieldAndDoesNotRepeatItNorSaveAnything_CA_040_05(string field, string value)
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var body = new Dictionary<string, object?> { ["calleNumero"] = "Calle Falsa 123", ["colonia"] = "Centro" };
        body[field] = value == "REFERENCIA-LARGA" ? new string('r', 201) : value;

        using var response = await host.SendAsync(HttpMethod.Post, AddressesHost.Route, ana.Token, body);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var raw = await response.Content.ReadAsStringAsync();
        JsonElement.Parse(raw).GetProperty("errors").TryGetProperty(field, out var reasons).Should().BeTrue();
        reasons.GetArrayLength().Should().BePositive();
        ProblemBody.Reasons(raw).Should().NotContain("DROP TABLE").And.NotContain("Colonia Que No Existe").And.NotContain(new string('r', 201));
        (await CountAsync(host)).Should().Be(0);
    }

    [Fact]
    public async Task MissingFields_Return422WithAnErrorPerField_CA_040_05()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();

        using var response = await host.SendAsync(HttpMethod.Post, AddressesHost.Route, ana.Token, new { });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var errors = JsonElement.Parse(await response.Content.ReadAsStringAsync()).GetProperty("errors");
        errors.TryGetProperty("calleNumero", out _).Should().BeTrue();
        errors.TryGetProperty("colonia", out _).Should().BeTrue();
        (await CountAsync(host)).Should().Be(0);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("""{"calleNumero":123,"colonia":"Centro"}""")]
    [InlineData("""{"calleNumero":"Calle Falsa 123","colonia":"Centro","principal":"si"}""")]
    [InlineData("""{"calleNumero":{"$gt":""},"colonia":"Centro"}""")]
    public async Task UnreadableBody_Returns400WithoutDotNetDetailsAndSavesNothing_CA_040_06(string json)
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();

        using var response = await host.SendRawAsync(HttpMethod.Post, AddressesHost.Route, ana.Token, json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().NotContain("System.").And.NotContain("LineNumber");
        (await CountAsync(host)).Should().Be(0);
    }

    [Fact]
    public async Task CatalogThatDoesNotAnswer_Returns503AndSavesNothing_CA_040_05()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        host.Colonias.Down = true;

        using var response = await host.SendAsync(HttpMethod.Post, AddressesHost.Route, ana.Token, _valid);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        (await response.Content.ReadAsStringAsync()).Should().Contain("market_unavailable").And.NotContain("Falsa");
        (await CountAsync(host)).Should().Be(0);
    }

    [Fact]
    public async Task TokenOfAnAccountThatDoesNotExist_Returns401AndSavesNothing_CA_040_04()
    {
        await using var host = await AddressesHost.StartAsync();

        using var response = await host.SendAsync(HttpMethod.Post, AddressesHost.Route, AddressesHost.Token(Guid.NewGuid().ToString("D")), _valid);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await CountAsync(host)).Should().Be(0);
    }
}
