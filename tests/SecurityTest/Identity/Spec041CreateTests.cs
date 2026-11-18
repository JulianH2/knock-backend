using System.Net;
using AwesomeAssertions;
using Knock.SecurityTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.SecurityTest.Identity;

/// <summary>Spec 041, CAS-02: <c>POST /api/v1/me/casa-y-auto</c>. Host real en Production con MongoDB y Redis reales; solo datos ficticios.</summary>
public class Spec041CreateTests
{
    [Fact]
    public async Task ACarWithAYear_Returns201WithTheItem_AndSavesItForTheTokenPerson_CA_041_03()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var sub = TokenForge.Sub();

        using var response = await HomeAndCarHttp.SendAsync(hosts, HttpMethod.Post, HomeAndCarHttp.Route, TokenForge.Valid(sub), new { tipo = "auto", nombre = "  Sedán de prueba  ", anio = 2018 });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        SecureHttp.AssertNotCacheable(response);
        var body = await SecureHttp.ReadJsonAsync(response);
        var id = body.GetProperty("id").GetString()!;
        body.GetProperty("tipo").GetString().Should().Be("auto");
        body.GetProperty("nombre").GetString().Should().Be("Sedán de prueba", "el nombre se guarda sin espacios en los extremos");
        body.GetProperty("anio").GetInt32().Should().Be(2018);
        body.TryGetProperty("creadoEn", out _).Should().BeTrue();
        response.Headers.Location!.ToString().Should().EndWith($"{HomeAndCarHttp.Route}/{id}");
        var stored = await HomeAndCarHttp.Items(hosts).Find(new BsonDocument("_id", ObjectId.Parse(id))).SingleAsync();
        stored["usuarioId"].AsString.Should().Be(sub);
        stored["anio"].ToInt32().Should().Be(2018);
    }

    [Theory]
    [InlineData("electrodomestico")]
    [InlineData("instalacion")]
    [InlineData("otro")]
    public async Task AnItemThatIsNotACar_IsSavedWithoutYear_CA_041_03(string tipo)
    {
        var hosts = await SecurityHosts.SharedAsync();
        var sub = TokenForge.Sub();

        using var response = await HomeAndCarHttp.SendAsync(hosts, HttpMethod.Post, HomeAndCarHttp.Route, TokenForge.Valid(sub), new { tipo, nombre = "Boiler de prueba" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await SecureHttp.ReadJsonAsync(response)).GetProperty("anio").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Null);
        (await HomeAndCarHttp.CountAsync(hosts, sub)).Should().Be(1);
    }

    [Theory]
    [InlineData("none")]
    [InlineData("expired")]
    public async Task WithoutTokenOrWithAnExpiredOne_Returns401_AndSavesNothing_CA_041_04(string kind)
    {
        var hosts = await SecurityHosts.SharedAsync();
        var marker = "Marca-" + Guid.NewGuid().ToString("N");

        using var response = await HomeAndCarHttp.SendAsync(hosts, HttpMethod.Post, HomeAndCarHttp.Route, kind == "expired" ? TokenForge.Expired() : null, new { tipo = "otro", nombre = marker });

        await SecureHttp.AssertProblemAsync(response, HttpStatusCode.Unauthorized);
        (await HomeAndCarHttp.Items(hosts).CountDocumentsAsync(new BsonDocument("nombre", marker))).Should().Be(0);
    }

    [Theory]
    [InlineData("""{"tipo":"vehiculo-raro","nombre":"Nombre válido"}""", "tipo")]
    [InlineData("""{"nombre":"Nombre válido"}""", "tipo")]
    [InlineData("""{"tipo":"AUTO","nombre":"Nombre válido"}""", "tipo")]
    [InlineData("""{"tipo":"otro"}""", "nombre")]
    [InlineData("""{"tipo":"otro","nombre":"X"}""", "nombre")]
    [InlineData("""{"tipo":"otro","nombre":"   "}""", "nombre")]
    [InlineData("""{"tipo":"otro","nombre":"Nombre\u0007con control"}""", "nombre")]
    [InlineData("""{"tipo":"auto","nombre":"Nombre válido","anio":1949}""", "anio")]
    [InlineData("""{"tipo":"auto","nombre":"Nombre válido","anio":9999}""", "anio")]
    [InlineData("""{"tipo":"auto","nombre":"Nombre válido","anio":0}""", "anio")]
    [InlineData("""{"tipo":"otro","nombre":"Nombre válido","anio":2020}""", "anio")]
    public async Task AnInvalidValue_Returns422WithTheFieldAndSavesNothing_CA_041_05(string json, string field)
    {
        var hosts = await SecurityHosts.SharedAsync();
        var sub = TokenForge.Sub();

        using var response = await HomeAndCarHttp.SendRawAsync(hosts, HttpMethod.Post, HomeAndCarHttp.Route, TokenForge.Valid(sub), json);

        var raw = await SecureHttp.AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity);
        System.Text.Json.JsonElement.Parse(raw).GetProperty("errors").TryGetProperty(field, out _).Should().BeTrue($"el error va en errors.{field}");
        raw.Should().NotContain("vehiculo-raro").And.NotContain("").And.NotContain("9999");
        (await HomeAndCarHttp.CountAsync(hosts, sub)).Should().Be(0);
    }

    [Fact]
    public async Task TheYearLimits_Are1950AndTheCurrentYearPlusOne_CA_041_05()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var sub = TokenForge.Sub();
        var nextYear = DateTime.UtcNow.Year + 1;

        using var low = await HomeAndCarHttp.SendAsync(hosts, HttpMethod.Post, HomeAndCarHttp.Route, TokenForge.Valid(sub), new { tipo = "auto", nombre = "Clásico de prueba", anio = 1950 });
        using var high = await HomeAndCarHttp.SendAsync(hosts, HttpMethod.Post, HomeAndCarHttp.Route, TokenForge.Valid(sub), new { tipo = "auto", nombre = "Modelo nuevo de prueba", anio = nextYear });
        using var over = await HomeAndCarHttp.SendAsync(hosts, HttpMethod.Post, HomeAndCarHttp.Route, TokenForge.Valid(sub), new { tipo = "auto", nombre = "Modelo futuro de prueba", anio = nextYear + 1 });

        low.StatusCode.Should().Be(HttpStatusCode.Created);
        high.StatusCode.Should().Be(HttpStatusCode.Created);
        over.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Theory]
    [InlineData("""{"tipo":"otro","nombre":""")]
    [InlineData("""{"tipo":5,"nombre":"Nombre válido"}""")]
    [InlineData("""{"tipo":"auto","nombre":"Nombre válido","anio":true}""")]
    [InlineData("""{"tipo":"auto","nombre":"Nombre válido","anio":2020.5}""")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("not json")]
    public async Task AnUnreadableBody_Returns400AndSavesNothing_CA_041_06(string json)
    {
        var hosts = await SecurityHosts.SharedAsync();
        var sub = TokenForge.Sub();

        using var response = await HomeAndCarHttp.SendRawAsync(hosts, HttpMethod.Post, HomeAndCarHttp.Route, TokenForge.Valid(sub), json);

        await SecureHttp.AssertProblemAsync(response, HttpStatusCode.BadRequest);
        (await HomeAndCarHttp.CountAsync(hosts, sub)).Should().Be(0);
    }

    [Fact]
    public async Task AnEmptyBody_Returns400AndSavesNothing_CA_041_06()
    {
        var hosts = await SecurityHosts.SharedAsync();

        using var response = await HomeAndCarHttp.SendRawAsync(hosts, HttpMethod.Post, HomeAndCarHttp.Route, TokenForge.Valid(), string.Empty);

        await SecureHttp.AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }
}
