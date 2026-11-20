using System.Net;
using AwesomeAssertions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.IntegrationTest.Requests;

public sealed class Spec028CotizarTests
{
    [Fact]
    public async Task Cotizar_Exige_Envio_Activo_CA_028_11()
    {
        await using var host = await StartAsync(true);
        var id = await RequestAsync(host);
        var sub = await TechnicianAsync(host);
        using var missing = await QuoteAsync(host, id, sub);
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await SendAsync(host, id, sub, "activo");
        using var accepted = await QuoteAsync(host, id, sub);
        accepted.StatusCode.Should().Be(HttpStatusCode.Created);
        var send = await host.Database.GetCollection<BsonDocument>("envios").Find(new BsonDocument("tecnicoUsuarioId", sub)).SingleAsync();
        send["estado"].AsString.Should().Be("cotizado");
    }

    [Theory]
    [InlineData("descartado")]
    [InlineData("cerrado")]
    [InlineData("cotizado")]
    public async Task Cotizar_Rechaza_Envio_No_Activo_CA_028_11(string state)
    {
        await using var host = await StartAsync(true);
        var id = await RequestAsync(host);
        var sub = await TechnicianAsync(host);
        await SendAsync(host, id, sub, state);
        using var response = await QuoteAsync(host, id, sub);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Tope_Condicionado_Concurrencia_Y_Sexta_CA_028_06()
    {
        await using var host = await StartAsync(true);
        var id = await RequestAsync(host);
        var people = new List<string>();
        for (var i = 0; i < 7; i++)
        {
            var sub = await TechnicianAsync(host);
            people.Add(sub);
            await SendAsync(host, id, sub, "activo");
        }
        for (var i = 0; i < 4; i++)
        {
            using var response = await QuoteAsync(host, id, people[i]);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }
        var results = await Task.WhenAll(QuoteAsync(host, id, people[4]), QuoteAsync(host, id, people[5]));
        try
        {
            results.Select(x => x.StatusCode).Should().BeEquivalentTo([HttpStatusCode.Created, HttpStatusCode.Conflict]);
            await QuoteHttp.Code(results.Single(x => x.StatusCode == HttpStatusCode.Conflict), "quote_quota_full");
        }
        finally { foreach (var result in results) result.Dispose(); }
        using var sixth = await QuoteAsync(host, id, people[6]);
        sixth.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await QuoteHttp.Code(sixth, "quote_quota_full");
        (await Spec020Host.ReadAsync(sixth)).GetProperty("detail").GetString().Should().Be("Esta solicitud ya tiene suficientes cotizaciones");
        var rejectedSend = await host.Database.GetCollection<BsonDocument>("envios").Find(new BsonDocument("tecnicoUsuarioId", people[6])).SingleAsync();
        rejectedSend["estado"].AsString.Should().Be("activo");
        var request = await host.Database.GetCollection<BsonDocument>("solicitudes").Find(new BsonDocument("_id", ObjectId.Parse(id))).SingleAsync();
        request["cotizacionesActivas"].AsInt32.Should().Be(5);
        (await host.Database.GetCollection<BsonDocument>("cotizaciones").CountDocumentsAsync(new BsonDocument("solicitudId", ObjectId.Parse(id)))).Should().Be(5);
    }

    [Theory]
    [InlineData("activo", HttpStatusCode.OK)]
    [InlineData("cotizado", HttpStatusCode.OK)]
    [InlineData("descartado", HttpStatusCode.NotFound)]
    [InlineData("cerrado", HttpStatusCode.NotFound)]
    public async Task Detalle_Respeta_Estado_Del_Envio_CA_028_14(string state, HttpStatusCode expected)
    {
        await using var host = await StartAsync(true);
        var id = await RequestAsync(host);
        var sub = await TechnicianAsync(host);
        await SendAsync(host, id, sub, state);
        using var response = await host.SendAsync(HttpMethod.Get, "/api/v1/solicitudes/" + id, sub);
        response.StatusCode.Should().Be(expected);
        if (expected == HttpStatusCode.OK)
        {
            var body = await Spec020Host.ReadAsync(response);
            body.GetProperty("colonia").GetProperty("nombre").GetString().Should().Be("Centro");
            body.GetProperty("direccion").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Null);
        }
    }

    [Fact]
    public async Task Apagado_Conserva_Elegibilidad_Base_CA_028_16()
    {
        await using var host = await StartAsync(false);
        var id = await RequestAsync(host);
        var eligible = await TechnicianAsync(host);
        var other = Guid.NewGuid().ToString("D");
        await host.SeedTechnicianAsync(other, "activo", false, ["ele"], host.ColoniaId);
        using var rejected = await QuoteAsync(host, id, other);
        using var accepted = await QuoteAsync(host, id, eligible);
        rejected.StatusCode.Should().Be(HttpStatusCode.NotFound);
        accepted.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private static Task<Spec020Host> StartAsync(bool enabled) => Spec020Host.StartAsync(new Dictionary<string, string?>
    {
        ["Oleadas:Habilitado"] = enabled.ToString(),
        ["TareasProgramadas:IntervaloSegundos"] = "3600",
    });

    private static async Task<string> RequestAsync(Spec020Host host)
    {
        using var response = await host.CreateAsync(host.ValidBody());
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await Spec020Host.ReadAsync(response)).GetProperty("id").GetString()!;
    }

    private static async Task<string> TechnicianAsync(Spec020Host host)
    {
        var sub = Guid.NewGuid().ToString("D");
        await host.SeedTechnicianAsync(sub, "activo", false, [Spec020Host.Plumbing], host.ColoniaId);
        return sub;
    }

    private static async Task SendAsync(Spec020Host host, string id, string sub, string state)
    {
        var technician = await host.Database.GetCollection<BsonDocument>("tecnicos").Find(new BsonDocument("usuarioId", sub)).SingleAsync();
        await host.Database.GetCollection<BsonDocument>("envios").InsertOneAsync(new BsonDocument
        {
            ["solicitudId"] = ObjectId.Parse(id),
            ["tecnicoId"] = technician["_id"],
            ["tecnicoUsuarioId"] = sub,
            ["estado"] = state,
            ["oleada"] = 1,
            ["enviadoEn"] = DateTime.UtcNow,
        });
    }

    private static Task<HttpResponseMessage> QuoteAsync(Spec020Host host, string id, string sub) =>
        host.SendAsync(HttpMethod.Post, $"/api/v1/solicitudes/{id}/cotizaciones", sub,
            new { tipoPrecio = "fijo", precio = 100, dia = Spec020Host.Tomorrow(), hora = "12:00" });
}
