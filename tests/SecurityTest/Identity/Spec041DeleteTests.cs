using System.Net;
using AwesomeAssertions;
using Knock.SecurityTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.SecurityTest.Identity;

/// <summary>Spec 041, CAS-04: <c>DELETE /api/v1/me/casa-y-auto/{id}</c>. Host real en Production con MongoDB y Redis reales; solo datos ficticios.</summary>
public class Spec041DeleteTests
{
    private static string Path(object id) => $"{HomeAndCarHttp.Route}/{id}";

    [Fact]
    public async Task TheOwnerRemovesTheItem_Returns204WithoutBody_CA_041_13()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var sub = TokenForge.Sub();
        var id = await HomeAndCarHttp.SeedAsync(hosts, sub);
        var kept = await HomeAndCarHttp.SeedAsync(hosts, sub, "otro", "Otro de prueba", null);

        using var response = await HomeAndCarHttp.SendAsync(hosts, HttpMethod.Delete, Path(id), TokenForge.Valid(sub));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
        (await HomeAndCarHttp.Items(hosts).CountDocumentsAsync(new BsonDocument("_id", id))).Should().Be(0);
        (await HomeAndCarHttp.Items(hosts).CountDocumentsAsync(new BsonDocument("_id", kept))).Should().Be(1, "solo se quita el elemento pedido");
    }

    [Theory]
    [InlineData("none")]
    [InlineData("expired")]
    public async Task WithoutTokenOrWithAnExpiredOne_Returns401_AndRemovesNothing_CA_041_14(string kind)
    {
        var hosts = await SecurityHosts.SharedAsync();
        var sub = TokenForge.Sub();
        var id = await HomeAndCarHttp.SeedAsync(hosts, sub);

        using var response = await HomeAndCarHttp.SendAsync(hosts, HttpMethod.Delete, Path(id), kind == "expired" ? TokenForge.Expired(sub) : null);

        await SecureHttp.AssertProblemAsync(response, HttpStatusCode.Unauthorized);
        (await HomeAndCarHttp.Items(hosts).CountDocumentsAsync(new BsonDocument("_id", id))).Should().Be(1);
    }

    // La spec lista 403 para quien no es dueño (CA-15), pero R-16 y CA-26 piden 404 «sin revelar que existe»: gana el 404 y nunca se quita nada (ver Memoria.md).
    [Fact]
    public async Task AnotherPersonsItem_Returns404AndRemovesNothing_CA_041_15()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var id = await HomeAndCarHttp.SeedAsync(hosts, TokenForge.Sub());

        using var response = await HomeAndCarHttp.SendAsync(hosts, HttpMethod.Delete, Path(id), TokenForge.Valid());

        await SecureHttp.AssertProblemAsync(response, HttpStatusCode.NotFound);
        (await HomeAndCarHttp.Items(hosts).CountDocumentsAsync(new BsonDocument("_id", id))).Should().Be(1);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12345")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzz")]
    [InlineData("0123456789abcdef0123456789")]
    public async Task AnInvalidId_Returns422WithoutRepeatingIt_AndRemovesNothing_CA_041_16(string id)
    {
        var hosts = await SecurityHosts.SharedAsync();
        var sub = TokenForge.Sub();
        await HomeAndCarHttp.SeedAsync(hosts, sub);

        using var response = await HomeAndCarHttp.SendAsync(hosts, HttpMethod.Delete, Path(id), TokenForge.Valid(sub));

        var raw = await SecureHttp.AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity);
        System.Text.Json.JsonElement.Parse(raw).GetProperty("errors").TryGetProperty("id", out _).Should().BeTrue();
        raw.Should().NotContain(id);
        (await HomeAndCarHttp.CountAsync(hosts, sub)).Should().Be(1);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("twice")]
    public async Task AnItemThatIsNotAvailable_Returns404ProblemDetails_CA_041_17(string kind)
    {
        var hosts = await SecurityHosts.SharedAsync();
        var sub = TokenForge.Sub();
        var id = ObjectId.GenerateNewId();
        if (kind == "twice")
        {
            id = await HomeAndCarHttp.SeedAsync(hosts, sub);
            using var first = await HomeAndCarHttp.SendAsync(hosts, HttpMethod.Delete, Path(id), TokenForge.Valid(sub));
            first.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using var response = await HomeAndCarHttp.SendAsync(hosts, HttpMethod.Delete, Path(id), TokenForge.Valid(sub));

        await SecureHttp.AssertProblemAsync(response, HttpStatusCode.NotFound);
    }
}
