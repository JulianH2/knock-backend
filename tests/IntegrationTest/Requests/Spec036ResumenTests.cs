extern alias MarketApi;

using System.Net;
using System.Text.Json;
using Knock.IntegrationTest.Support;
using MarketApi::Knock.Market.Api.Common.Crypto;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Requests;

public sealed class Spec036ResumenTests
{
    private const string _route = "/api/v1/tecnicos/me/trabajo";

    [Fact]
    public async Task Visitas_Y_Terminadas_Por_Estado_CA_036_01()
    {
        await using var host = await Spec020Host.StartAsync();
        var sub = Guid.NewGuid().ToString();
        await SeedAsync(host, sub);
        using var response = await host.SendAsync(HttpMethod.Get, _route, sub);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await Spec020Host.ReadAsync(response);
        var upcoming = json.GetProperty("proximasVisitas");
        Assert.Single(upcoming.EnumerateArray());
        Assert.Equal(Spec020Host.Address, upcoming[0].GetProperty("lugar").GetProperty("direccion").GetString());
        Assert.Single(json.GetProperty("terminadas").GetProperty("items").EnumerateArray());
        Assert.Equal(1, json.GetProperty("resumen").GetProperty("trabajosTerminados").GetInt32());
        Assert.True(response.Headers.CacheControl?.Private);
        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task Cliente_O_Tecnico_Inactivo_CA_036_02()
    {
        await using var host = await Spec020Host.StartAsync();
        var sub = Guid.NewGuid().ToString();
        await host.SeedTechnicianAsync(sub, "inactivo", false, [], host.ColoniaId);
        using var anonymous = await host.SendAsync(HttpMethod.Get, _route);
        using var client = await host.SendAsync(HttpMethod.Get, _route, host.ClientSub);
        using var inactive = await host.SendAsync(HttpMethod.Get, _route, sub);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, client.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, inactive.StatusCode);
    }

