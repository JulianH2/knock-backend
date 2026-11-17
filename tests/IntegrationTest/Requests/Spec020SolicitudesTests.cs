using System.Net;
using AwesomeAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Requests;

/// <summary>Spec 020, T-05 y T-06: crear una solicitud y ver su detalle según quién lo pide (MongoDB real, fotos y moderación de mentira).</summary>
public sealed class Spec020SolicitudesTests
{
    [Fact]
    public async Task Crea_Solicitud_Valida_CA_020_01()
    {
        await using var host = await Spec020Host.StartAsync();

        using var response = await host.CreateAsync(host.ValidBody());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.ToString().Should().StartWith("/api/v1/solicitudes/");
        response.Headers.CacheControl!.Private.Should().BeTrue();
        response.Headers.CacheControl.NoStore.Should().BeTrue("la respuesta es personal y no se cachea");
        response.Headers.Contains("ETag").Should().BeFalse();
        var json = await Spec020Host.ReadAsync(response);
        var id = json.GetProperty("id").GetString()!;
        json.GetProperty("estado").GetString().Should().Be("enviada");
        json.GetProperty("modalidad").GetString().Should().Be("domicilio");
        json.GetProperty("direccion").GetString().Should().Be(Spec020Host.Address, "la vista del cliente dueño lleva la dirección descifrada");
        json.GetProperty("totalCotizaciones").GetInt32().Should().Be(0);
        (json.GetProperty("venceEn").GetDateTimeOffset() - json.GetProperty("creadoEn").GetDateTimeOffset()).Should().BeCloseTo(TimeSpan.FromHours(48), TimeSpan.FromSeconds(1));

        var stored = await host.Database.GetCollection<BsonDocument>("solicitudes").Find(new BsonDocument("_id", ObjectId.Parse(id))).SingleAsync();
        stored["clienteId"].AsString.Should().Be(host.ClientSub);
        stored["estado"].AsString.Should().Be("enviada");
        stored.ToJson().Should().NotContain("Calle Ficticia", "la dirección se guarda cifrada");
        stored["direccionCifrada"].AsString.Should().StartWith("1.", "lleva la versión de la llave");

        var queued = await host.Database.GetCollection<BsonDocument>("outbox").Find(new BsonDocument("_id", "SolicitudCreada:" + id)).SingleAsync();
        queued["estado"].AsString.Should().Be("pendiente");
        queued["datos"]["solicitudId"].AsString.Should().Be(id);
        queued.ToJson().Should().NotContain("fregadero").And.NotContain("Calle Ficticia", "el evento no lleva la descripción ni la dirección");
    }

    [Fact]
    public async Task Oculta_Detalle_Ajeno_CA_020_12()
    {
        await using var host = await Spec020Host.StartAsync();
        using var created = await host.CreateAsync(host.ValidBody());
        var id = (await Spec020Host.ReadAsync(created)).GetProperty("id").GetString()!;
        var otherClient = Guid.NewGuid().ToString("D");
        var otherTrade = Guid.NewGuid().ToString("D");
        await host.SeedTechnicianAsync(otherTrade, "activo", hidden: false, [Spec020Host.Workshop], host.ColoniaId);

        using var asOtherClient = await host.SendAsync(HttpMethod.Get, "/api/v1/solicitudes/" + id, otherClient);
        using var asOtherTrade = await host.SendAsync(HttpMethod.Get, "/api/v1/solicitudes/" + id, otherTrade);
        using var unknown = await host.SendAsync(HttpMethod.Get, "/api/v1/solicitudes/" + ObjectId.GenerateNewId(), host.ClientSub);

        asOtherClient.StatusCode.Should().Be(HttpStatusCode.NotFound);
        asOtherTrade.StatusCode.Should().Be(HttpStatusCode.NotFound, "un técnico de otro oficio no ve la solicitud");
        unknown.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var ajena = await Spec020Host.ReadAsync(asOtherClient);
        var inexistente = await Spec020Host.ReadAsync(unknown);
        ajena.GetProperty("status").GetInt32().Should().Be(inexistente.GetProperty("status").GetInt32());
        ajena.GetProperty("title").GetString().Should().Be(inexistente.GetProperty("title").GetString(), "el 404 es uniforme: no distingue ajena de inexistente");
        ajena.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(inexistente.EnumerateObject().Select(p => p.Name));
    }

    [Fact]
    public async Task Id_Mal_Formado_Responde_400_R_08()
    {
        await using var host = await Spec020Host.StartAsync();

        using var response = await host.SendAsync(HttpMethod.Get, "/api/v1/solicitudes/no-es-un-object-id", host.ClientSub);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Vista_Tecnico_Sin_Direccion_CA_020_13()
    {
        await using var host = await Spec020Host.StartAsync();
        using var created = await host.CreateAsync(host.ValidBody());
        var id = (await Spec020Host.ReadAsync(created)).GetProperty("id").GetString()!;
        var technician = Guid.NewGuid().ToString("D");
        await host.SeedTechnicianAsync(technician, "activo", hidden: false, [Spec020Host.Plumbing], host.ColoniaId);

        using var response = await host.SendAsync(HttpMethod.Get, "/api/v1/solicitudes/" + id, technician);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var raw = await response.Content.ReadAsStringAsync();
        var json = System.Text.Json.JsonElement.Parse(raw);
        json.GetProperty("oficio").GetProperty("clave").GetString().Should().Be(Spec020Host.Plumbing);
        json.GetProperty("colonia").GetProperty("nombre").GetString().Should().Be("Centro");
        json.GetProperty("descripcion").GetString().Should().Be(Spec020Host.Description);
        json.GetProperty("fotos").GetArrayLength().Should().Be(1);
        json.GetProperty("fotos")[0].GetProperty("url").GetString().Should().StartWith("https://files.test/");
        json.TryGetProperty("direccion", out var address).Should().BeTrue();
        address.ValueKind.Should().Be(System.Text.Json.JsonValueKind.Null, "la vista del técnico nunca trae la dirección");
        json.GetProperty("totalCotizaciones").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Null);
        raw.Should().NotContain("Calle Ficticia").And.NotContain(host.ClientSub, "ni datos de quién es el cliente");
    }

    [Theory]
    [InlineData("suspendido", false)]
    [InlineData("activo", true)]
    public async Task Tecnico_Suspendido_U_Oculto_No_Ve_La_Solicitud_R_23(string state, bool hidden)
    {
        await using var host = await Spec020Host.StartAsync();
        using var created = await host.CreateAsync(host.ValidBody());
        var id = (await Spec020Host.ReadAsync(created)).GetProperty("id").GetString()!;
        var technician = Guid.NewGuid().ToString("D");
        await host.SeedTechnicianAsync(technician, state, hidden, [Spec020Host.Plumbing], host.ColoniaId);

        using var response = await host.SendAsync(HttpMethod.Get, "/api/v1/solicitudes/" + id, technician);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Tecnico_Que_No_Atiende_La_Colonia_No_Ve_La_Solicitud_R_23()
    {
        await using var host = await Spec020Host.StartAsync();
        using var created = await host.CreateAsync(host.ValidBody());
        var id = (await Spec020Host.ReadAsync(created)).GetProperty("id").GetString()!;
        var technician = Guid.NewGuid().ToString("D");
        var elsewhere = await host.SeedColoniaAsync("Lejana", "42999");
        await host.SeedTechnicianAsync(technician, "activo", hidden: false, [Spec020Host.Plumbing], elsewhere);

        using var response = await host.SendAsync(HttpMethod.Get, "/api/v1/solicitudes/" + id, technician);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
