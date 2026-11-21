using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Requests;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Tecnicos;

/// <summary>
/// HU-005 (KNOCK-53), exploración sin cuenta en Mercado sobre MongoDB real: lo público es solo lectura y nunca trae datos privados (TC-HU-005-01 y TC-HU-005-05).
/// Solo datos ficticios.
/// </summary>
public sealed class Hu005ExploracionPublicaTests
{
    private const string _phone = "5512345678";
    private const string _workshop = "Taller Ficticio 45, Colonia Piloto";
    private static readonly string[] _privateValues = [_phone, _workshop, "coordinates", "usuarioId", "tallerDireccion", "contactoPublico", "whatsapp", "radioAtencionKm"];

    private static async Task<ObjectId> SeedAsync(Spec020Host host, string atencion)
    {
        var id = ObjectId.GenerateNewId();
        await host.Database.GetCollection<BsonDocument>("tecnicos").InsertOneAsync(new BsonDocument
        {
            ["_id"] = id,
            ["usuarioId"] = Guid.NewGuid().ToString("D"),
            ["nombre"] = "Ana Pruebas",
            ["nombreNegocio"] = "Plomería Ana",
            ["estado"] = "activo",
            ["oculto"] = false,
            ["oficios"] = new BsonArray { Spec020Host.Plumbing },
            ["atencion"] = atencion,
            ["coloniaBaseId"] = host.ColoniaId,
            ["radioAtencionKm"] = 5,
            ["nivelVerificacion"] = "basica",
            ["descripcion"] = "Arreglo fugas y calentadores",
            ["anios"] = 7,
            ["tallerDireccion"] = _workshop,
            ["contactoPublico"] = new BsonDocument { ["telefono"] = _phone, ["whatsapp"] = _phone },
            ["ubicacionBase"] = new BsonDocument { ["type"] = "Point", ["coordinates"] = new BsonArray { -99.34, 20.05 } },
            ["palabrasBusqueda"] = new BsonArray { "ana", "pruebas", "plomeria" },
            ["disponibleHasta"] = DateTime.UtcNow.AddHours(2),
        });
        return id;
    }

    [Fact]
    public async Task Sin_Cuenta_El_Perfil_Y_El_Autocompletar_Responden_200_Sin_Pedir_Sesion_TC_HU_005_01()
    {
        await using var host = await Spec020Host.StartAsync();
        var id = await SeedAsync(host, "ambos");

        using var profile = await host.SendAsync(HttpMethod.Get, $"/api/v1/tecnicos/{id}");
        using var suggestions = await host.SendAsync(HttpMethod.Get, "/api/v1/tecnicos/autocompletar?q=plomer");

        profile.StatusCode.Should().Be(HttpStatusCode.OK);
        suggestions.StatusCode.Should().Be(HttpStatusCode.OK);
        profile.Headers.CacheControl?.NoStore.Should().BeTrue("el perfil público no se guarda en cachés compartidas");
    }

    [Theory]
    [InlineData("domicilio")]
    [InlineData("taller")]
    [InlineData("ambos")]
    public async Task Sin_Cuenta_El_Perfil_No_Trae_Telefono_Direccion_Ni_Datos_Privados_TC_HU_005_05(string atencion)
    {
        await using var host = await Spec020Host.StartAsync();
        var id = await SeedAsync(host, atencion);

        using var response = await host.SendAsync(HttpMethod.Get, $"/api/v1/tecnicos/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var raw = await response.Content.ReadAsStringAsync();
        foreach (var value in _privateValues)
        {
            raw.Should().NotContain(value);
        }
    }

    [Fact]
    public async Task Sin_Cuenta_El_Autocompletar_No_Trae_Telefono_Direccion_Ni_Datos_Privados_TC_HU_005_05()
    {
        await using var host = await Spec020Host.StartAsync();
        await SeedAsync(host, "domicilio");

        using var response = await host.SendAsync(HttpMethod.Get, "/api/v1/tecnicos/autocompletar?q=plomer");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var raw = await response.Content.ReadAsStringAsync();
        foreach (var value in _privateValues)
        {
            raw.Should().NotContain(value);
        }
    }

    [Fact]
    public async Task Sin_Cuenta_Las_Acciones_Que_Comprometen_Datos_Piden_Sesion_TC_HU_005_02()
    {
        await using var host = await Spec020Host.StartAsync();
        var id = await SeedAsync(host, "ambos");

        using var quotes = await host.SendAsync(HttpMethod.Get, $"/api/v1/tecnicos/{id}/llega-a-mi-colonia?colonia={host.ColoniaId}");
        using var request = await host.SendAsync(HttpMethod.Post, "/api/v1/solicitudes");

        quotes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        request.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
