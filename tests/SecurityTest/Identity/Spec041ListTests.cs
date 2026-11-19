using System.Net;
using AwesomeAssertions;
using Knock.SecurityTest.Support;
using Xunit;

namespace Knock.SecurityTest.Identity;

/// <summary>Spec 041, CAS-01: <c>GET /api/v1/me/casa-y-auto</c>. Host real en Production con MongoDB y Redis reales; solo datos ficticios.</summary>
public class Spec041ListTests
{
    [Fact]
    public async Task APersonListsOnlyTheirOwnItems_Returns200_CA_041_01()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var sub = TokenForge.Sub();
        var other = TokenForge.Sub();
        var older = await HomeAndCarHttp.SeedAsync(hosts, sub, "auto", "Sedán de prueba", 2018, DateTime.UtcNow.AddDays(-2));
        var newer = await HomeAndCarHttp.SeedAsync(hosts, sub, "instalacion", "Boiler de prueba", null, DateTime.UtcNow.AddDays(-1));
        await HomeAndCarHttp.SeedAsync(hosts, other, "auto", "Auto de otra persona", 2019);

        using var response = await HomeAndCarHttp.SendAsync(hosts, HttpMethod.Get, HomeAndCarHttp.Route, TokenForge.Valid(sub));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        SecureHttp.AssertNotCacheable(response);
        var items = (await SecureHttp.ReadJsonAsync(response)).EnumerateArray().ToList();
        items.Select(item => item.GetProperty("id").GetString()).Should().Equal(newer.ToString(), older.ToString());
        items[1].GetProperty("tipo").GetString().Should().Be("auto");
        items[1].GetProperty("nombre").GetString().Should().Be("Sedán de prueba");
        items[1].GetProperty("anio").GetInt32().Should().Be(2018);
        items[1].TryGetProperty("creadoEn", out _).Should().BeTrue();
        items[0].GetProperty("anio").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Null, "un elemento sin año lo devuelve nulo");
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().NotContain("Auto de otra persona").And.NotContain(sub).And.NotContain("usuarioId");
    }

    [Fact]
    public async Task APersonWithoutItems_GetsAnEmptyList_CA_041_01()
    {
        var hosts = await SecurityHosts.SharedAsync();

        using var response = await HomeAndCarHttp.SendAsync(hosts, HttpMethod.Get, HomeAndCarHttp.Route, TokenForge.Valid());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await SecureHttp.ReadJsonAsync(response)).GetArrayLength().Should().Be(0);
    }

    [Theory]
    [InlineData("none")]
    [InlineData("expired")]
    public async Task WithoutTokenOrWithAnExpiredOne_Returns401_CA_041_02(string kind)
    {
        var hosts = await SecurityHosts.SharedAsync();
        var token = kind == "expired" ? TokenForge.Expired() : null;

        using var response = await HomeAndCarHttp.SendAsync(hosts, HttpMethod.Get, HomeAndCarHttp.Route, token);

        await SecureHttp.AssertProblemAsync(response, HttpStatusCode.Unauthorized);
    }
}
