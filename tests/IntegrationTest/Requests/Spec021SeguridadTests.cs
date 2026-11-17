extern alias MarketApi;

using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Requests;

/// <summary>Spec 021: autorización por recurso, contacto que solo se revela al elegir, código cifrado, concurrencia y registros sin datos personales.</summary>
public sealed class Spec021SeguridadTests
{
    [Fact]
    public async Task Solicitud_Ajena_CA_021_03()
    {
        await using var host = await Spec020Host.StartAsync();
        var (requestId, quotes, _) = await host.QuotedRequestAsync(2);
        var stranger = Guid.NewGuid().ToString("D");

        using var list = await host.CompareAsync(requestId, sub: stranger);
        using var choose = await host.ChooseAsync(requestId, quotes[0], stranger);
        using var unknown = await host.CompareAsync(ObjectId.GenerateNewId());

        list.StatusCode.Should().Be(HttpStatusCode.NotFound);
        choose.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var foreign = await Spec020Host.ReadAsync(list);
        var missing = await Spec020Host.ReadAsync(unknown);
        foreign.GetProperty("title").GetString().Should().Be(missing.GetProperty("title").GetString(), "el 404 es uniforme");
        (await host.OneAsync("solicitudes", requestId))["estado"].AsString.Should().Be("cotizando");
        (await host.CountAsync("visitas")).Should().Be(0);
    }

