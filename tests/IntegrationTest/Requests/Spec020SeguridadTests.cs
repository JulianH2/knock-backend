using System.Net;
using System.Text;
using AwesomeAssertions;
using Knock.Contracts.Moderation;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using Xunit;

namespace Knock.IntegrationTest.Requests;

/// <summary>Spec 020: sesión obligatoria, límite de cuerpo, moderación y fotos que fallan cerrado, límite por persona y registros sin datos personales.</summary>
public sealed class Spec020SeguridadTests
{
    [Fact]
    public async Task Exige_Sesion_CA_020_16()
    {
        await using var host = await Spec020Host.StartAsync();
        var id = ObjectId.GenerateNewId().ToString();

        using var create = await host.SendAsync(HttpMethod.Post, "/api/v1/solicitudes", sub: null, body: host.ValidBody());
        using var list = await host.SendAsync(HttpMethod.Get, "/api/v1/solicitudes");
        using var detail = await host.SendAsync(HttpMethod.Get, "/api/v1/solicitudes/" + id);

        create.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        list.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        detail.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await host.CountRequestsAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Cuerpo_Demasiado_Grande_CA_020_22()
    {
        await using var host = await Spec020Host.StartAsync();
        var body = host.ValidBody();
        body["descripcion"] = new string('a', 9 * 1024);

        using var response = await host.SendAsync(HttpMethod.Post, "/api/v1/solicitudes", host.ClientSub, rawJson: System.Text.Json.JsonSerializer.Serialize(body));

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        (await host.CountRequestsAsync()).Should().Be(0, "no llegó al servicio");
    }

    [Fact]
    public async Task Falla_Cerrado_CA_020_15()
    {
        await using var host = await Spec020Host.StartAsync();

        host.Moderator.Outcome = ModerationOutcome.Unavailable;
        using var down = await host.CreateAsync(host.ValidBody());
        host.Moderator.Outcome = ModerationOutcome.Rejected;
        using var flagged = await host.CreateAsync(host.ValidBody());

        down.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        (await Spec020Host.ReadAsync(down)).GetProperty("code").GetString().Should().Be("moderation_unavailable");
        flagged.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await Spec020Host.ReadAsync(flagged)).GetProperty("code").GetString().Should().Be("content_rejected");
        (await host.CountRequestsAsync()).Should().Be(0, "sin moderación no se guarda nada");
    }

    [Fact]
    public async Task Rechaza_Foto_No_Valida_CA_020_18()
    {
        await using var host = await Spec020Host.StartAsync();
        host.Photos.Ready = false;
        var body = host.ValidBody();
        var photo = ((string[])body["fotos"]!)[0];

        using var response = await host.CreateAsync(body);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var raw = await response.Content.ReadAsStringAsync();
        System.Text.Json.JsonElement.Parse(raw).GetProperty("code").GetString().Should().Be("photo_invalid");
        raw.Should().NotContain(photo, "no dice cuál foto ni cuál condición falló");
        (await host.CountRequestsAsync()).Should().Be(0);
    }

    [Fact]
    public async Task No_Registra_Datos_Personales_CA_020_17()
    {
        await using var host = await Spec020Host.StartAsync();
        var body = host.ValidBody();
        var photo = ((string[])body["fotos"]!)[0];
        string? id = null;

        var output = await ConsoleCapture.RunAsync(async () =>
        {
            using var created = await host.CreateAsync(body);
            id = (await Spec020Host.ReadAsync(created)).GetProperty("id").GetString();
            using var list = await host.SendAsync(HttpMethod.Get, "/api/v1/solicitudes", host.ClientSub);
            using var detail = await host.SendAsync(HttpMethod.Get, "/api/v1/solicitudes/" + id, host.ClientSub);
        });

        id.Should().NotBeNull();
        output.Should().NotContain("fregadero", "la descripción no se registra")
            .And.NotContain("Calle Ficticia", "la dirección no se registra")
            .And.NotContain(photo, "los identificadores de fotos no se registran")
            .And.NotContain("sig=ficticia", "los enlaces firmados no se registran");
    }

    [Fact]
    public async Task Onceava_Solicitud_En_Una_Hora_Recibe_429_R_Limite()
    {
        await using var host = await Spec020Host.StartAsync();
        for (var i = 0; i < 10; i++)
        {
            using var ok = await host.CreateAsync(host.ValidBody());
            ok.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        using var limited = await host.CreateAsync(host.ValidBody());
        using var otherPerson = await host.CreateAsync(host.ValidBody(), sub: Guid.NewGuid().ToString("D"));

        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        limited.Headers.Contains("Retry-After").Should().BeTrue();
        otherPerson.StatusCode.Should().Be(HttpStatusCode.Created, "el límite es por persona");
        (await host.CountRequestsAsync()).Should().Be(11);
    }

    [Fact]
    public async Task Sin_Llave_De_Cifrado_La_Direccion_No_Se_Guarda_En_Claro_R_06()
    {
        await using var host = await Spec020Host.StartAsync(new Dictionary<string, string?> { ["Security:FieldEncryption:ActiveVersion"] = string.Empty });

        using var response = await host.CreateAsync(host.ValidBody());

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError, "falla cerrado en lugar de guardar la dirección sin cifrar");
        (await host.CountRequestsAsync()).Should().Be(0);
        Encoding.UTF8.GetString(await response.Content.ReadAsByteArrayAsync()).Should().NotContain("Calle Ficticia");
    }
}
