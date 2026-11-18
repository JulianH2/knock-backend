using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Requests;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Tecnicos;

/// <summary>Rutas del propio técnico (spec 046, TEC-08 a TEC-11) sobre MongoDB real, con un técnico registrado por <c>POST /tecnicos</c>. Solo datos ficticios.</summary>
public sealed class Spec046PropioHttpTests
{
    private static readonly string[] _days = ["lunes", "martes", "miercoles", "jueves", "viernes", "sabado", "domingo"];

    private static async Task<(string Sub, string Id)> RegisterAsync(Spec020Host host)
    {
        var sub = Guid.NewGuid().ToString("D");
        using var response = await host.SendAsync(HttpMethod.Post, "/api/v1/tecnicos", sub, new Dictionary<string, object?>
        {
            ["nombre"] = "Ana Pruebas",
            ["oficios"] = new[] { Spec020Host.Plumbing },
            ["atencion"] = "domicilio",
            ["radioKm"] = 5,
            ["coloniaId"] = host.ColoniaId.ToString(),
            ["perfil"] = new Dictionary<string, object?> { ["descripcion"] = "Descripción inicial" },
            ["aceptaTerminos"] = true,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (sub, (await Spec020Host.ReadAsync(response)).GetProperty("id").GetString()!);
    }

    private static object Week(string mondayStart = "09:00", string mondayEnd = "13:00") => new
    {
        horarios = _days.Select(day => day == "lunes"
            ? new { dia = day, abierto = true, franjas = new[] { new { inicio = mondayStart, fin = mondayEnd } } }
            : new { dia = day, abierto = false, franjas = Array.Empty<object>().Select(_ => new { inicio = "", fin = "" }).ToArray() }).ToArray(),
    };

    [Fact]
    public async Task Sin_Token_Responde_401_En_Todas_Las_Rutas_Propias_CA_046_32()
    {
        await using var host = await Spec020Host.StartAsync();
        foreach (var (method, path) in new[]
        {
            (HttpMethod.Put, "/api/v1/tecnicos/me"), (HttpMethod.Put, "/api/v1/tecnicos/me/disponibilidad"),
            (HttpMethod.Get, "/api/v1/tecnicos/me/horarios"), (HttpMethod.Put, "/api/v1/tecnicos/me/horarios"),
        })
        {
            using var response = await host.SendAsync(method, path, sub: null, body: method == HttpMethod.Get ? null : new { });
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, path);
        }
    }

    [Fact]
    public async Task Encender_Disponible_Hoy_Responde_200_Y_Lo_Guarda_CA_046_36()
    {
        await using var host = await Spec020Host.StartAsync();
        var (sub, _) = await RegisterAsync(host);

        using var response = await host.SendAsync(HttpMethod.Put, "/api/v1/tecnicos/me/disponibilidad", sub, new { disponibleHoy = true });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await Spec020Host.ReadAsync(response);
        json.GetProperty("disponibleHoy").GetBoolean().Should().BeTrue();
        var stored = await host.Database.GetCollection<BsonDocument>("tecnicos").Find(Builders<BsonDocument>.Filter.Eq("usuarioId", sub)).SingleAsync();
        stored["disponibleHasta"].ToUniversalTime().Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Disponibilidad_De_Quien_No_Es_Tecnico_Responde_403_CA_046_38()
    {
        await using var host = await Spec020Host.StartAsync();
        using var response = await host.SendAsync(HttpMethod.Put, "/api/v1/tecnicos/me/disponibilidad", host.ClientSub, new { disponibleHoy = true });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Spec020Host.ReadAsync(response)).GetProperty("code").GetString().Should().Be("not_a_technician");
    }

    [Fact]
    public async Task Disponibilidad_Sin_Valor_Responde_422_CA_046_39()
    {
        await using var host = await Spec020Host.StartAsync();
        var (sub, _) = await RegisterAsync(host);
        using var response = await host.SendAsync(HttpMethod.Put, "/api/v1/tecnicos/me/disponibilidad", sub, new { disponibleHoy = (bool?)null });
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Guardar_Y_Leer_Horarios_CA_046_43()
    {
        await using var host = await Spec020Host.StartAsync();
        var (sub, _) = await RegisterAsync(host);

        using var before = await host.SendAsync(HttpMethod.Get, "/api/v1/tecnicos/me/horarios", sub);
        using var saved = await host.SendAsync(HttpMethod.Put, "/api/v1/tecnicos/me/horarios", sub, Week());
        using var after = await host.SendAsync(HttpMethod.Get, "/api/v1/tecnicos/me/horarios", sub);

        before.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Spec020Host.ReadAsync(before)).GetProperty("horarios").EnumerateArray().Should().OnlyContain(d => !d.GetProperty("abierto").GetBoolean());
        saved.StatusCode.Should().Be(HttpStatusCode.OK);
        var monday = (await Spec020Host.ReadAsync(after)).GetProperty("horarios")[0];
        monday.GetProperty("dia").GetString().Should().Be("lunes");
        monday.GetProperty("franjas")[0].GetProperty("inicio").GetString().Should().Be("09:00");
    }

    [Fact]
    public async Task Franja_Al_Reves_Responde_422_Y_Conserva_El_Horario_CA_046_63()
    {
        await using var host = await Spec020Host.StartAsync();
        var (sub, _) = await RegisterAsync(host);
        (await host.SendAsync(HttpMethod.Put, "/api/v1/tecnicos/me/horarios", sub, Week())).Dispose();

        using var rejected = await host.SendAsync(HttpMethod.Put, "/api/v1/tecnicos/me/horarios", sub, Week("18:00", "09:00"));
        using var after = await host.SendAsync(HttpMethod.Get, "/api/v1/tecnicos/me/horarios", sub);

        rejected.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await Spec020Host.ReadAsync(after)).GetProperty("horarios")[0].GetProperty("franjas")[0].GetProperty("inicio").GetString().Should().Be("09:00");
    }

