using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using MongoDB.Bson;
using Xunit;

namespace Knock.IntegrationTest.Requests;

/// <summary>Spec 022, T-01 a T-03: ver la visita como cliente o como técnico elegido (MongoDB real).</summary>
public sealed class Spec022VistaTests
{
    [Fact]
    public async Task Cliente_Ve_Codigo_CA_022_01()
    {
        await using var host = await Spec020Host.StartAsync();
        var visit = await host.ConfirmedVisitAsync();

        using var response = await host.VisitAsync(visit.VisitId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        response.Headers.Contains("ETag").Should().BeFalse();
        var json = await Spec020Host.ReadAsync(response);
        json.GetProperty("rol").GetString().Should().Be("cliente");
        json.GetProperty("estado").GetString().Should().Be("confirmada");
        json.GetProperty("codigo").GetString().Should().MatchRegex("^[0-9]{4}$");
        json.GetProperty("codigoVenceEn").GetDateTimeOffset().Should().BeAfter(DateTimeOffset.UtcNow);
        json.GetProperty("reglaSeguridad").GetString().Should().Be($"Pídele a {Spec021Host.TechnicianName} que te diga este código en la puerta. Si no lo sabe, no abras.");
        json.GetProperty("tecnico").GetProperty("nombre").GetString().Should().Be(Spec021Host.TechnicianName);
        json.GetProperty("puedeAvisar").GetBoolean().Should().BeTrue();
        json.GetProperty("puedeLlegar").GetBoolean().Should().BeFalse();
        json.TryGetProperty("motivoCodigo", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Tecnico_No_Ve_Codigo_CA_022_02()
    {
        await using var host = await Spec020Host.StartAsync();
        var visit = await host.ConfirmedVisitAsync();
        await host.OpenWindowAsync(visit.VisitId);

        using var response = await host.VisitAsync(visit.VisitId, visit.Technician.Sub);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var raw = await response.Content.ReadAsStringAsync();
        var json = JsonElement.Parse(raw);
        json.GetProperty("rol").GetString().Should().Be("tecnico");
        json.GetProperty("puedeLlegar").GetBoolean().Should().BeTrue("la ventana de la cita está abierta");
        raw.Should().NotContain(visit.Code!, "el técnico nunca ve el código")
            .And.NotContain(host.ClientSub, "ni quién es el cliente")
            .And.NotContain("codigo", "la vista del técnico no tiene ningún campo de código")
            .And.NotContain("telefono").And.NotContain("whatsapp");
    }

    [Fact]
    public async Task Cita_En_Taller_CA_022_09()
    {
        await using var host = await Spec020Host.StartAsync();
        var visit = await host.ConfirmedVisitAsync(workshop: true);
        await host.OpenWindowAsync(visit.VisitId);

        using var client = await host.VisitAsync(visit.VisitId);
        using var arrival = await host.ArriveAsync(visit.VisitId, visit.Technician.Sub, "1234");

        var json = await Spec020Host.ReadAsync(client);
        json.TryGetProperty("codigo", out _).Should().BeFalse();
        json.GetProperty("motivoCodigo").GetString().Should().Be("no_aplica");
        json.GetProperty("lugar").GetProperty("tipo").GetString().Should().Be("taller");
        json.GetProperty("lugar").GetProperty("direccion").GetString().Should().Be(Spec021Host.WorkshopAddress);
        arrival.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Spec020Host.ReadAsync(arrival)).GetProperty("code").GetString().Should().Be("visit_without_code");
    }

    [Fact]
    public async Task Contacto_Y_Lugar_Por_Rol_CA_022_17()
    {
        await using var host = await Spec020Host.StartAsync();
        var visit = await host.ConfirmedVisitAsync();

        using var client = await host.VisitAsync(visit.VisitId);
        using var technician = await host.VisitAsync(visit.VisitId, visit.Technician.Sub);

        var asClient = await Spec020Host.ReadAsync(client);
        asClient.GetProperty("tecnico").GetProperty("telefono").GetString().Should().Be(Spec021Host.TechnicianPhone);
        asClient.GetProperty("tecnico").GetProperty("whatsapp").GetString().Should().Be(Spec021Host.TechnicianWhatsApp);
        asClient.GetProperty("lugar").GetProperty("direccion").GetString().Should().Be(Spec020Host.Address);
        var asTechnician = await Spec020Host.ReadAsync(technician);
        asTechnician.GetProperty("lugar").GetProperty("direccion").GetString().Should().Be(Spec020Host.Address, "el técnico elegido ve la dirección");
        asTechnician.GetProperty("cliente").GetProperty("colonia").GetString().Should().Be("Centro");
        asTechnician.GetProperty("cliente").EnumerateObject().Select(p => p.Name).Should().Equal("colonia");
    }

    [Theory]
    [InlineData("terminada", false)]
    [InlineData("cancelada", false)]
    [InlineData("no_presentado", false)]
    [InlineData("confirmada", true)]
    [InlineData("confirmada", false)]
    [InlineData("en_curso", false)]
    public async Task Tecnico_Solo_Ve_La_Direccion_Con_Visita_Vigente_Y_Sin_Bloqueo_CA_022_17(string estado, bool locked)
    {
        await using var host = await Spec020Host.StartAsync();
        var visit = await host.ConfirmedVisitAsync();
        await host.SetVisitAsync(visit.VisitId, MongoDB.Driver.Builders<BsonDocument>.Update.Combine(MongoDB.Driver.Builders<BsonDocument>.Update.Set("estado", estado), MongoDB.Driver.Builders<BsonDocument>.Update.Set("bloqueada", locked)));

        using var response = await host.VisitAsync(visit.VisitId, visit.Technician.Sub);

        var raw = await response.Content.ReadAsStringAsync();
        var json = JsonElement.Parse(raw);
        json.GetProperty("cliente").GetProperty("colonia").GetString().Should().Be("Centro", "la colonia sí se ve");
        var mayLook = (estado is "confirmada" or "en_curso") && !locked;
        json.GetProperty("lugar").TryGetProperty("direccion", out _).Should().Be(mayLook);
        if (!mayLook)
        {
            raw.Should().NotContain("Calle Ficticia");
        }
    }

    [Fact]
    public async Task Codigo_Cifrado_CA_022_03()
    {
        await using var host = await Spec020Host.StartAsync();
        var visit = await host.ConfirmedVisitAsync();

        var document = await host.VisitDocAsync(visit.VisitId);

        var stored = document["codigoCifrado"].AsString;
        stored.Should().StartWith("1.").And.NotBe(visit.Code);
        stored.Should().NotMatchRegex("^[0-9]{4}$", "nunca en claro");
        document.ToJson().Should().NotContain("\"" + visit.Code + "\"", "el código no aparece en claro en ninguna parte del documento");
        document["codigoUsado"].AsBoolean.Should().BeFalse();
        document["intentos"].AsInt32.Should().Be(0);
    }

    [Fact]
    public async Task Codigo_Se_Oculta_Con_Su_Motivo_R_03()
    {
        await using var host = await Spec020Host.StartAsync();
        var used = await host.ConfirmedVisitAsync();
        var expired = await host.ConfirmedVisitAsync();
        var locked = await host.ConfirmedVisitAsync();
        var cancelled = await host.ConfirmedVisitAsync();
        await host.SetVisitAsync(used.VisitId, MongoDB.Driver.Builders<BsonDocument>.Update.Set("codigoUsado", true));
        await host.SetVisitAsync(expired.VisitId, MongoDB.Driver.Builders<BsonDocument>.Update.Set("codigoVenceEn", DateTime.UtcNow.AddMinutes(-1)));
        await host.SetVisitAsync(locked.VisitId, MongoDB.Driver.Builders<BsonDocument>.Update.Set("bloqueada", true));
        await host.SetVisitAsync(cancelled.VisitId, MongoDB.Driver.Builders<BsonDocument>.Update.Set("estado", "cancelada"));

        var reasons = new List<string?>();
        foreach (var visit in new[] { used, expired, locked, cancelled })
        {
            using var response = await host.VisitAsync(visit.VisitId);
            var json = await Spec020Host.ReadAsync(response);
            json.TryGetProperty("codigo", out _).Should().BeFalse();
            reasons.Add(json.GetProperty("motivoCodigo").GetString());
        }

        reasons.Should().Equal("usado", "vencido", "bloqueada", "no_aplica");
    }
}
