using System.Net;
using AwesomeAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Requests;

/// <summary>Spec 021, T-04 y T-06: elegir una cotización, de forma atómica (MongoDB real).</summary>
public sealed class Spec021EleccionTests
{
    [Fact]
    public async Task Elige_Cotizacion_CA_021_04()
    {
        await using var host = await Spec020Host.StartAsync();
        var (requestId, quotes, _) = await host.QuotedRequestAsync(3);

        using var response = await host.ChooseAsync(requestId, quotes[1]);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        var json = await Spec020Host.ReadAsync(response);
        json.GetProperty("estado").GetString().Should().Be("confirmada");
        json.GetProperty("cotizacionId").GetString().Should().Be(quotes[1].ToString());
        json.GetProperty("precio").GetInt32().Should().Be(1100, "el precio sale de la cotización guardada");
        json.GetProperty("modalidad").GetString().Should().Be("domicilio");
        json.ToString().Should().NotContain("codigo", "el código de visita no viaja en esta respuesta");

        var request = await host.OneAsync("solicitudes", requestId);
        request["estado"].AsString.Should().Be("cita_confirmada");
        request["cotizacionElegidaId"].AsObjectId.Should().Be(quotes[1]);
        (await host.OneAsync("cotizaciones", quotes[1]))["estado"].AsString.Should().Be("aceptada");
        foreach (var other in new[] { quotes[0], quotes[2] })
        {
            var stored = await host.OneAsync("cotizaciones", other);
            stored["estado"].AsString.Should().Be("rechazada");
            stored["motivoRechazo"].AsString.Should().Be("other_quote_chosen");
        }

        (await host.CountAsync("visitas")).Should().Be(1);
        var visit = await host.Database.GetCollection<BsonDocument>("visitas").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        visit["estado"].AsString.Should().Be("confirmada");
        visit["solicitudId"].AsObjectId.Should().Be(requestId);
        visit["codigoUsado"].AsBoolean.Should().BeFalse();
        visit["intentos"].AsInt32.Should().Be(0);
        var events = await host.Database.GetCollection<BsonDocument>("outbox").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        events.Count(e => e["tipo"].AsString == "CotizacionAceptada").Should().Be(1);
        events.Count(e => e["tipo"].AsString == "CotizacionRechazada").Should().Be(2);
        events.Count(e => e["tipo"].AsString == "VisitaConfirmada").Should().Be(1);
    }

    [Fact]
    public async Task Eleccion_Repetida_Responde_409_CA_021_06()
    {
        await using var host = await Spec020Host.StartAsync();
        var (requestId, quotes, _) = await host.QuotedRequestAsync(2);
        (await host.ChooseAsync(requestId, quotes[0])).Dispose();

        using var sameQuote = await host.ChooseAsync(requestId, quotes[0]);
        using var otherQuote = await host.ChooseAsync(requestId, quotes[1]);

        foreach (var response in new[] { sameQuote, otherQuote })
        {
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await Spec020Host.ReadAsync(response)).GetProperty("code").GetString().Should().Be("request_already_chosen");
        }