    [Fact]
    public async Task Horarios_Y_Perfil_De_Quien_No_Es_Tecnico_Responden_404_CA_046_45()
    {
        await using var host = await Spec020Host.StartAsync();
        using var schedule = await host.SendAsync(HttpMethod.Get, "/api/v1/tecnicos/me/horarios", host.ClientSub);
        using var replace = await host.SendAsync(HttpMethod.Put, "/api/v1/tecnicos/me/horarios", host.ClientSub, Week());
        using var profile = await host.SendAsync(HttpMethod.Put, "/api/v1/tecnicos/me", host.ClientSub, new { descripcion = "Hola" });
        schedule.StatusCode.Should().Be(HttpStatusCode.NotFound);
        replace.StatusCode.Should().Be(HttpStatusCode.NotFound);
        profile.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Cambiar_La_Descripcion_Se_Ve_En_El_Perfil_Al_Instante_CA_046_61()
    {
        await using var host = await Spec020Host.StartAsync();
        var (sub, id) = await RegisterAsync(host);
        (await host.SendAsync(HttpMethod.Get, $"/api/v1/tecnicos/{id}", sub)).Dispose();

        using var updated = await host.SendAsync(HttpMethod.Put, "/api/v1/tecnicos/me", sub, new { descripcion = "Descripción nueva" });
        using var profile = await host.SendAsync(HttpMethod.Get, $"/api/v1/tecnicos/{id}", sub);

        updated.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await Spec020Host.ReadAsync(updated);
        json.GetProperty("descripcion").GetString().Should().Be("Descripción nueva");
        json.GetProperty("radioKm").GetInt32().Should().Be(5);
        (await Spec020Host.ReadAsync(profile)).GetProperty("detalle").GetProperty("descripcion").GetString().Should().Be("Descripción nueva");
    }

    [Fact]
    public async Task Foto_De_Otro_Sitio_Responde_422_CA_046_34()
    {
        await using var host = await Spec020Host.StartAsync();
        var (sub, _) = await RegisterAsync(host);
        using var response = await host.SendAsync(HttpMethod.Put, "/api/v1/tecnicos/me", sub, new { fotoUrl = "https://otro.sitio.test/foto.jpg" });
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadAsStringAsync()).Should().Contain("fotoUrl").And.NotContain("otro.sitio");
    }

    [Fact]
    public async Task Cuerpo_Ilegible_Responde_400_CA_046_47()
    {
        await using var host = await Spec020Host.StartAsync();
        var (sub, _) = await RegisterAsync(host);
        using var schedule = await host.SendAsync(HttpMethod.Put, "/api/v1/tecnicos/me/horarios", sub, rawJson: "{\"horarios\": [");
        using var profile = await host.SendAsync(HttpMethod.Put, "/api/v1/tecnicos/me", sub, rawJson: "{\"descripcion\": ");
        schedule.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        profile.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
