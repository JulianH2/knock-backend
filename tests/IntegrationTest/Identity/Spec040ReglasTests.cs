using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using StackExchange.Redis;
using Xunit;

namespace Knock.IntegrationTest.Identity;

/// <summary>Spec 040, T-06: reglas de negocio de las direcciones (R-10 a R-14): datos válidos, una sola principal, tope de 10, privacidad y propiedad. Solo datos ficticios.</summary>
public class Spec040ReglasTests
{
    private static string Uri(object id) => $"{AddressesHost.Route}/{id}";

    private static async Task<Dictionary<ObjectId, bool>> PrincipalsAsync(AddressesHost host, Spec040Person person)
    {
        var documents = await host.Addresses.Find(Builders<BsonDocument>.Filter.Eq("usuarioId", person.UserId)).ToListAsync();
        return documents.ToDictionary(document => document["_id"].AsObjectId, document => document["principal"].AsBoolean);
    }

    private static async Task<ObjectId> PostAsync(AddressesHost host, Spec040Person person, string street, bool? principal = null)
    {
        using var response = await host.SendAsync(HttpMethod.Post, AddressesHost.Route, person.Token, new { calleNumero = street, colonia = "Centro", principal });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return ObjectId.Parse(JsonElement.Parse(await response.Content.ReadAsStringAsync()).GetProperty("id").GetString()!);
    }

    [Theory]
    [InlineData("calleNumero", "Calle 1")]
    [InlineData("colonia", "Colonia Que No Existe")]
    [InlineData("referencias", "REFERENCIA-LARGA")]
    public async Task InvalidFields_Return422AndNothingIsSaved_CA_040_18(string field, string value)
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var body = new Dictionary<string, object?> { ["calleNumero"] = "Calle Falsa 123", ["colonia"] = "Centro", [field] = value == "REFERENCIA-LARGA" ? new string('r', 201) : value };