        (await host.CountAsync("visitas")).Should().Be(1);
        (await host.CountAsync("outbox", Builders<BsonDocument>.Filter.Eq("tipo", "VisitaConfirmada"))).Should().Be(1);
    }

    [Fact]
    public async Task Cotizacion_No_Disponible_CA_021_07()
    {
        await using var host = await Spec020Host.StartAsync();
        var (requestId, quotes, technicians) = await host.QuotedRequestAsync(1);
        var (otherRequest, otherQuotes, _) = await host.QuotedRequestAsync(1);
        var withdrawn = await host.SeedQuoteAsync(requestId, await host.SeedPublicTechnicianAsync(), state: "retirada");

        using var foreign = await host.ChooseAsync(requestId, otherQuotes[0]);
        using var missing = await host.ChooseAsync(requestId, ObjectId.GenerateNewId());
        using var retired = await host.ChooseAsync(requestId, withdrawn);

        foreign.StatusCode.Should().Be(HttpStatusCode.NotFound);
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
        retired.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Spec020Host.ReadAsync(retired)).GetProperty("code").GetString().Should().Be("quote_unavailable");
        (await host.OneAsync("solicitudes", requestId))["estado"].AsString.Should().Be("cotizando");
        (await host.OneAsync("solicitudes", otherRequest))["estado"].AsString.Should().Be("cotizando");
        (await host.OneAsync("cotizaciones", quotes[0]))["estado"].AsString.Should().Be("enviada");
        (await host.CountAsync("visitas")).Should().Be(0);
        technicians.Should().HaveCount(1);
    }

    [Theory]
    [InlineData("vencida", "request_expired")]
    [InlineData("cancelada", "request_cancelled")]
    [InlineData("enviada", "request_without_quotes")]
    [InlineData("cotizando-vencida", "request_expired")]
    public async Task Solicitud_No_Elegible_CA_021_08(string state, string code)
    {
        await using var host = await Spec020Host.StartAsync();
        var (requestId, quotes, _) = await host.QuotedRequestAsync(1);
        if (state == "cotizando-vencida")
        {
            await host.SetRequestAsync(requestId, "cotizando", DateTime.UtcNow.AddSeconds(-1));
        }
        else
        {
            await host.SetRequestAsync(requestId, state);
        }

        using var response = await host.ChooseAsync(requestId, quotes[0]);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Spec020Host.ReadAsync(response)).GetProperty("code").GetString().Should().Be(code);
        (await host.OneAsync("cotizaciones", quotes[0]))["estado"].AsString.Should().Be("enviada");
        (await host.CountAsync("visitas")).Should().Be(0);
        (await host.CountAsync("outbox", Builders<BsonDocument>.Filter.Regex("tipo", "^(Cotizacion(Aceptada|Rechazada)|VisitaConfirmada)$"))).Should().Be(0);
    }

    [Fact]
    public async Task Falla_De_Transaccion_CA_021_17()
    {
        await using var host = await Spec020Host.StartAsync();
        var (requestId, quotes, _) = await host.QuotedRequestAsync(2);
        host.OutboxFailure.Fail = true;

        using var failed = await host.ChooseAsync(requestId, quotes[0]);

        failed.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        (await Spec020Host.ReadAsync(failed)).GetProperty("code").GetString().Should().Be("service_unavailable");
        (await host.OneAsync("solicitudes", requestId))["estado"].AsString.Should().Be("cotizando");
        foreach (var quote in quotes)
        {
            (await host.OneAsync("cotizaciones", quote))["estado"].AsString.Should().Be("enviada");
        }

        (await host.CountAsync("visitas")).Should().Be(0);
        (await host.CountAsync("outbox", Builders<BsonDocument>.Filter.Regex("tipo", "^(Cotizacion(Aceptada|Rechazada)|VisitaConfirmada)$"))).Should().Be(0);

        host.OutboxFailure.Fail = false;
        using var retried = await host.ChooseAsync(requestId, quotes[0]);
        retried.StatusCode.Should().Be(HttpStatusCode.OK);
        (await host.CountAsync("visitas")).Should().Be(1);
    }

    [Fact]
    public async Task Fallo_Al_Armar_La_Respuesta_Tras_El_Commit_No_Devuelve_Error_R_20()
    {
        await using var host = await Spec020Host.StartAsync();
        var (requestId, quotes, _) = await host.QuotedRequestAsync(2);
        await host.Database.GetCollection<BsonDocument>("solicitudes").UpdateOneAsync(
            Builders<BsonDocument>.Filter.Eq("_id", requestId),
            Builders<BsonDocument>.Update.Set("direccionCifrada", "1.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"));

        using var response = await host.ChooseAsync(requestId, quotes[0]);
        using var repeated = await host.ChooseAsync(requestId, quotes[1]);

        response.StatusCode.Should().Be(HttpStatusCode.OK, "la elección ya está confirmada: un fallo al descifrar la dirección no puede convertirse en 500");
        var json = await Spec020Host.ReadAsync(response);
        json.GetProperty("lugar").TryGetProperty("direccion", out var address).Should().BeFalse("la dirección ilegible sale ausente");
        address.ValueKind.Should().Be(System.Text.Json.JsonValueKind.Undefined);
        (await host.OneAsync("solicitudes", requestId))["estado"].AsString.Should().Be("cita_confirmada");
        (await host.CountAsync("visitas")).Should().Be(1);
        repeated.StatusCode.Should().Be(HttpStatusCode.Conflict, "repetir después de una respuesta degradada sigue dando 409");
    }

    [Fact]
    public async Task Elige_Una_Cotizacion_De_Rango_Sin_Campo_Precio_R_14()
    {
        await using var host = await Spec020Host.StartAsync();
        var requestId = await host.CreateRequestAsync();
        await host.SetRequestAsync(requestId, "cotizando");
        var technician = await host.SeedPublicTechnicianAsync();
        var quote = await host.SeedQuoteAsync(requestId, technician, "rango", price: null, min: 600, max: 800);
        await host.Database.GetCollection<BsonDocument>("cotizaciones").UpdateOneAsync(
            Builders<BsonDocument>.Filter.Eq("_id", quote),
            Builders<BsonDocument>.Update.Unset("precio").Unset("precioMin").Unset("precioMax").Unset("nota"));

        using var list = await host.CompareAsync(requestId);
        using var response = await host.ChooseAsync(requestId, quote);

        list.StatusCode.Should().Be(HttpStatusCode.OK, "una cotización sin los campos opcionales (como las de las semillas de demo) también se lista");
        response.StatusCode.Should().Be(HttpStatusCode.OK, "ni elegirla puede dar error");
        var json = await Spec020Host.ReadAsync(response);
        json.GetProperty("precio").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Null);
        (await host.CountAsync("visitas")).Should().Be(1);
    }

    [Fact]
    public async Task Id_Mal_Formado_Y_Cuerpo_Invalido_CA_021_15_Eleccion()
    {
        await using var host = await Spec020Host.StartAsync();
        var (requestId, _, _) = await host.QuotedRequestAsync(1);

        using var badId = await host.SendAsync(HttpMethod.Post, "/api/v1/solicitudes/no-es-id/elegir", host.ClientSub, new { cotizacionId = ObjectId.GenerateNewId().ToString() });
        using var badBody = await host.SendAsync(HttpMethod.Post, $"/api/v1/solicitudes/{requestId}/elegir", host.ClientSub, new { cotizacionId = "no-es-un-object-id" });
        using var noBody = await host.SendAsync(HttpMethod.Post, $"/api/v1/solicitudes/{requestId}/elegir", host.ClientSub, new { });

        badId.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        badBody.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await Spec020Host.ReadAsync(badBody)).GetProperty("errors").TryGetProperty("cotizacionId", out _).Should().BeTrue();
        (await badBody.Content.ReadAsStringAsync()).Should().NotContain("no-es-un-object-id", "el motivo nunca repite el valor enviado");
        noBody.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Cuerpo_Demasiado_Grande_Responde_413_R_Limite()
    {
        await using var host = await Spec020Host.StartAsync();
        var (requestId, _, _) = await host.QuotedRequestAsync(1);

        using var response = await host.SendAsync(HttpMethod.Post, $"/api/v1/solicitudes/{requestId}/elegir", host.ClientSub, rawJson: "{\"cotizacionId\":\"" + new string('a', 9000) + "\"}");

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
    }
}
