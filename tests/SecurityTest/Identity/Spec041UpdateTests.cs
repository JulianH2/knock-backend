using System.Net;
using AwesomeAssertions;
using Knock.SecurityTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.SecurityTest.Identity;

/// <summary>Spec 041, CAS-03: <c>PUT /api/v1/me/casa-y-auto/{id}</c>. Host real en Production con MongoDB y Redis reales; solo datos ficticios.</summary>
public class Spec041UpdateTests
{
    private static string Path(object id) => $"{HomeAndCarHttp.Route}/{id}";

    private static async Task<BsonDocument> StoredAsync(SecurityHosts hosts, ObjectId id) =>
        await HomeAndCarHttp.Items(hosts).Find(new BsonDocument("_id", id)).SingleAsync();

    [Fact]
    public async Task TheOwnerChangesOnlyTheSentFields_Returns200_CA_041_07()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var sub = TokenForge.Sub();
        var id = await HomeAndCarHttp.SeedAsync(hosts, sub, "auto", "Sedán de prueba", 2018);

        using var onlyName = await HomeAndCarHttp.SendAsync(hosts, HttpMethod.Put, Path(id), TokenForge.Valid(sub), new { nombre = "  Sedán renombrado  " });

        onlyName.StatusCode.Should().Be(HttpStatusCode.OK);
        SecureHttp.AssertNotCacheable(onlyName);
        var first = await SecureHttp.ReadJsonAsync(onlyName);
        first.GetProperty("id").GetString().Should().Be(id.ToString());
        first.GetProperty("nombre").GetString().Should().Be("Sedán renombrado");
        first.GetProperty("tipo").GetString().Should().Be("auto", "lo que no se envía no cambia");
        first.GetProperty("anio").GetInt32().Should().Be(2018, "lo que no se envía no cambia");

        using var onlyYear = await HomeAndCarHttp.SendAsync(hosts, HttpMethod.Put, Path(id), TokenForge.Valid(sub), new { anio = 2021 });