        using var response = await host.SendAsync(HttpMethod.Post, AddressesHost.Route, ana.Token, body);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await host.Addresses.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(0);
    }

    [Fact]
    public async Task FirstAddressIsPrincipal_AndAskingForPrincipalMovesTheMarkOnCreate_CA_040_19()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var first = await PostAsync(host, ana, "Calle Falsa 123", principal: false);
        (await PrincipalsAsync(host, ana))[first].Should().BeTrue("la primera dirección queda principal aunque no la marque");
        var second = await PostAsync(host, ana, "Avenida Siempre Viva 742");
        (await PrincipalsAsync(host, ana))[first].Should().BeTrue("una segunda dirección sin marcar no mueve la principal");
        var third = await PostAsync(host, ana, "Calle del Tercero 333", principal: true);

        var principals = await PrincipalsAsync(host, ana);

        principals[first].Should().BeFalse();
        principals[second].Should().BeFalse();
        principals[third].Should().BeTrue();
        principals.Values.Count(isPrincipal => isPrincipal).Should().Be(1);
    }

    [Fact]
    public async Task MarkingTheSecondAsPrincipal_UnmarksTheFirst_CA_040_19()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var first = await host.SeedAsync(ana, "Calle Falsa 123", "Centro", null, principal: true);
        var second = await host.SeedAsync(ana, "Avenida Siempre Viva 742", "Centro", null, principal: false);

        using var response = await host.SendAsync(HttpMethod.Put, Uri(second), ana.Token, new { principal = true });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonElement.Parse(await response.Content.ReadAsStringAsync()).GetProperty("principal").GetBoolean().Should().BeTrue();
        var principals = await PrincipalsAsync(host, ana);
        principals[first].Should().BeFalse();
        principals[second].Should().BeTrue();
    }

    [Fact]
    public async Task UnmarkingTheOnlyPrincipalDirectly_Returns422WithErrorsPrincipalAndChangesNothing_CA_040_19()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var first = await host.SeedAsync(ana, "Calle Falsa 123", "Centro", null, principal: true);

        using var response = await host.SendAsync(HttpMethod.Put, Uri(first), ana.Token, new { principal = false });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        JsonElement.Parse(await response.Content.ReadAsStringAsync()).GetProperty("errors").TryGetProperty("principal", out _).Should().BeTrue();
        (await PrincipalsAsync(host, ana))[first].Should().BeTrue();
    }

    [Fact]
    public async Task DeletingThePrincipal_PromotesTheMostRecentOne_CA_040_19()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var principal = await host.SeedAsync(ana, "Calle Falsa 123", "Centro", null, principal: true, createdAt: DateTime.UtcNow.AddDays(-5));
        var oldest = await host.SeedAsync(ana, "Calle Antigua 100", "Centro", null, principal: false, createdAt: DateTime.UtcNow.AddDays(-4));
        var newest = await host.SeedAsync(ana, "Calle Reciente 200", "Centro", null, principal: false, createdAt: DateTime.UtcNow.AddDays(-1));

        using var response = await host.SendAsync(HttpMethod.Delete, Uri(principal), ana.Token);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var principals = await PrincipalsAsync(host, ana);
        principals.Should().NotContainKey(principal);
        principals[newest].Should().BeTrue();
        principals[oldest].Should().BeFalse();
    }

    [Fact]
    public async Task DeletingANonPrincipal_KeepsThePrincipal_AndDeletingTheLastOneLeavesNone_CA_040_19()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var principal = await host.SeedAsync(ana, "Calle Falsa 123", "Centro", null, principal: true);
        var other = await host.SeedAsync(ana, "Avenida Siempre Viva 742", "Centro", null, principal: false);

        (await host.SendAsync(HttpMethod.Delete, Uri(other), ana.Token)).Dispose();
        (await PrincipalsAsync(host, ana)).Should().ContainSingle().Which.Should().Be(new KeyValuePair<ObjectId, bool>(principal, true));
        (await host.SendAsync(HttpMethod.Delete, Uri(principal), ana.Token)).Dispose();

        (await PrincipalsAsync(host, ana)).Should().BeEmpty();
    }

    [Fact]
    public async Task ThePrincipalOfAnotherPerson_IsNeverTouched_CA_040_19()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var beto = await host.NewPersonAsync();
        var ofBeto = await host.SeedAsync(beto, "Calle de Beto 999", "Centro", null, principal: true);

        var created = await PostAsync(host, ana, "Calle Falsa 123", principal: true);

        (await PrincipalsAsync(host, beto))[ofBeto].Should().BeTrue();
        (await PrincipalsAsync(host, ana))[created].Should().BeTrue();
    }

    [Fact]
    public async Task EleventhAddress_Returns409AddressLimitReachedAndIsNotSaved_CA_040_20()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        for (var index = 0; index < 10; index++)
        {
            await host.SeedAsync(ana, $"Calle Falsa {100 + index}", "Centro", null, principal: index == 0);
        }

        using var response = await host.SendAsync(HttpMethod.Post, AddressesHost.Route, ana.Token, new { calleNumero = "Calle Once 111", colonia = "Centro" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var raw = await response.Content.ReadAsStringAsync();
        JsonElement.Parse(raw).GetProperty("code").GetString().Should().Be("address_limit_reached");
        ProblemBody.Reasons(raw).Should().NotContain("Once");
        (await PrincipalsAsync(host, ana)).Should().HaveCount(10);
    }

    [Fact]
    public async Task TheLimitIsPerPerson_AndFreesUpWhenOneIsDeleted_CA_040_20()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var beto = await host.NewPersonAsync();
        var seeded = new List<ObjectId>();
        for (var index = 0; index < 10; index++)
        {
            seeded.Add(await host.SeedAsync(ana, $"Calle Falsa {100 + index}", "Centro", null, principal: index == 0));
        }

        using var ofBeto = await host.SendAsync(HttpMethod.Post, AddressesHost.Route, beto.Token, new { calleNumero = "Calle de Beto 999", colonia = "Centro" });
        (await host.SendAsync(HttpMethod.Delete, Uri(seeded[5]), ana.Token)).Dispose();
        using var afterDelete = await host.SendAsync(HttpMethod.Post, AddressesHost.Route, ana.Token, new { calleNumero = "Calle Nueva 555", colonia = "Centro" });

        ofBeto.StatusCode.Should().Be(HttpStatusCode.Created);
        afterDelete.StatusCode.Should().Be(HttpStatusCode.Created);
        (await PrincipalsAsync(host, ana)).Should().HaveCount(10);
    }

    [Fact]
    public async Task Address_IsNeverCachedNorStoredInPlain_AndEveryResponseIsPrivateAndNoStore_CA_040_21()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var created = await PostAsync(host, ana, "Calle Secretisima 123");
        var responses = new List<HttpResponseMessage>
        {
            await host.SendAsync(HttpMethod.Get, AddressesHost.Route, ana.Token),
            await host.SendAsync(HttpMethod.Put, Uri(created), ana.Token, new { referencias = "Referencia Secreta" }),
            await host.SendAsync(HttpMethod.Put, Uri(created), ana.Token, new { calleNumero = "x" }),
            await host.SendAsync(HttpMethod.Post, AddressesHost.Route, ana.Token, new { calleNumero = "x" }),
            await host.SendAsync(HttpMethod.Put, Uri(ObjectId.GenerateNewId()), ana.Token, new { colonia = "Centro" }),
            await host.SendAsync(HttpMethod.Delete, Uri("no-es-un-id"), ana.Token),
            await host.SendRawAsync(HttpMethod.Post, AddressesHost.Route, ana.Token, "{"),
            await host.SendAsync(HttpMethod.Delete, Uri(created), ana.Token),
        };

        foreach (var response in responses)
        {
            using (response)
            {
                response.Headers.CacheControl!.NoStore.Should().BeTrue($"{response.RequestMessage!.Method} {(int)response.StatusCode} no se cachea");
            }
        }

        (await host.SendAsync(HttpMethod.Get, AddressesHost.Route, ana.Token)).Dispose();
        var multiplexer = host.Factory.Services.GetRequiredService<IConnectionMultiplexer>();
        foreach (var endpoint in multiplexer.GetEndPoints())
        {
            await foreach (var key in multiplexer.GetServer(endpoint).KeysAsync(pattern: "*"))
            {
                key.ToString().Should().NotContainEquivalentOf("direccion");
                var value = (string?)await multiplexer.GetDatabase().StringGetAsync(key);
                (value ?? string.Empty).Should().NotContain("Secretisima").And.NotContain("Secreta");
            }
        }
    }

    [Fact]
    public async Task StoredAddressesNeverContainThePlainStreetOrReferences_CA_040_21()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        using var created = await host.SendAsync(HttpMethod.Post, AddressesHost.Route, ana.Token, new { calleNumero = "Calle Secretisima 123", colonia = "Centro", referencias = "Referencia Secreta" });

        var stored = await host.Addresses.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync();

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        stored.Should().ContainSingle().Which.ToJson().Should().NotContain("Secretisima").And.NotContain("Secreta");
    }

    [Fact]
    public async Task AddressOfAnotherPerson_Returns404OnEditAndDelete_AndAConfirmedAppointmentKeepsItsOwnCopy_CA_040_22()
    {
        await using var host = await AddressesHost.StartAsync();
        var ana = await host.NewPersonAsync();
        var beto = await host.NewPersonAsync();
        var id = await host.SeedAsync(ana, "Calle Falsa 123", "Centro", null, principal: true);
        var appointment = new BsonDocument { ["estado"] = "confirmada", ["lugar"] = new BsonDocument { ["calleNumero"] = "copia de la cita", ["colonia"] = "Centro" } };
        var visits = host.Factory.Database.GetCollection<BsonDocument>("visitas");
        await visits.InsertOneAsync(appointment);

        using var edit = await host.SendAsync(HttpMethod.Put, Uri(id), beto.Token, new { colonia = "Tultengo" });
        using var delete = await host.SendAsync(HttpMethod.Delete, Uri(id), beto.Token);
        using var ownDelete = await host.SendAsync(HttpMethod.Delete, Uri(id), ana.Token);

        edit.StatusCode.Should().Be(HttpStatusCode.NotFound);
        delete.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ownDelete.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await visits.Find(Builders<BsonDocument>.Filter.Empty).SingleAsync()).ToJson().Should().Be(appointment.ToJson());
    }
}
