using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Requests;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using Xunit;

namespace Knock.IntegrationTest.Tecnicos;

/// <summary><c>GET /tecnicos/{id}/llega-a-mi-colonia</c> (TEC-06) y <c>GET /tecnicos/autocompletar</c> (TEC-03) sobre MongoDB real. Solo datos ficticios.</summary>
public sealed class Spec046BusquedaHttpTests
{
    private static async Task<ObjectId> SeedAsync(Spec020Host host, bool oculto = false, string negocio = "Plomería Ana")
    {
        var id = ObjectId.GenerateNewId();
        await host.Database.GetCollection<BsonDocument>("tecnicos").InsertOneAsync(new BsonDocument
        {
            ["_id"] = id,
            ["usuarioId"] = Guid.NewGuid().ToString("D"),
            ["nombre"] = "Ana Pruebas",
            ["nombreNegocio"] = negocio,
            ["estado"] = "activo",
            ["oculto"] = oculto,
            ["oficios"] = new BsonArray { Spec020Host.Plumbing },
            ["atencion"] = "taller",
            ["coloniaBaseId"] = host.ColoniaId.ToString(),
        });
        return id;
    }

    [Fact]
    public async Task Llega_A_Su_Colonia_Base_Responde_200_CA_046_20()
    {
        await using var host = await Spec020Host.StartAsync();
        var id = await SeedAsync(host);
        using var response = await host.SendAsync(HttpMethod.Get, $"/api/v1/tecnicos/{id}/llega-a-mi-colonia?colonia={host.ColoniaId}", host.ClientSub);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Spec020Host.ReadAsync(response)).GetProperty("atiende").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Llega_A_Mi_Colonia_Sin_Token_Responde_401_CA_046_21()
    {
        await using var host = await Spec020Host.StartAsync();
        var id = await SeedAsync(host);
        using var response = await host.SendAsync(HttpMethod.Get, $"/api/v1/tecnicos/{id}/llega-a-mi-colonia?colonia={host.ColoniaId}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Colonia_Mal_Escrita_O_Fuera_Del_Catalogo_Responde_422_CA_046_22()
    {
        await using var host = await Spec020Host.StartAsync();
        var id = await SeedAsync(host);
        using var malformed = await host.SendAsync(HttpMethod.Get, $"/api/v1/tecnicos/{id}/llega-a-mi-colonia?colonia=centro", host.ClientSub);
        using var unknown = await host.SendAsync(HttpMethod.Get, $"/api/v1/tecnicos/{id}/llega-a-mi-colonia?colonia={ObjectId.GenerateNewId()}", host.ClientSub);
        malformed.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        ProblemBody.Reasons(await malformed.Content.ReadAsStringAsync()).Should().NotContain("centro");
        unknown.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await Spec020Host.ReadAsync(unknown)).GetProperty("code").GetString().Should().Be("colonia_not_found");
    }

    [Fact]
    public async Task Tecnico_Oculto_Responde_404_CA_046_23()
    {
        await using var host = await Spec020Host.StartAsync();
        var id = await SeedAsync(host, oculto: true);
        using var response = await host.SendAsync(HttpMethod.Get, $"/api/v1/tecnicos/{id}/llega-a-mi-colonia?colonia={host.ColoniaId}", host.ClientSub);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Spec020Host.ReadAsync(response)).GetProperty("code").GetString().Should().Be("technician_not_found");
    }

    [Fact]
    public async Task Autocompletar_Sin_Sesion_Sugiere_Oficios_Y_Negocios_Visibles_CA_046_10()
    {
        await using var host = await Spec020Host.StartAsync();
        await SeedAsync(host, negocio: "Plomería Ana");
        await SeedAsync(host, oculto: true, negocio: "Plomería Oculta");

        using var response = await host.SendAsync(HttpMethod.Get, "/api/v1/tecnicos/autocompletar?q=plomer");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var texts = (await Spec020Host.ReadAsync(response)).GetProperty("sugerencias").EnumerateArray().Select(s => s.GetProperty("texto").GetString()).ToList();
        texts.Should().Contain("Plomería").And.Contain("Plomería Ana").And.NotContain("Plomería Oculta");
        texts.Should().HaveCountLessThanOrEqualTo(8);
    }

    [Fact]
    public async Task Autocompletar_Con_Texto_Invalido_Responde_422_CA_046_11()
    {
        await using var host = await Spec020Host.StartAsync();
        using var shortText = await host.SendAsync(HttpMethod.Get, "/api/v1/tecnicos/autocompletar?q=p");
        using var symbols = await host.SendAsync(HttpMethod.Get, "/api/v1/tecnicos/autocompletar?q=%2A.%2A");
        shortText.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        symbols.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        ProblemBody.Reasons(await symbols.Content.ReadAsStringAsync()).Should().NotContain("*.*");
    }
}
