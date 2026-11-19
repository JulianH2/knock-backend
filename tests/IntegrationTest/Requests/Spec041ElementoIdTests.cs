using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Requests;

/// <summary>
/// Spec 041, R-19 y CA-29 (T-00): <c>POST /solicitudes</c> acepta <c>elementoId</c> opcional como ampliación aditiva de la spec 020: sin el campo responde igual, con 24 caracteres
/// hexadecimales lo guarda y nunca lo devuelve, y con otro formato responde 422 <c>errors.elementoId</c>. MongoDB real, fotos y moderación de mentira.
/// </summary>
public sealed class Spec041ElementoIdTests
{
    [Fact]
    public async Task WithoutElementoId_ResponseAndStorageAreTheSameAsSpec020_CA_041_29()
    {
        await using var host = await Spec020Host.StartAsync();

        using var response = await host.CreateAsync(host.ValidBody());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await Spec020Host.ReadAsync(response);
        json.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(
            "id", "estado", "modalidad", "oficio", "colonia", "descripcion", "fotos", "dia", "franja", "direccion", "totalCotizaciones", "venceEn", "creadoEn");
        var stored = await host.Database.GetCollection<BsonDocument>("solicitudes").Find(new BsonDocument("_id", ObjectId.Parse(json.GetProperty("id").GetString()!))).SingleAsync();
        stored.Contains("elementoId").Should().BeFalse("sin el campo, el documento queda como antes");
    }

    [Fact]
    public async Task WithANullElementoId_IsTreatedAsAbsent_CA_041_29()
    {
        await using var host = await Spec020Host.StartAsync();
        var body = host.ValidBody();
        body["elementoId"] = null;

        using var response = await host.CreateAsync(body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = (await Spec020Host.ReadAsync(response)).GetProperty("id").GetString()!;
        (await host.Database.GetCollection<BsonDocument>("solicitudes").Find(new BsonDocument("_id", ObjectId.Parse(id))).SingleAsync()).Contains("elementoId").Should().BeFalse();
    }

    [Fact]
    public async Task WithAValidElementoId_ItIsSavedAndNeverReturned_CA_041_29()
    {
        await using var host = await Spec020Host.StartAsync();
        var elementId = ObjectId.GenerateNewId().ToString();
        var body = host.ValidBody();
        body["elementoId"] = elementId.ToUpperInvariant();

        using var response = await host.CreateAsync(body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().NotContain("elementoId").And.NotContain(elementId);
        var id = System.Text.Json.JsonElement.Parse(raw).GetProperty("id").GetString()!;
        var stored = await host.Database.GetCollection<BsonDocument>("solicitudes").Find(new BsonDocument("_id", ObjectId.Parse(id))).SingleAsync();
        stored["elementoId"].AsString.Should().Be(elementId, "se guarda en minúsculas, como los demás identificadores");
        var detail = await host.SendAsync(HttpMethod.Get, "/api/v1/solicitudes/" + id, host.ClientSub);
        (await detail.Content.ReadAsStringAsync()).Should().NotContain("elementoId", "ni el detalle ni la lista lo devuelven");
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0123456789abcdef0123456")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzz")]
    [InlineData("0123456789abcdef012345678")]
    [InlineData("")]
    [InlineData(" 0123456789abcdef01234567")]
    public async Task AMalformedElementoId_Returns422WithoutRepeatingIt_AndSavesNothing_CA_041_29(string value)
    {
        await using var host = await Spec020Host.StartAsync();
        var body = host.ValidBody();
        body["elementoId"] = value;

        using var response = await host.CreateAsync(body);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var raw = await response.Content.ReadAsStringAsync();
        System.Text.Json.JsonElement.Parse(raw).GetProperty("errors").TryGetProperty("elementoId", out _).Should().BeTrue();
        if (value.Length > 2)
        {
            ProblemBody.Reasons(raw).Should().NotContain(value);
        }

        (await host.CountRequestsAsync()).Should().Be(0);
    }
}
