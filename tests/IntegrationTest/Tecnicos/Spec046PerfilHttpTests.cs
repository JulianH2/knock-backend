using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Requests;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Tecnicos;

/// <summary><c>GET /api/v1/tecnicos/{id}</c> (spec 046, TEC-05) sobre MongoDB real. Solo datos ficticios.</summary>
public sealed class Spec046PerfilHttpTests
{
    private const string _phone = "5512345678";
    private const string _workshop = "Taller Ficticio 45, Colonia Piloto";

    private static async Task<ObjectId> SeedAsync(Spec020Host host, string estado = "activo", bool oculto = false)
    {
        var id = ObjectId.GenerateNewId();
        await host.Database.GetCollection<BsonDocument>("tecnicos").InsertOneAsync(new BsonDocument
        {
            ["_id"] = id,
            ["usuarioId"] = Guid.NewGuid().ToString("D"),
            ["nombre"] = "Ana Pruebas",
            ["nombreNegocio"] = "Plomería Ana",
            ["estado"] = estado,
            ["oculto"] = oculto,
            ["oficios"] = new BsonArray { Spec020Host.Plumbing },
            ["atencion"] = "ambos",
            ["coloniaBaseId"] = host.ColoniaId.ToString(),
            ["radioAtencionKm"] = 5,
            ["nivelVerificacion"] = "basica",
            ["descripcion"] = "Arreglo fugas y calentadores",
            ["anios"] = 7,
            ["tallerDireccion"] = _workshop,
            ["contactoPublico"] = new BsonDocument { ["telefono"] = _phone, ["whatsapp"] = _phone },
            ["ubicacionBase"] = new BsonDocument { ["type"] = "Point", ["coordinates"] = new BsonArray { -99.34, 20.05 } },
            ["disponibleHasta"] = DateTime.UtcNow.AddHours(2),
        });
        return id;
    }

    [Fact]
    public async Task Sin_Sesion_Responde_200_Solo_Con_El_Resumen_CA_046_16()
    {
        await using var host = await Spec020Host.StartAsync();
        var id = await SeedAsync(host);

        using var response = await host.SendAsync(HttpMethod.Get, $"/api/v1/tecnicos/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await Spec020Host.ReadAsync(response);
        json.GetProperty("nombre").GetString().Should().Be("Ana Pruebas");
        json.GetProperty("verificacion").GetString().Should().Be("basica");
        json.GetProperty("detalle").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Null);
    }

    [Fact]
    public async Task Con_Sesion_Suma_El_Detalle_Sin_Telefono_Ni_Direccion_CA_046_58()
    {
        await using var host = await Spec020Host.StartAsync();
        var id = await SeedAsync(host);

        using var response = await host.SendAsync(HttpMethod.Get, $"/api/v1/tecnicos/{id}", host.ClientSub);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().NotContain(_phone).And.NotContain("Ficticio").And.NotContain("coordinates").And.NotContain("usuarioId");
        var detail = (await Spec020Host.ReadAsync(response)).GetProperty("detalle");
        detail.GetProperty("disponibleHoy").GetBoolean().Should().BeTrue();
        detail.GetProperty("coloniaId").GetString().Should().Be(host.ColoniaId.ToString());
    }

    [Fact]
    public async Task Tecnico_Oculto_O_Inexistente_Responde_404_CA_046_18()
    {
        await using var host = await Spec020Host.StartAsync();
        var hidden = await SeedAsync(host, oculto: true);
        var suspended = await SeedAsync(host, estado: "suspendido");

        foreach (var id in new[] { hidden, suspended, ObjectId.GenerateNewId() })
        {
            using var response = await host.SendAsync(HttpMethod.Get, $"/api/v1/tecnicos/{id}", host.ClientSub);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await Spec020Host.ReadAsync(response)).GetProperty("code").GetString().Should().Be("technician_not_found");
        }
    }

    [Fact]
    public async Task Identificador_Mal_Formado_Responde_422_Sin_Repetirlo_CA_046_17()
    {
        await using var host = await Spec020Host.StartAsync();
        using var response = await host.SendAsync(HttpMethod.Get, "/api/v1/tecnicos/no-es-un-id");
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().Contain("\"id\"").And.NotContain("no-es-un-id");
    }
}