        onlyYear.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await StoredAsync(hosts, id);
        stored["nombre"].AsString.Should().Be("Sedán renombrado");
        stored["anio"].ToInt32().Should().Be(2021);
        stored["usuarioId"].AsString.Should().Be(sub);
    }

    [Fact]
    public async Task ChangingACarIntoAnotherType_DropsTheYear_CA_041_07()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var sub = TokenForge.Sub();
        var id = await HomeAndCarHttp.SeedAsync(hosts, sub, "auto", "Camioneta de prueba", 2015);

        using var response = await HomeAndCarHttp.SendAsync(hosts, HttpMethod.Put, Path(id), TokenForge.Valid(sub), new { tipo = "otro" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await SecureHttp.ReadJsonAsync(response)).GetProperty("anio").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Null);
        (await StoredAsync(hosts, id)).Contains("anio").Should().BeFalse("el año solo existe en los autos");
    }

    [Fact]
    public async Task AnEmptyChange_Returns200WithTheItemAsItWas_CA_041_07()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var sub = TokenForge.Sub();
        var id = await HomeAndCarHttp.SeedAsync(hosts, sub, "instalacion", "Tinaco de prueba", null);

        using var response = await HomeAndCarHttp.SendAsync(hosts, HttpMethod.Put, Path(id), TokenForge.Valid(sub), new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await SecureHttp.ReadJsonAsync(response)).GetProperty("nombre").GetString().Should().Be("Tinaco de prueba");
    }

    [Theory]
    [InlineData("none")]
    [InlineData("expired")]
    public async Task WithoutTokenOrWithAnExpiredOne_Returns401_AndChangesNothing_CA_041_08(string kind)
    {
        var hosts = await SecurityHosts.SharedAsync();
        var sub = TokenForge.Sub();
        var id = await HomeAndCarHttp.SeedAsync(hosts, sub);

        using var response = await HomeAndCarHttp.SendAsync(hosts, HttpMethod.Put, Path(id), kind == "expired" ? TokenForge.Expired(sub) : null, new { nombre = "Nombre cambiado" });

        await SecureHttp.AssertProblemAsync(response, HttpStatusCode.Unauthorized);
        (await StoredAsync(hosts, id))["nombre"].AsString.Should().Be("Auto de prueba");
    }

    // La spec lista 403 para quien no es dueño (CA-09), pero R-16 y CA-26 piden 404 «sin revelar que existe»: gana el 404 y nunca se cambia nada (ver Memoria.md).
    [Fact]
    public async Task AnotherPersonsItem_Returns404AndChangesNothing_CA_041_09()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var owner = TokenForge.Sub();
        var id = await HomeAndCarHttp.SeedAsync(hosts, owner);

        using var response = await HomeAndCarHttp.SendAsync(hosts, HttpMethod.Put, Path(id), TokenForge.Valid(), new { nombre = "Nombre cambiado" });

        await SecureHttp.AssertProblemAsync(response, HttpStatusCode.NotFound);
        var stored = await StoredAsync(hosts, id);
        stored["nombre"].AsString.Should().Be("Auto de prueba");
        stored["usuarioId"].AsString.Should().Be(owner);
    }

    [Theory]
    [InlineData("{\"tipo\":\"vehiculo-raro\"}", "tipo")]
    [InlineData("{\"nombre\":\"X\"}", "nombre")]
    [InlineData("{\"nombre\":\"Nombre\\u0007con control\"}", "nombre")]
    [InlineData("{\"anio\":1949}", "anio")]
    [InlineData("{\"anio\":9999}", "anio")]
    [InlineData("{\"tipo\":\"otro\",\"anio\":2020}", "anio")]
    public async Task AnInvalidValue_Returns422WithTheFieldAndChangesNothing_CA_041_10(string json, string field)
    {
        var hosts = await SecurityHosts.SharedAsync();
        var sub = TokenForge.Sub();
        var id = await HomeAndCarHttp.SeedAsync(hosts, sub, "auto", "Auto de prueba", 2020);

        using var response = await HomeAndCarHttp.SendRawAsync(hosts, HttpMethod.Put, Path(id), TokenForge.Valid(sub), json);

        var raw = await SecureHttp.AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity);
        System.Text.Json.JsonElement.Parse(raw).GetProperty("errors").TryGetProperty(field, out _).Should().BeTrue($"el error va en errors.{field}");
        raw.Should().NotContain("vehiculo-raro").And.NotContain("9999").And.NotContain("\\u0007");
        var stored = await StoredAsync(hosts, id);
        stored["nombre"].AsString.Should().Be("Auto de prueba");
        stored["tipo"].AsString.Should().Be("auto");
        stored["anio"].ToInt32().Should().Be(2020);
    }

    [Fact]
    public async Task AYearOnAnItemThatIsNotACar_Returns422_CA_041_10()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var sub = TokenForge.Sub();
        var id = await HomeAndCarHttp.SeedAsync(hosts, sub, "electrodomestico", "Lavadora de prueba", null);

        using var response = await HomeAndCarHttp.SendAsync(hosts, HttpMethod.Put, Path(id), TokenForge.Valid(sub), new { anio = 2020 });

        var raw = await SecureHttp.AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity);
        System.Text.Json.JsonElement.Parse(raw).GetProperty("errors").TryGetProperty("anio", out _).Should().BeTrue();
        (await StoredAsync(hosts, id)).Contains("anio").Should().BeFalse();
    }

    [Theory]
    [InlineData("{\"nombre\":")]
    [InlineData("{\"tipo\":5}")]
    [InlineData("{\"anio\":2020.5}")]
    [InlineData("null")]
    [InlineData("not json")]
    [InlineData("")]
    public async Task AnUnreadableBody_Returns400AndChangesNothing_CA_041_11(string json)
    {
        var hosts = await SecurityHosts.SharedAsync();
        var sub = TokenForge.Sub();
        var id = await HomeAndCarHttp.SeedAsync(hosts, sub);

        using var response = await HomeAndCarHttp.SendRawAsync(hosts, HttpMethod.Put, Path(id), TokenForge.Valid(sub), json);

        await SecureHttp.AssertProblemAsync(response, HttpStatusCode.BadRequest);
        (await StoredAsync(hosts, id))["nombre"].AsString.Should().Be("Auto de prueba");
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("deleted")]
    public async Task AnItemThatIsNotAvailable_Returns404ProblemDetails_CA_041_12(string kind)
    {
        var hosts = await SecurityHosts.SharedAsync();
        var sub = TokenForge.Sub();
        var id = ObjectId.GenerateNewId();
        if (kind == "deleted")
        {
            id = await HomeAndCarHttp.SeedAsync(hosts, sub);
            await HomeAndCarHttp.Items(hosts).DeleteOneAsync(new BsonDocument("_id", id));
        }

        using var response = await HomeAndCarHttp.SendAsync(hosts, HttpMethod.Put, Path(id), TokenForge.Valid(sub), new { nombre = "Nombre cambiado" });

        await SecureHttp.AssertProblemAsync(response, HttpStatusCode.NotFound);
        (await HomeAndCarHttp.Items(hosts).CountDocumentsAsync(new BsonDocument("_id", id))).Should().Be(0, "no se crea nada");
    }
}
