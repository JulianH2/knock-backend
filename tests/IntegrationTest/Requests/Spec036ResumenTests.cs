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
