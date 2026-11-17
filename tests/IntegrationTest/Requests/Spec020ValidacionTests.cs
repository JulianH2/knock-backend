using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using MongoDB.Bson;
using Xunit;

namespace Knock.IntegrationTest.Requests;

/// <summary>Spec 020, T-05: lo que se rechaza al crear una solicitud, con 422 y sin guardar nada (MongoDB real).</summary>
public sealed class Spec020ValidacionTests
{
    [Fact]
    public async Task Rechaza_Fotos_Y_Descripcion_CA_020_02()
    {
        await using var host = await Spec020Host.StartAsync();
        var photo = ObjectId.GenerateNewId().ToString();
        var cases = new (string Field, Action<Dictionary<string, object?>> Break)[]
        {
            ("fotos", b => b["fotos"] = Array.Empty<string>()),
            ("fotos", b => b["fotos"] = Enumerable.Range(0, 6).Select(_ => ObjectId.GenerateNewId().ToString()).ToArray()),
            ("fotos", b => b["fotos"] = new[] { photo, photo }),
            ("descripcion", b => b["descripcion"] = "1234567"),
        };

        foreach (var (field, breakIt) in cases)
        {
            var body = host.ValidBody();
            breakIt(body);

            using var response = await host.CreateAsync(body);

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            (await Spec020Host.ReadAsync(response)).GetProperty("errors").GetProperty(field).GetArrayLength().Should().BeGreaterThan(0);
        }

        (await host.CountRequestsAsync()).Should().Be(0, "no se guarda nada");
    }

    [Fact]
    public async Task Rechaza_Catalogo_Invalido_CA_020_04()
    {
        await using var host = await Spec020Host.StartAsync();
        await host.SeedTradeAsync("ele", "Electricidad", "domicilio", active: false);
        var inactiveTrade = host.ValidBody(trade: "ele");
        var unknownColonia = host.ValidBody();
        unknownColonia["coloniaId"] = ObjectId.GenerateNewId().ToString();

        using var trade = await host.CreateAsync(inactiveTrade);
        using var colonia = await host.CreateAsync(unknownColonia);

        trade.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await Spec020Host.ReadAsync(trade)).GetProperty("code").GetString().Should().Be("trade_invalid");
        colonia.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await Spec020Host.ReadAsync(colonia)).GetProperty("code").GetString().Should().Be("colonia_not_found");
        (await host.CountRequestsAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Rechaza_Direccion_En_Taller_CA_020_05()
    {
        await using var host = await Spec020Host.StartAsync();

        using var workshop = await host.CreateAsync(host.ValidBody(trade: Spec020Host.Workshop));
        using var domicile = await host.CreateAsync(host.ValidBody(address: null));

        workshop.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await Spec020Host.ReadAsync(workshop)).GetProperty("code").GetString().Should().Be("address_not_applicable");
        domicile.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await Spec020Host.ReadAsync(domicile)).GetProperty("errors").GetProperty("direccion").GetArrayLength().Should().BeGreaterThan(0);
        (await host.CountRequestsAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Taller_Sin_Direccion_Se_Crea_R_06()
    {
        await using var host = await Spec020Host.StartAsync();

        using var response = await host.CreateAsync(host.ValidBody(trade: Spec020Host.Workshop, address: null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await Spec020Host.ReadAsync(response);
        json.GetProperty("modalidad").GetString().Should().Be("taller");
        json.GetProperty("direccion").ValueKind.Should().Be(JsonValueKind.Null);
    }
}
