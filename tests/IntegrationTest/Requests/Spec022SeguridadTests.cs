extern alias MarketApi;

using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using MarketApi::Knock.Market.Api.Hosting;
using MarketApi::Knock.Market.Api.Modules.Requests.Data;
using MarketApi::Knock.Market.Api.Modules.Requests.Domain;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Requests;

/// <summary>Spec 022: autorización por recurso, sesión obligatoria, aviso sin datos sensibles, cierres con evento y registros sin código ni dirección.</summary>
public sealed class Spec022SeguridadTests
{
    [Fact]
    public async Task Visita_Ajena_CA_022_10()
    {
        await using var host = await Spec020Host.StartAsync();
        var visit = await host.ConfirmedVisitAsync();
        await host.OpenWindowAsync(visit.VisitId);
        var stranger = Guid.NewGuid().ToString("D");
        var otherTechnician = await host.SeedPublicTechnicianAsync();

        using var strangerView = await host.VisitAsync(visit.VisitId, stranger);
        using var otherTechnicianView = await host.VisitAsync(visit.VisitId, otherTechnician.Sub);
        using var otherTechnicianArrival = await host.ArriveAsync(visit.VisitId, otherTechnician.Sub, visit.Code);
        using var clientArrival = await host.ArriveAsync(visit.VisitId, host.ClientSub, visit.Code);
        using var unknown = await host.VisitAsync(ObjectId.GenerateNewId());
        using var strangerNotice = await host.NoticeAsync(visit.VisitId, stranger);
        using var techniciansNotice = await host.NoticeAsync(visit.VisitId, visit.Technician.Sub);

        strangerView.StatusCode.Should().Be(HttpStatusCode.NotFound);
        otherTechnicianView.StatusCode.Should().Be(HttpStatusCode.NotFound);
        otherTechnicianArrival.StatusCode.Should().Be(HttpStatusCode.NotFound, "ni con el código correcto");
        clientArrival.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Spec020Host.ReadAsync(clientArrival)).GetProperty("code").GetString().Should().Be("not_a_technician");
        strangerNotice.StatusCode.Should().Be(HttpStatusCode.NotFound);
        techniciansNotice.StatusCode.Should().Be(HttpStatusCode.NotFound, "el aviso es solo del cliente dueño");
        (await Spec020Host.ReadAsync(strangerView)).GetProperty("title").GetString().Should().Be((await Spec020Host.ReadAsync(unknown)).GetProperty("title").GetString(), "el 404 es uniforme");
        (await host.VisitDocAsync(visit.VisitId))["codigoUsado"].AsBoolean.Should().BeFalse();
    }

    [Fact]
    public async Task Exige_Sesion_CA_022_13()
    {
        await using var host = await Spec020Host.StartAsync();
        var visit = await host.ConfirmedVisitAsync();
        var id = visit.VisitId;

        using var view = await host.SendAsync(HttpMethod.Get, $"/api/v1/visitas/{id}");
        using var arrival = await host.SendAsync(HttpMethod.Post, $"/api/v1/visitas/{id}/llegada", body: new { codigo = visit.Code });
        using var notice = await host.SendAsync(HttpMethod.Post, $"/api/v1/visitas/{id}/aviso", rawJson: "{}");

        view.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        arrival.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        notice.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Id_Mal_Formado_Responde_400_R_01()
    {
        await using var host = await Spec020Host.StartAsync();
        var technician = await host.SeedPublicTechnicianAsync();

        using var view = await host.SendAsync(HttpMethod.Get, "/api/v1/visitas/no-es-id", host.ClientSub);
        using var arrival = await host.SendAsync(HttpMethod.Post, "/api/v1/visitas/no-es-id/llegada", technician.Sub, new { codigo = "1234" });
        using var notice = await host.SendAsync(HttpMethod.Post, "/api/v1/visitas/no-es-id/aviso", host.ClientSub, rawJson: "{}");

        view.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        arrival.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        notice.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Sexta_Llegada_En_Un_Minuto_Recibe_429_R_Limite()
    {
        await using var host = await Spec020Host.StartAsync();
        var visit = await host.ConfirmedVisitAsync();
        await host.OpenWindowAsync(visit.VisitId);
        var statuses = new List<HttpStatusCode>();

        for (var i = 0; i < 6; i++)
        {
            using var response = await host.ArriveAsync(visit.VisitId, visit.Technician.Sub, "12");
            statuses.Add(response.StatusCode);
        }

        statuses.Take(5).Should().OnlyContain(s => s == HttpStatusCode.UnprocessableEntity);
        statuses[5].Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Sin_Datos_Sensibles_CA_022_14()
    {
        await using var host = await Spec020Host.StartAsync();
        var visit = await host.ConfirmedVisitAsync();
        await host.OpenWindowAsync(visit.VisitId);

        var output = await ConsoleCapture.RunAsync(async () =>
        {
            using var wrong = await host.ArriveAsync(visit.VisitId, visit.Technician.Sub, Spec022Host.WrongCode(visit.Code!));
            using var right = await host.ArriveAsync(visit.VisitId, visit.Technician.Sub, visit.Code);
            using var again = await host.ArriveAsync(visit.VisitId, visit.Technician.Sub, visit.Code);
            using var view = await host.VisitAsync(visit.VisitId);
            using var notice = await host.NoticeAsync(visit.VisitId);
        });

        output.Should().NotContain("Calle Ficticia", "la dirección no se registra")
            .And.NotContain($"\"{visit.Code}\"", "el código no se registra")
            .And.NotContain("codigo=" + visit.Code)
            .And.NotContain(host.ClientSub, "el cliente no se registra")
            .And.NotContain(Spec021Host.TechnicianPhone);
    }

    [Fact]
    public async Task Spec036_Cierre_Guarda_Fecha_Terminada_CA_036_03()
    {
        await using var host = await Spec020Host.StartAsync();
        var visit = await host.ConfirmedVisitAsync();
        await host.SetVisitAsync(visit.VisitId, Builders<BsonDocument>.Update.Set("estado", "en_curso"));
        var store = new VisitStore(host.Database, Options.Create(new KnockMongoOptions()), new MongoOutbox(host.Database));
        var record = (await store.FindAsync(visit.VisitId, CancellationToken.None))!;
        var now = new DateTime(2026, 11, 19, 12, 34, 56, DateTimeKind.Utc);

        (await store.CloseAsync(record, VisitCloseReason.Finished, now, CancellationToken.None)).Should().BeTrue();
        var stored = await host.VisitDocAsync(visit.VisitId);
        stored["estado"].AsString.Should().Be("terminada");
        stored["terminadaEn"].ToUniversalTime().Should().Be(now);
        stored["actualizadoEn"].ToUniversalTime().Should().Be(now);
    }

    [Fact]
    public async Task Cierre_Emite_VisitaCerrada_CA_022_19()
    {
        await using var host = await Spec020Host.StartAsync();
        var store = new VisitStore(host.Database, Options.Create(new KnockMongoOptions()), new MongoOutbox(host.Database));
        var expected = new Dictionary<VisitCloseReason, (string Motivo, string From, string To)>
        {
            [VisitCloseReason.Cancelled] = ("cancelada", "confirmada", "cancelada"),
            [VisitCloseReason.NoShow] = ("no_presentado", "confirmada", "no_presentado"),
            [VisitCloseReason.Finished] = ("terminada", "en_curso", "terminada"),
            [VisitCloseReason.Blocked] = ("bloqueada", "confirmada", "confirmada"),
        };

        foreach (var (reason, (motivo, from, to)) in expected)
        {
            var visit = await host.ConfirmedVisitAsync();
            await host.SetVisitAsync(visit.VisitId, Builders<BsonDocument>.Update.Set("estado", from));
            var record = (await store.FindAsync(visit.VisitId, CancellationToken.None))!;

            (await store.CloseAsync(record, reason, DateTime.UtcNow, CancellationToken.None)).Should().BeTrue();
            (await store.CloseAsync(record, reason, DateTime.UtcNow, CancellationToken.None)).Should().BeFalse("cerrar dos veces no vuelve a emitir");

            var events = await host.Database.GetCollection<BsonDocument>("outbox")
                .Find(Builders<BsonDocument>.Filter.Eq("tipo", "VisitaCerrada") & Builders<BsonDocument>.Filter.Eq("agregadoId", visit.VisitId.ToString())).ToListAsync();
            events.Should().ContainSingle().Which["datos"]["motivo"].AsString.Should().Be(motivo);
            events[0]["datos"].AsBsonDocument.Names.Should().BeEquivalentTo("visitaId", "motivo");
            (await host.VisitDocAsync(visit.VisitId))["estado"].AsString.Should().Be(to);
        }
    }

    [Fact]
    public async Task Cierre_Con_Falla_No_Deja_Evento_Ni_Cambio_CA_022_19()
    {
        await using var host = await Spec020Host.StartAsync();
        var visit = await host.ConfirmedVisitAsync();
        host.OutboxFailure.Fail = true;
        var store = new VisitStore(host.Database, Options.Create(new KnockMongoOptions()), new FlakyOutbox(new MongoOutbox(host.Database), host.OutboxFailure));
        var record = (await store.FindAsync(visit.VisitId, CancellationToken.None))!;

        var close = () => store.CloseAsync(record, VisitCloseReason.Cancelled, DateTime.UtcNow, CancellationToken.None);

        await close.Should().ThrowAsync<TimeoutException>();
        (await host.VisitDocAsync(visit.VisitId))["estado"].AsString.Should().Be("confirmada", "si el evento falla, el cambio tampoco queda");
        (await host.EventsAsync("VisitaCerrada")).Should().Be(0);
    }
}
