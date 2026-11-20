using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Identity;

/// <summary>Spec 040, T-05: <c>DELETE /api/v1/me/direcciones/{id}</c> (DIR-04) de punta a punta con MongoDB real. Solo datos ficticios.</summary>
public class Spec040BorrarTests
{
    private static string Uri(object id) => $"{AddressesHost.Route}/{id}";

    private static async Task<bool> ExistsAsync(AddressesHost host, ObjectId id) =>
        await host.Addresses.CountDocumentsAsync(Builders<BsonDocument>.Filter.Eq("_id", id)) == 1;

    [Fact]
    public async Task Owner_DeletesItsAddress_Returns204WithoutBodyAndKeepsTheOthers_CA_040_13()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var beto = await host.NewPersonAsync();
        var target = await host.SeedAsync(ana, "Calle Falsa 123", "Centro", "Portón verde", principal: true);
        var other = await host.SeedAsync(ana, "Avenida Siempre Viva 742", "Tultengo", null, principal: false);
        var ofBeto = await host.SeedAsync(beto, "Calle de Beto 999", "Centro", null, principal: true);

        using var response = await host.SendAsync(HttpMethod.Delete, Uri(target), ana.Token);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        (await ExistsAsync(host, target)).Should().BeFalse();
        (await ExistsAsync(host, other)).Should().BeTrue();
        (await ExistsAsync(host, ofBeto)).Should().BeTrue();
    }

    [Fact]
    public async Task WithoutTokenOrWithAnExpiredToken_Returns401AndDeletesNothing_CA_040_14()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var id = await host.SeedAsync(ana, "Calle Falsa 123", "Centro", null, principal: true);

        using var none = await host.SendAsync(HttpMethod.Delete, Uri(id), null);
        using var expired = await host.SendAsync(HttpMethod.Delete, Uri(id), AddressesHost.Token(ana.Sub, TimeSpan.FromMinutes(-5)));

        none.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        expired.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ExistsAsync(host, id)).Should().BeTrue();
    }

    // La spec 040 pide 403 para quien no es dueña (CA-15) y 404 para una dirección ajena sin revelar que existe (R-14 y CA-22). Aquí se aplica el 404 (ver la nota de CA_040_09).
    [Fact]
    public async Task PersonWhoDoesNotOwnTheAddress_GetsTheSame404AsAMissingOneAndDeletesNothing_CA_040_15()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var beto = await host.NewPersonAsync();
        var id = await host.SeedAsync(ana, "Calle Falsa 123", "Centro", null, principal: true);

        using var foreign = await host.SendAsync(HttpMethod.Delete, Uri(id), beto.Token);
        using var missing = await host.SendAsync(HttpMethod.Delete, Uri(ObjectId.GenerateNewId()), beto.Token);

        foreign.StatusCode.Should().Be(HttpStatusCode.NotFound);
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await foreign.Content.ReadAsStringAsync()).Should().NotContain("Falsa").And.NotContain(id.ToString());
        (await ExistsAsync(host, id)).Should().BeTrue();
    }

    [Theory]
    [InlineData("no-es-un-id")]
    [InlineData("123")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzz")]
    [InlineData("0123456789abcdef0123456789")]
    public async Task MalformedId_Returns422WithErrorsIdAndDoesNotEchoItNorDeleteAnything_CA_040_16(string id)
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var kept = await host.SeedAsync(ana, "Calle Falsa 123", "Centro", null, principal: true);

        using var response = await host.SendAsync(HttpMethod.Delete, Uri(id), ana.Token);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var raw = await response.Content.ReadAsStringAsync();
        JsonElement.Parse(raw).GetProperty("errors").TryGetProperty("id", out _).Should().BeTrue();
        ProblemBody.Reasons(raw).Should().NotContain(id);
        (await ExistsAsync(host, kept)).Should().BeTrue();
    }

    [Fact]
    public async Task AddressThatDoesNotExist_Returns404ProblemDetailsAndChangesNothing_CA_040_17()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var id = await host.SeedAsync(ana, "Calle Falsa 123", "Centro", null, principal: true);

        using var response = await host.SendAsync(HttpMethod.Delete, Uri(ObjectId.GenerateNewId()), ana.Token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = JsonElement.Parse(await response.Content.ReadAsStringAsync());
        problem.GetProperty("status").GetInt32().Should().Be(404);
        problem.TryGetProperty("correlationId", out _).Should().BeTrue();
        problem.TryGetProperty("traceId", out _).Should().BeTrue();
        (await ExistsAsync(host, id)).Should().BeTrue();
    }

    [Fact]
    public async Task DeletingTwice_ReturnsTheSecond404WithoutHarm_CA_040_17()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var id = await host.SeedAsync(ana, "Calle Falsa 123", "Centro", null, principal: true);

        using var first = await host.SendAsync(HttpMethod.Delete, Uri(id), ana.Token);
        using var second = await host.SendAsync(HttpMethod.Delete, Uri(id), ana.Token);

        first.StatusCode.Should().Be(HttpStatusCode.NoContent);
        second.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
