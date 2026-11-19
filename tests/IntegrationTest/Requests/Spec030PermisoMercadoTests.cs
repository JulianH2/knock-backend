using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Requests;

/// <summary>Spec 030, R-02 y R-03: la ruta interna de Mercado que dice quién participa en la conversación de un par (solicitud, técnico) y si está abierta (MongoDB real).</summary>
public sealed class Spec030PermisoMercadoTests
{
    [Fact]
    public async Task Cliente_Dueno_Con_Tecnico_Que_Cotizo_Obtiene_El_Permiso()
    {
        await using var host = await Spec020Host.StartAsync();
        var (requestId, _, technicians) = await host.QuotedRequestAsync(quotes: 1);
        var technician = technicians[0];

        using var response = await host.PermissionAsync(requestId, host.ClientSub, technician.Id.ToString());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        var json = await Spec020Host.ReadAsync(response);
        json.GetProperty("solicitudId").GetString().Should().Be(requestId.ToString());
        json.GetProperty("tecnicoId").GetString().Should().Be(technician.Id.ToString());
        json.GetProperty("clienteId").GetString().Should().Be(host.ClientSub);
        json.GetProperty("tecnicoUsuarioId").GetString().Should().Be(technician.Sub);
        json.GetProperty("rol").GetString().Should().Be("cliente");
        json.GetProperty("abierta").GetBoolean().Should().BeTrue();
        json.GetProperty("elegido").GetBoolean().Should().BeFalse();
        json.TryGetProperty("motivoCierre", out _).Should().BeFalse();
        json.TryGetProperty("visitaId", out _).Should().BeFalse();
        json.GetProperty("tecnicoPublico").GetProperty("nombre").GetString().Should().NotBeNullOrWhiteSpace();
        json.GetProperty("servicio").GetString().Should().NotBeNullOrWhiteSpace();
        json.GetProperty("coloniaNombre").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Tecnico_Obtiene_Su_Permiso_Y_El_TecnicoId_Que_Mande_Se_Ignora()
    {
        await using var host = await Spec020Host.StartAsync();
        var (requestId, _, technicians) = await host.QuotedRequestAsync(quotes: 2);

        using var withoutId = await host.PermissionAsync(requestId, technicians[0].Sub, technicianId: null);
        using var withOther = await host.PermissionAsync(requestId, technicians[0].Sub, technicians[1].Id.ToString());

        foreach (var response in new[] { withoutId, withOther })
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var json = await Spec020Host.ReadAsync(response);
            json.GetProperty("rol").GetString().Should().Be("tecnico");
            json.GetProperty("tecnicoId").GetString().Should().Be(technicians[0].Id.ToString(), "un técnico nunca habla en nombre de otro");
            json.GetProperty("tecnicoUsuarioId").GetString().Should().Be(technicians[0].Sub);
        }
    }

    [Fact]
    public async Task Ajeno_Recibe_404_CA_030_17()
    {
        await using var host = await Spec020Host.StartAsync();
        var (requestId, _, technicians) = await host.QuotedRequestAsync(quotes: 1);
        var withoutQuote = await host.SeedPublicTechnicianAsync();
        var outsider = Guid.NewGuid().ToString("D");

        using var anotherClient = await host.PermissionAsync(requestId, outsider, technicians[0].Id.ToString());
        using var anotherTechnician = await host.PermissionAsync(requestId, withoutQuote.Sub, technicianId: null);
        using var technicianWithoutQuote = await host.PermissionAsync(requestId, host.ClientSub, withoutQuote.Id.ToString());
        using var unknownTechnician = await host.PermissionAsync(requestId, host.ClientSub, ObjectId.GenerateNewId().ToString());
        using var unknownRequest = await host.PermissionAsync(ObjectId.GenerateNewId(), host.ClientSub, technicians[0].Id.ToString());

        foreach (var response in new[] { anotherClient, anotherTechnician, technicianWithoutQuote, unknownTechnician, unknownRequest })
        {
            response.StatusCode.Should().Be(HttpStatusCode.NotFound, "ajeno, sin cotización o inexistente se ven igual");
        }
    }