    [Fact]
    public async Task Terminadas_Paginadas_CA_036_03()
    {
        await using var host = await Spec020Host.StartAsync();
        var sub = Guid.NewGuid().ToString();
        await host.SeedTechnicianAsync(sub, "activo", false, [], host.ColoniaId);
        var date = new DateTime(2026, 11, 18, 0, 0, 0, DateTimeKind.Utc);
        var visits = Enumerable.Range(0, 25).Select(i => new BsonDocument
        {
            ["_id"] = ObjectId.GenerateNewId(),
            ["solicitudId"] = ObjectId.GenerateNewId(),
            ["tecnicoUsuarioId"] = sub,
            ["estado"] = "terminada",
            ["terminadaEn"] = date.AddDays(-(i / 10)),
        }).ToArray();
        await host.Database.GetCollection<BsonDocument>("visitas").InsertManyAsync(visits);
        using var first = await host.SendAsync(HttpMethod.Get, _route, sub);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var page = (await Spec020Host.ReadAsync(first)).GetProperty("terminadas");
        var ids = page.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("solicitudId").GetString()).ToArray();
        Assert.Equal(20, ids.Length);
        var cursor = page.GetProperty("nextCursor").GetString();
        Assert.False(string.IsNullOrEmpty(cursor));
        Assert.NotNull(cursor);
        using var second = await host.SendAsync(HttpMethod.Get, _route + "?cursorTerminadas=" + Uri.EscapeDataString(cursor), sub);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var last = (await Spec020Host.ReadAsync(second)).GetProperty("terminadas");
        var remaining = last.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("solicitudId").GetString()).ToArray();
        Assert.Equal(5, remaining.Length);
        Assert.Equal(JsonValueKind.Null, last.GetProperty("nextCursor").ValueKind);
        var expected = visits.OrderByDescending(x => x["terminadaEn"].ToUniversalTime())
            .ThenByDescending(x => x["_id"].AsObjectId).Select(x => x["solicitudId"].AsObjectId.ToString());
        Assert.Equal(expected, ids.Concat(remaining));
    }

    [Fact]
    public async Task Base_Caida_Devuelve_503_Sin_Secciones_CA_036_05()
    {
        await using var factory = MarketApiFactory.CreateOffline(_ => { });
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_route, UriKind.Relative));
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Spec020Host.Token(Guid.NewGuid().ToString()));
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.Private);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var body = await Spec020Host.ReadAsync(response);
        Assert.Equal("unavailable", body.GetProperty("code").GetString());
        Assert.False(body.TryGetProperty("proximasVisitas", out _));
        Assert.False(body.TryGetProperty("cotizacionesEnviadas", out _));
        Assert.False(body.TryGetProperty("terminadas", out _));
        Assert.False(body.TryGetProperty("resumen", out _));
    }

    [Fact]
    public async Task Otro_Tecnico_Y_Cursor_Invalido_CA_036_06()
    {
        await using var host = await Spec020Host.StartAsync();
        var owner = Guid.NewGuid().ToString();
        var other = Guid.NewGuid().ToString();
        await SeedAsync(host, owner);
        await host.SeedTechnicianAsync(other, "activo", false, [], host.ColoniaId);
        using var response = await host.SendAsync(HttpMethod.Get, _route, other);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await Spec020Host.ReadAsync(response);
        Assert.Empty(body.GetProperty("proximasVisitas").EnumerateArray());
        Assert.Empty(body.GetProperty("cotizacionesEnviadas").EnumerateArray());
        Assert.Empty(body.GetProperty("terminadas").GetProperty("items").EnumerateArray());
        Assert.Equal(0, body.GetProperty("resumen").GetProperty("trabajosTerminados").GetInt32());
        using var invalid = await host.SendAsync(HttpMethod.Get, _route + "?cursorTerminadas=invalid!", other);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var problem = await Spec020Host.ReadAsync(invalid);
        Assert.Equal("invalid_cursor", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Motivo_Rechazo_En_Cotizacion_CA_036_04()
    {
        await using var host = await Spec020Host.StartAsync();
        var sub = Guid.NewGuid().ToString();
        await SeedAsync(host, sub);
        using var response = await host.SendAsync(HttpMethod.Get, _route, sub);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await Spec020Host.ReadAsync(response);
        var quote = Assert.Single(json.GetProperty("cotizacionesEnviadas").EnumerateArray());
        Assert.Equal("other_quote_chosen", quote.GetProperty("motivoRechazo").GetString());
        Assert.Equal("2026-11-18T00:00:00Z", quote.GetProperty("enviadaEn").GetString());
    }

    [Fact]
    public async Task Sin_Datos_De_Contacto_CA_036_06()
    {
        await using var host = await Spec020Host.StartAsync();
        var sub = Guid.NewGuid().ToString();
        await SeedAsync(host, sub);
        using var response = await host.SendAsync(HttpMethod.Get, _route, sub);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(host.ClientSub, body, StringComparison.Ordinal);
        Assert.DoesNotContain("private-client-name", body, StringComparison.Ordinal);
        Assert.DoesNotContain("private-client-phone", body, StringComparison.Ordinal);
        Assert.DoesNotContain("private-closed-address", body, StringComparison.Ordinal);
        Assert.DoesNotContain("private-visit-code", body, StringComparison.Ordinal);
    }

    private static async Task SeedAsync(Spec020Host host, string sub)
    {
        await host.SeedTechnicianAsync(sub, "activo", false, ["plo"], host.ColoniaId);
        var requestId = ObjectId.GenerateNewId();
        var cipher = host.Services.GetRequiredService<FieldCipher>();
        await host.Database.GetCollection<BsonDocument>("solicitudes").InsertOneAsync(new BsonDocument
        {
            ["_id"] = requestId,
            ["clienteId"] = host.ClientSub,
            ["coloniaNombre"] = "Centro",
            ["oficioNombre"] = "Plomería",
            ["direccionCifrada"] = cipher.Encrypt(Spec020Host.Address, $"solicitudes|direccion|{requestId}"),
            ["nombre"] = "private-client-name",
            ["telefono"] = "private-client-phone",
        });
        var visits = host.Database.GetCollection<BsonDocument>("visitas");
        await visits.InsertManyAsync([
            new BsonDocument { ["solicitudId"] = requestId, ["tecnicoUsuarioId"] = sub, ["estado"] = "confirmada",
                ["dia"] = "2026-11-20", ["hora"] = "12:00", ["modalidad"] = "domicilio", ["precio"] = 100, ["tope"] = 110,
                ["codigoCifrado"] = "private-visit-code" },
            new BsonDocument { ["solicitudId"] = ObjectId.GenerateNewId(), ["tecnicoUsuarioId"] = sub, ["estado"] = "terminada",
                ["terminadaEn"] = new DateTime(2026, 11, 18, 0, 0, 0, DateTimeKind.Utc), ["precio"] = 100, ["direccion"] = "private-closed-address" },
        ]);
        await host.Database.GetCollection<BsonDocument>("cotizaciones").InsertOneAsync(new BsonDocument
        {
            ["solicitudId"] = requestId,
            ["tecnicoId"] = ObjectId.GenerateNewId(),
            ["tecnicoUsuarioId"] = sub,
            ["tipoPrecio"] = "fijo",
            ["precio"] = 100,
            ["tope"] = 110,
            ["estado"] = "rechazada",
            ["motivoRechazo"] = "other_quote_chosen",
            ["creadoEn"] = new DateTime(2026, 11, 18, 0, 0, 0, DateTimeKind.Utc),
        });
    }
}
