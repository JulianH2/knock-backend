using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Requests;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Tecnicos;

/// <summary><c>POST /api/v1/tecnicos</c> (spec 046, TEC-01) sobre MongoDB real, con el host de Mercado de la spec 020. Solo datos ficticios.</summary>
public sealed class Spec046RegistroHttpTests
{
    private const string _route = "/api/v1/tecnicos";
    private const string _workshop = "Calle Ficticia 123, Colonia Piloto";

    private static Dictionary<string, object?> Body(Spec020Host host, string atencion = "domicilio") => new()
    {
        ["nombre"] = "Ana Pruebas",
        ["oficios"] = new[] { Spec020Host.Plumbing },
        ["atencion"] = atencion,
        ["direccionTaller"] = atencion == "domicilio" ? null : _workshop,
        ["radioKm"] = atencion == "taller" ? null : 5,
        ["coloniaId"] = host.ColoniaId.ToString(),
        ["perfil"] = new Dictionary<string, object?> { ["nombreNegocio"] = "Plomería Ana", ["anios"] = 4 },
        ["aceptaTerminos"] = true,
    };

    private static IMongoCollection<BsonDocument> Technicians(Spec020Host host) => host.Database.GetCollection<BsonDocument>("tecnicos");

    private static Task<string> UniqueIndexAsync(Spec020Host host) =>
        Technicians(host).Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("usuarioId"), new CreateIndexOptions { Unique = true, Name = "usuarioId_1" }));

    [Fact]
    public async Task Registro_Valido_Responde_201_Y_Guarda_El_Perfil_CA_046_01()
    {
        await using var host = await Spec020Host.StartAsync();
        var sub = Guid.NewGuid().ToString("D");

        using var response = await host.SendAsync(HttpMethod.Post, _route, sub, Body(host, "ambos"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        var json = await Spec020Host.ReadAsync(response);
        var id = json.GetProperty("id").GetString();
        response.Headers.Location!.ToString().Should().Be($"/api/v1/tecnicos/{id}");
        json.GetProperty("estado").GetString().Should().Be("activo");
        json.GetProperty("disponibleHoy").GetBoolean().Should().BeFalse();
        json.TryGetProperty("direccionTaller", out _).Should().BeFalse();
        (await response.Content.ReadAsStringAsync()).Should().NotContain("Ficticia");
        var stored = await Technicians(host).Find(Builders<BsonDocument>.Filter.Eq("usuarioId", sub)).SingleAsync();
        stored["_id"].AsObjectId.ToString().Should().Be(id);
        stored["coloniaBaseId"].AsString.Should().Be(host.ColoniaId.ToString());
        stored["tallerDireccion"].AsString.Should().NotContain("Ficticia");
        stored["radioAtencionKm"].AsInt32.Should().Be(5);
    }

    [Fact]
    public async Task Sin_Token_Responde_401_Y_No_Guarda_CA_046_02()
    {
        await using var host = await Spec020Host.StartAsync();
        using var response = await host.SendAsync(HttpMethod.Post, _route, sub: null, Body(host));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await Technicians(host).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(0);
    }

    [Fact]
    public async Task Valor_Invalido_Responde_422_Sin_Repetirlo_Y_No_Guarda_CA_046_03()
    {
        await using var host = await Spec020Host.StartAsync();
        var body = Body(host);
        body["radioKm"] = 7;
        body["nombre"] = "<b>Mallory</b>";

        using var response = await host.SendAsync(HttpMethod.Post, _route, Guid.NewGuid().ToString("D"), body);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var text = await response.Content.ReadAsStringAsync();
        text.Should().Contain("radioKm").And.Contain("nombre").And.NotContain("Mallory");
        (await Technicians(host).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(0);
    }

    [Fact]
    public async Task Oficio_Inactivo_Responde_422_Con_Codigo_CA_046_55()
    {
        await using var host = await Spec020Host.StartAsync();
        await host.SeedTradeAsync("vie", "Oficio viejo", "domicilio", active: false);
        var body = Body(host);
        body["oficios"] = new[] { Spec020Host.Plumbing, "vie" };

        using var response = await host.SendAsync(HttpMethod.Post, _route, Guid.NewGuid().ToString("D"), body);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await Spec020Host.ReadAsync(response)).GetProperty("code").GetString().Should().Be("trade_invalid");
        (await Technicians(host).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(0);
    }

    [Fact]
    public async Task Cuerpo_Ilegible_Responde_400_Y_No_Guarda_CA_046_04()
    {
        await using var host = await Spec020Host.StartAsync();
        using var response = await host.SendAsync(HttpMethod.Post, _route, Guid.NewGuid().ToString("D"), rawJson: "{\"nombre\": ");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Technicians(host).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(0);
    }

    [Fact]
    public async Task Persona_Que_Ya_Es_Tecnico_Responde_409_CA_046_05()
    {
        await using var host = await Spec020Host.StartAsync();
        var sub = Guid.NewGuid().ToString("D");
        using var first = await host.SendAsync(HttpMethod.Post, _route, sub, Body(host));
        using var second = await host.SendAsync(HttpMethod.Post, _route, sub, Body(host, "taller"));

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Spec020Host.ReadAsync(second)).GetProperty("code").GetString().Should().Be("already_a_technician");
        var stored = await Technicians(host).Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        stored["atencion"].AsString.Should().Be("domicilio");
    }

    [Fact]
    public async Task Identificador_Ajeno_En_El_Cuerpo_Se_Ignora_CA_046_66()
    {
        await using var host = await Spec020Host.StartAsync();
        var sub = Guid.NewGuid().ToString("D");
        var body = Body(host);
        body["usuarioId"] = "otra-persona";
        body["estado"] = "suspendido";

        using var response = await host.SendAsync(HttpMethod.Post, _route, sub, body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var stored = await Technicians(host).Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        stored["usuarioId"].AsString.Should().Be(sub);
        stored["estado"].AsString.Should().Be("activo");
    }

    [Fact]
    public async Task Dos_Registros_A_La_Vez_Dejan_Un_Solo_Perfil_CA_046_69()
    {
        await using var host = await Spec020Host.StartAsync();
        await UniqueIndexAsync(host);
        var sub = Guid.NewGuid().ToString("D");

        var responses = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => host.SendAsync(HttpMethod.Post, _route, sub, Body(host))));

        responses.Count(r => r.StatusCode == HttpStatusCode.Created).Should().Be(1);
        responses.Count(r => r.StatusCode == HttpStatusCode.Conflict).Should().Be(3);
        (await Technicians(host).CountDocumentsAsync(Builders<BsonDocument>.Filter.Eq("usuarioId", sub))).Should().Be(1);
        foreach (var response in responses) response.Dispose();
    }

    [Fact]
    public async Task Cuerpo_Demasiado_Grande_Responde_413_CA_046_68()
    {
        await using var host = await Spec020Host.StartAsync();
        var body = Body(host);
        body["otroOficio"] = new string('a', 9000);
        using var response = await host.SendAsync(HttpMethod.Post, _route, Guid.NewGuid().ToString("D"), body);
        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
    }
}