    [Fact]
    public async Task Cliente_Sin_Tecnico_O_Con_Identificadores_Mal_Formados_Recibe_400()
    {
        await using var host = await Spec020Host.StartAsync();
        var (requestId, _, _) = await host.QuotedRequestAsync(quotes: 1);

        using var withoutTechnician = await host.PermissionAsync(requestId, host.ClientSub, technicianId: null);
        using var badTechnician = await host.PermissionAsync(requestId, host.ClientSub, "no-es-un-id");
        using var badRequest = await host.SendAsync(HttpMethod.Get, "/api/v1/solicitudes/no-es-un-id/conversacion", host.ClientSub);

        withoutTechnician.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        badTechnician.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        badRequest.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Sin_Sesion_Responde_401()
    {
        await using var host = await Spec020Host.StartAsync();
        var (requestId, _, technicians) = await host.QuotedRequestAsync(quotes: 1);

        using var response = await host.SendAsync(HttpMethod.Get, $"/api/v1/solicitudes/{requestId}/conversacion?tecnicoId={technicians[0].Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("cancelada", "request_cancelled")]
    [InlineData("vencida", "request_expired")]
    [InlineData("terminada", "other_quote_chosen")]
    public async Task Solicitud_Cerrada_Deja_La_Conversacion_En_Solo_Lectura(string state, string reason)
    {
        await using var host = await Spec020Host.StartAsync();
        var (requestId, _, technicians) = await host.QuotedRequestAsync(quotes: 1);
        await host.SetRequestAsync(requestId, state);

        using var response = await host.PermissionAsync(requestId, host.ClientSub, technicians[0].Id.ToString());

        response.StatusCode.Should().Be(HttpStatusCode.OK, "una conversación cerrada se puede leer");
        var json = await Spec020Host.ReadAsync(response);
        json.GetProperty("abierta").GetBoolean().Should().BeFalse();
        json.GetProperty("motivoCierre").GetString().Should().Be(reason);
    }

    [Fact]
    public async Task Cotizacion_Retirada_Cierra_La_Conversacion_Como_Descartada()
    {
        await using var host = await Spec020Host.StartAsync();
        var (requestId, quoteIds, technicians) = await host.QuotedRequestAsync(quotes: 1);
        await host.Database.GetCollection<BsonDocument>("cotizaciones")
            .UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", quoteIds[0]), Builders<BsonDocument>.Update.Set("estado", "retirada"));

        using var response = await host.PermissionAsync(requestId, technicians[0].Sub, technicianId: null);

        var json = await Spec020Host.ReadAsync(response);
        json.GetProperty("abierta").GetBoolean().Should().BeFalse();
        json.GetProperty("motivoCierre").GetString().Should().Be("dismissed");
    }

    [Fact]
    public async Task Tras_Elegir_Solo_El_Elegido_Sigue_Abierto_Con_Su_Visita()
    {
        await using var host = await Spec020Host.StartAsync();
        var (requestId, quoteIds, technicians) = await host.QuotedRequestAsync(quotes: 2);
        (await host.ChooseAsync(requestId, quoteIds[0])).EnsureSuccessStatusCode();
        var visit = await host.Database.GetCollection<BsonDocument>("visitas").Find(Builders<BsonDocument>.Filter.Eq("solicitudId", requestId)).SingleAsync();

        using var chosen = await host.PermissionAsync(requestId, technicians[0].Sub, technicianId: null);
        using var other = await host.PermissionAsync(requestId, host.ClientSub, technicians[1].Id.ToString());

        var chosenJson = await Spec020Host.ReadAsync(chosen);
        chosenJson.GetProperty("abierta").GetBoolean().Should().BeTrue();
        chosenJson.GetProperty("elegido").GetBoolean().Should().BeTrue();
        chosenJson.GetProperty("visitaId").GetString().Should().Be(visit["_id"].AsObjectId.ToString());
        var otherJson = await Spec020Host.ReadAsync(other);
        otherJson.GetProperty("abierta").GetBoolean().Should().BeFalse();
        otherJson.GetProperty("elegido").GetBoolean().Should().BeFalse();
        otherJson.GetProperty("motivoCierre").GetString().Should().Be("other_quote_chosen");
        otherJson.TryGetProperty("visitaId", out _).Should().BeFalse("el otro técnico no ve la visita ajena");
    }

    [Fact]
    public async Task La_Respuesta_No_Lleva_Contacto_Ni_Direcciones()
    {
        await using var host = await Spec020Host.StartAsync();
        var (requestId, quoteIds, technicians) = await host.QuotedRequestAsync(quotes: 1);
        (await host.ChooseAsync(requestId, quoteIds[0])).EnsureSuccessStatusCode();

        using var response = await host.PermissionAsync(requestId, host.ClientSub, technicians[0].Id.ToString());

        var text = await response.Content.ReadAsStringAsync();
        foreach (var forbidden in new[] { "telefono", "whatsapp", "direccion", "tallerDireccion", "contactoPublico", "codigo" })
        {
            text.Should().NotContain($"\"{forbidden}\"");
        }

        JsonElement.Parse(text).GetProperty("tecnicoPublico").EnumerateObject().Select(p => p.Name).Should().BeSubsetOf(["nombre", "fotoUrl"]);
    }
}

/// <summary>Llamada a la ruta interna de permiso de la spec 030.</summary>
internal static class Spec030Host
{
    public static Task<HttpResponseMessage> PermissionAsync(this Spec020Host host, ObjectId requestId, string sub, string? technicianId) =>
        host.SendAsync(HttpMethod.Get, $"/api/v1/solicitudes/{requestId}/conversacion" + (technicianId is null ? string.Empty : "?tecnicoId=" + technicianId), sub);
}