    [Fact]
    public async Task Exige_Sesion_Cliente_CA_021_12()
    {
        await using var host = await Spec020Host.StartAsync();
        var (requestId, quotes, technicians) = await host.QuotedRequestAsync(1);

        using var noTokenList = await host.SendAsync(HttpMethod.Get, $"/api/v1/solicitudes/{requestId}/cotizaciones");
        using var noTokenChoose = await host.SendAsync(HttpMethod.Post, $"/api/v1/solicitudes/{requestId}/elegir", body: new { cotizacionId = quotes[0].ToString() });
        using var technicianList = await host.CompareAsync(requestId, sub: technicians[0].Sub);
        using var technicianChoose = await host.ChooseAsync(requestId, quotes[0], technicians[0].Sub);

        noTokenList.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        noTokenChoose.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        technicianList.StatusCode.Should().Be(HttpStatusCode.NotFound);
        technicianChoose.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await host.CountAsync("visitas")).Should().Be(0);
    }

    [Fact]
    public async Task Dos_Elecciones_CA_021_05()
    {
        await using var host = await Spec020Host.StartAsync();
        var (requestId, quotes, _) = await host.QuotedRequestAsync(2);

        var responses = await Task.WhenAll(host.ChooseAsync(requestId, quotes[0]), host.ChooseAsync(requestId, quotes[1]));

        try
        {
            responses.Select(r => r.StatusCode).Should().BeEquivalentTo([HttpStatusCode.OK, HttpStatusCode.Conflict]);
            var loser = responses.Single(r => r.StatusCode == HttpStatusCode.Conflict);
            (await Spec020Host.ReadAsync(loser)).GetProperty("code").GetString().Should().Be("request_already_chosen");
            (await host.CountAsync("visitas")).Should().Be(1);
            (await host.CountAsync("outbox", Builders<BsonDocument>.Filter.Eq("tipo", "VisitaConfirmada"))).Should().Be(1);
            (await host.CountAsync("outbox", Builders<BsonDocument>.Filter.Eq("tipo", "CotizacionAceptada"))).Should().Be(1);
            (await host.CountAsync("outbox", Builders<BsonDocument>.Filter.Eq("tipo", "CotizacionRechazada"))).Should().Be(1);
            (await host.CountAsync("cotizaciones", Builders<BsonDocument>.Filter.Eq("estado", "aceptada"))).Should().Be(1);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    [Fact]
    public async Task Revela_Solo_Despues_CA_021_09()
    {
        await using var host = await Spec020Host.StartAsync();
        var (requestId, quotes, _) = await host.QuotedRequestAsync(2);
        using var before = await host.CompareAsync(requestId);
        var listed = await before.Content.ReadAsStringAsync();

        using var response = await host.ChooseAsync(requestId, quotes[0]);

        listed.Should().NotContain(Spec021Host.TechnicianPhone).And.NotContain(Spec021Host.TechnicianWhatsApp).And.NotContain(Spec020Host.Address);
        var json = await Spec020Host.ReadAsync(response);
        json.GetProperty("tecnico").GetProperty("telefono").GetString().Should().Be(Spec021Host.TechnicianPhone);
        json.GetProperty("tecnico").GetProperty("whatsapp").GetString().Should().Be(Spec021Host.TechnicianWhatsApp);
        json.GetProperty("lugar").GetProperty("tipo").GetString().Should().Be("domicilio");
        json.GetProperty("lugar").GetProperty("direccion").GetString().Should().Be(Spec020Host.Address, "a domicilio es la dirección que escribió el cliente");
    }

    [Fact]
    public async Task Cita_De_Taller_Revela_La_Direccion_Del_Taller_Y_Sin_Contacto_Ausente_R_15()
    {
        await using var host = await Spec020Host.StartAsync();
        var (requestId, _, _) = await host.QuotedRequestAsync(0, workshop: true);
        var withoutContact = await host.SeedPublicTechnicianAsync(withContact: false, withWorkshop: true);
        var quote = await host.SeedQuoteAsync(requestId, withoutContact);

        using var response = await host.ChooseAsync(requestId, quote);

        var json = await Spec020Host.ReadAsync(response);
        json.GetProperty("lugar").GetProperty("tipo").GetString().Should().Be("taller");
        json.GetProperty("lugar").GetProperty("direccion").GetString().Should().Be(Spec021Host.WorkshopAddress);
        json.GetProperty("tecnico").TryGetProperty("telefono", out _).Should().BeFalse("si el técnico no publicó su contacto, el campo va ausente y la app ofrece el chat");
        var visit = await host.Database.GetCollection<BsonDocument>("visitas").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        visit.Contains("codigoCifrado").Should().BeFalse("una cita de taller no lleva código");
    }

    [Fact]
    public async Task Listado_Sin_Contacto_CA_021_18()
    {
        await using var host = await Spec020Host.StartAsync();
        var (requestId, quotes, _) = await host.QuotedRequestAsync(2, workshop: false);

        using var before = await host.CompareAsync(requestId);
        (await host.ChooseAsync(requestId, quotes[0])).Dispose();
        using var after = await host.CompareAsync(requestId);

        foreach (var response in new[] { before, after })
        {
            var raw = await response.Content.ReadAsStringAsync();
            raw.Should().NotContain("telefono").And.NotContain("whatsapp").And.NotContain("direccion")
                .And.NotContain(Spec021Host.TechnicianPhone).And.NotContain(Spec020Host.Address).And.NotContain(Spec021Host.WorkshopAddress);
        }
    }

    [Fact]
    public async Task Genera_Codigo_Cifrado_CA_021_10()
    {
        await using var host = await Spec020Host.StartAsync();
        var (requestId, quotes, _) = await host.QuotedRequestAsync(1);
        var day = (await host.OneAsync("cotizaciones", quotes[0]))["dia"].AsString;

        (await host.ChooseAsync(requestId, quotes[0])).Dispose();

        var visit = await host.Database.GetCollection<BsonDocument>("visitas").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        var stored = visit["codigoCifrado"].AsString;
        stored.Should().StartWith("1.", "lleva la versión de la llave");
        stored.Length.Should().BeGreaterThan(20);
        System.Text.RegularExpressions.Regex.IsMatch(stored, @"^\d{4}$").Should().BeFalse("nunca en claro");
        visit["codigoVenceEn"].ToUniversalTime().Should().Be(MarketApi::Knock.Market.Api.Modules.Requests.Services.VisitCodeFactory.ExpiresAt(day));
    }

    [Fact]
    public async Task Sin_Datos_Personales_CA_021_13()
    {
        await using var host = await Spec020Host.StartAsync();
        var (requestId, quotes, _) = await host.QuotedRequestAsync(2);
        string? stored = null;

        var output = await ConsoleCapture.RunAsync(async () =>
        {
            using var list = await host.CompareAsync(requestId);
            using var chosen = await host.ChooseAsync(requestId, quotes[0]);
            using var repeated = await host.ChooseAsync(requestId, quotes[1]);
            stored = (await host.Database.GetCollection<BsonDocument>("visitas").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync())["codigoCifrado"].AsString;
        });

        stored.Should().NotBeNull();
        output.Should().NotContain(Spec021Host.TechnicianName, "el nombre del técnico no se registra")
            .And.NotContain(Spec021Host.TechnicianPhone)
            .And.NotContain(Spec021Host.TechnicianWhatsApp)
            .And.NotContain("Calle Ficticia", "la dirección no se registra")
            .And.NotContain(stored, "el código cifrado no se registra");
    }
}
