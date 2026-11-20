using System.Net;
using AwesomeAssertions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.IntegrationTest.Requests;

public sealed class Spec028BandejaTests
{
    [Fact]
    public async Task Bandeja_Solo_Envios_Propios_Sin_Datos_Del_Cliente_CA_028_12()
    {
        await using var host = await StartAsync();
        var sub = await TechnicianAsync(host);
        var other = await TechnicianAsync(host);
        for (var i = 0; i < 3; i++) await CreateSendAsync(host, sub);
        await CreateSendAsync(host, other);
        using var response = await host.SendAsync(HttpMethod.Get, "/api/v1/tecnicos/me/solicitudes", sub);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl!.Private.Should().BeTrue();
        response.Headers.CacheControl.NoStore.Should().BeTrue();
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().NotContain(Spec020Host.Address).And.NotContain(host.ClientSub);
        var body = System.Text.Json.JsonElement.Parse(raw);
        body.GetProperty("items").GetArrayLength().Should().Be(3);
        using var client = await host.SendAsync(HttpMethod.Get, "/api/v1/tecnicos/me/solicitudes", host.ClientSub);
        client.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Spec020Host.ReadAsync(client)).GetProperty("code").GetString().Should().Be("not_a_technician");
    }

    [Fact]
    public async Task Descarte_Es_Idempotente_Y_Exige_Envio_CA_028_13()
    {
        await using var host = await StartAsync();
        var sub = await TechnicianAsync(host);
        var id = await CreateSendAsync(host, sub);
        using var first = await DiscardAsync(host, id, sub);
        using var again = await DiscardAsync(host, id, sub);
        first.StatusCode.Should().Be(HttpStatusCode.NoContent);
        again.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var send = await host.Database.GetCollection<BsonDocument>("envios").Find(new BsonDocument("solicitudId", ObjectId.Parse(id))).SingleAsync();
        send["estado"].AsString.Should().Be("descartado");
        send.Contains("descartadoEn").Should().BeTrue();
        using var missing = await DiscardAsync(host, ObjectId.GenerateNewId().ToString(), sub);
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var inbox = await host.SendAsync(HttpMethod.Get, "/api/v1/tecnicos/me/solicitudes", sub);
        (await Spec020Host.ReadAsync(inbox)).GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Descartar_Y_Cotizar_No_Confirman_Ambas_CA_028_13()
    {
        await using var host = await StartAsync();
        var sub = await TechnicianAsync(host);
        var id = await CreateSendAsync(host, sub);
        var results = await Task.WhenAll(
            DiscardAsync(host, id, sub),
            host.SendAsync(HttpMethod.Post, $"/api/v1/solicitudes/{id}/cotizaciones", sub,
                new { tipoPrecio = "fijo", precio = 100, dia = Spec020Host.Tomorrow(), hora = "12:00" }));
        try
        {
            var discarded = results[0].StatusCode == HttpStatusCode.NoContent;
            var quoted = results[1].StatusCode == HttpStatusCode.Created;
            (discarded ^ quoted).Should().BeTrue();
            var send = await host.Database.GetCollection<BsonDocument>("envios").Find(new BsonDocument("solicitudId", ObjectId.Parse(id))).SingleAsync();
            send["estado"].AsString.Should().Be(quoted ? "cotizado" : "descartado");
        }
        finally { foreach (var result in results) result.Dispose(); }
    }

    [Fact]
    public async Task Envio_Descartado_No_Admite_Cotizacion_CA_028_13()
    {
        await using var host = await StartAsync();
        var sub = await TechnicianAsync(host);
        var id = await CreateSendAsync(host, sub);
        await host.Database.GetCollection<BsonDocument>("envios").UpdateOneAsync(
            new BsonDocument("solicitudId", ObjectId.Parse(id)),
            new BsonDocument("$set", new BsonDocument("estado", "descartado")));
        using var response = await host.SendAsync(HttpMethod.Post, $"/api/v1/solicitudes/{id}/cotizaciones", sub,
            new { tipoPrecio = "fijo", precio = 100, dia = Spec020Host.Tomorrow(), hora = "12:00" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await host.Database.GetCollection<BsonDocument>("cotizaciones").CountDocumentsAsync(new BsonDocument("solicitudId", ObjectId.Parse(id)))).Should().Be(0);
    }

    [Fact]
    public async Task Envio_Cotizado_No_Se_Puede_Descartar_CA_028_13()
    {
        await using var host = await StartAsync();
        var sub = await TechnicianAsync(host);
        var id = await CreateSendAsync(host, sub);
        using var quote = await host.SendAsync(HttpMethod.Post, $"/api/v1/solicitudes/{id}/cotizaciones", sub,
            new { tipoPrecio = "fijo", precio = 100, dia = Spec020Host.Tomorrow(), hora = "12:00" });
        quote.StatusCode.Should().Be(HttpStatusCode.Created);
        using var response = await DiscardAsync(host, id, sub);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Spec020Host.ReadAsync(response)).GetProperty("code").GetString().Should().Be("request_not_dismissable");
    }

    [Fact]
    public async Task Bandeja_Pagina_Y_Rechaza_Limites_Invalidos_CA_028_12()
    {
        await using var host = await StartAsync();
        var sub = await TechnicianAsync(host);
        for (var i = 0; i < 3; i++) await CreateSendAsync(host, sub);
        using var first = await host.SendAsync(HttpMethod.Get, "/api/v1/tecnicos/me/solicitudes?limit=2", sub);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await Spec020Host.ReadAsync(first);
        page.GetProperty("items").GetArrayLength().Should().Be(2);
        var cursor = page.GetProperty("nextCursor").GetString();
        cursor.Should().NotBeNullOrEmpty();
        using var second = await host.SendAsync(HttpMethod.Get, $"/api/v1/tecnicos/me/solicitudes?limit=2&cursor={cursor}", sub);
        (await Spec020Host.ReadAsync(second)).GetProperty("items").GetArrayLength().Should().Be(1);
        using var invalid = await host.SendAsync(HttpMethod.Get, "/api/v1/tecnicos/me/solicitudes?limit=21", sub);
        invalid.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        using var badCursor = await host.SendAsync(HttpMethod.Get, "/api/v1/tecnicos/me/solicitudes?cursor=!", sub);
        badCursor.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Sin_Token_Responde_401_CA_028_18()
    {
        await using var host = await StartAsync();
        using var inbox = await host.SendAsync(HttpMethod.Get, "/api/v1/tecnicos/me/solicitudes");
        using var discard = await host.SendAsync(HttpMethod.Post, $"/api/v1/solicitudes/{ObjectId.GenerateNewId()}/descartar");
        inbox.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        discard.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static Task<Spec020Host> StartAsync() => Spec020Host.StartAsync(new Dictionary<string, string?>
    {
        ["Oleadas:Habilitado"] = "true",
        ["TareasProgramadas:IntervaloSegundos"] = "3600",
    });

    private static async Task<string> TechnicianAsync(Spec020Host host)
    {
        var sub = Guid.NewGuid().ToString("D");
        await host.SeedTechnicianAsync(sub, "activo", false, [Spec020Host.Plumbing], host.ColoniaId);
        return sub;
    }

    private static async Task<string> CreateSendAsync(Spec020Host host, string sub)
    {
        using var response = await host.CreateAsync(host.ValidBody());
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = (await Spec020Host.ReadAsync(response)).GetProperty("id").GetString()!;
        var technician = await host.Database.GetCollection<BsonDocument>("tecnicos").Find(new BsonDocument("usuarioId", sub)).SingleAsync();
        await host.Database.GetCollection<BsonDocument>("envios").InsertOneAsync(new BsonDocument
        {
            ["solicitudId"] = ObjectId.Parse(id),
            ["tecnicoId"] = technician["_id"],
            ["tecnicoUsuarioId"] = sub,
            ["estado"] = "activo",
            ["oleada"] = 1,
            ["enviadoEn"] = DateTime.UtcNow,
            ["actualizadoEn"] = DateTime.UtcNow,
        });
        return id;
    }

    private static Task<HttpResponseMessage> DiscardAsync(Spec020Host host, string id, string sub) =>
        host.SendAsync(HttpMethod.Post, $"/api/v1/solicitudes/{id}/descartar", sub);
}
