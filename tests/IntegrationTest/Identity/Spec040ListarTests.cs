using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Identity;

/// <summary>Spec 040, T-02: <c>GET /api/v1/me/direcciones</c> (DIR-01) de punta a punta con MongoDB real. Solo datos ficticios.</summary>
public class Spec040ListarTests
{
    [Fact]
    public async Task OwnAddresses_AreListedDecryptedNewestFirstAndNeverTheOnesOfAnotherPerson_CA_040_01()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var beto = await host.NewPersonAsync();
        var older = DateTime.UtcNow.AddDays(-2);
        var newer = DateTime.UtcNow.AddDays(-1);
        await host.SeedAsync(ana, "Calle Falsa 123", "Centro", "Portón verde", principal: true, createdAt: older);
        await host.SeedAsync(ana, "Avenida Siempre Viva 742", "Tultengo", null, principal: false, createdAt: newer);
        await host.SeedAsync(beto, "Calle Secreta de Beto 999", "Centro", "Casa de Beto", principal: true);

        using var response = await host.SendAsync(HttpMethod.Get, AddressesHost.Route, ana.Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().NotContain("Beto").And.NotContain("Secreta");
        var items = JsonElement.Parse(raw).EnumerateArray().ToArray();
        items.Select(item => item.GetProperty("calleNumero").GetString()).Should().Equal("Avenida Siempre Viva 742", "Calle Falsa 123");
        items[1].GetProperty("colonia").GetString().Should().Be("Centro");
        items[1].GetProperty("referencias").GetString().Should().Be("Portón verde");
        items[1].GetProperty("principal").GetBoolean().Should().BeTrue();
        items[0].GetProperty("principal").GetBoolean().Should().BeFalse();
        items[0].TryGetProperty("usuarioId", out _).Should().BeFalse("la respuesta no trae el identificador de la persona");
        items[0].GetProperty("id").GetString().Should().HaveLength(24);
    }

    [Fact]
    public async Task PersonWithoutAddresses_GetsAnEmptyListWith200_CA_040_01()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();

        using var response = await host.SendAsync(HttpMethod.Get, AddressesHost.Route, ana.Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("[]");
    }

    [Fact]
    public async Task Response_IsPrivateAndNotCacheable_CA_040_01()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();

        using var response = await host.SendAsync(HttpMethod.Get, AddressesHost.Route, ana.Token);

        response.Headers.CacheControl!.Private.Should().BeTrue();
        response.Headers.CacheControl.NoStore.Should().BeTrue();
    }

    [Fact]
    public async Task WithoutTokenOrWithAnExpiredOrForgedToken_Returns401_CA_040_02()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        await host.SeedAsync(ana, "Calle Falsa 123", "Centro", null, principal: true);
        var forgedKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

        using var none = await host.SendAsync(HttpMethod.Get, AddressesHost.Route, null);
        using var expired = await host.SendAsync(HttpMethod.Get, AddressesHost.Route, AddressesHost.Token(ana.Sub, TimeSpan.FromMinutes(-5)));
        using var forged = await host.SendAsync(HttpMethod.Get, AddressesHost.Route, AddressesHost.Token(ana.Sub, key: forgedKey));

        foreach (var response in new[] { none, expired, forged })
        {
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await response.Content.ReadAsStringAsync()).Should().NotContain("Falsa");
        }
    }

    [Fact]
    public async Task TokenOfAnAccountThatDoesNotExist_Returns401AndLeaksNothing_CA_040_02()
    {
        await using var host = await AddressesHost.StartAsync();

        using var response = await host.SendAsync(HttpMethod.Get, AddressesHost.Route, AddressesHost.Token(Guid.NewGuid().ToString("D")));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task StoredDocument_HasNoPlainStreetOrReferences_CA_040_01()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        await host.SeedAsync(ana, "Calle Falsa 123", "Centro", "Portón verde", principal: true);

        var stored = await host.Addresses.Find(Builders<BsonDocument>.Filter.Empty).SingleAsync();

        stored.ToJson().Should().NotContain("Falsa").And.NotContain("Portón");
        stored["calleNumero"].AsString.Should().StartWith("1.");
    }
}
