using System.Net;
using AwesomeAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Requests;

/// <summary>Spec 022, T-06: el aviso a una persona de confianza (MongoDB real).</summary>
public sealed class Spec022AvisoTests
{
    [Fact]
    public async Task Texto_Sin_Codigo_Ni_Direccion_CA_022_11()
    {
        await using var host = await Spec020Host.StartAsync();
        var visit = await host.ConfirmedVisitAsync(level: "completa");
        var document = await host.VisitDocAsync(visit.VisitId);

        using var response = await host.NoticeAsync(visit.VisitId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        var json = await Spec020Host.ReadAsync(response);
        var text = json.GetProperty("texto").GetString()!;
        json.GetProperty("incluyeCodigo").GetBoolean().Should().BeFalse();
        json.GetProperty("incluyeDireccion").GetBoolean().Should().BeFalse();
        text.Should().Contain(Spec021Host.TechnicianName).And.Contain("con verificación completa")
            .And.Contain(document["dia"].AsString).And.Contain(document["hora"].AsString);
        text.Should().NotContain(visit.Code!).And.NotContain("Calle Ficticia").And.NotContain("Colonia Piloto");
        text.Should().Be($"Contraté a {Spec021Host.TechnicianName} (con verificación completa) para una visita el {document["dia"].AsString} a las {document["hora"].AsString}. Le pediré que me diga el código en la puerta; si no lo sabe, no le abriré. Te aviso cuando llegue.");
    }

    [Fact]
    public async Task Tecnico_Sin_Verificar_Lo_Dice_R_12()
    {
        await using var host = await Spec020Host.StartAsync();
        var visit = await host.ConfirmedVisitAsync(level: "ninguno");

        using var response = await host.NoticeAsync(visit.VisitId);

        (await Spec020Host.ReadAsync(response)).GetProperty("texto").GetString().Should().Contain("sin verificar");
    }

    [Fact]
    public async Task Cuenta_Cada_Aviso_Y_Deja_Evento_Sin_Texto_R_13()
    {
        await using var host = await Spec020Host.StartAsync();
        var visit = await host.ConfirmedVisitAsync();

        for (var i = 0; i < 3; i++)
        {
            using var response = await host.NoticeAsync(visit.VisitId);
            response.StatusCode.Should().Be(HttpStatusCode.OK, "el aviso no tiene límite propio");
        }

        (await host.VisitDocAsync(visit.VisitId))["avisosEnviados"].AsInt32.Should().Be(3);
        var events = await host.Database.GetCollection<BsonDocument>("outbox").Find(Builders<BsonDocument>.Filter.Eq("tipo", "AvisoConfianzaPreparado")).ToListAsync();
        events.Should().HaveCount(3);
        events.Should().OnlyContain(e => !e.ToJson().Contains(Spec021Host.TechnicianName) && !e.ToJson().Contains("Contraté") && !e.ToJson().Contains(visit.Code!));
    }
}
