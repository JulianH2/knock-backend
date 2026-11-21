using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Knock.IntegrationTest.Requests;
using Xunit;

namespace Knock.IntegrationTest.Tecnicos;

/// <summary><c>GET /api/v1/tecnicos</c> (spec 046, TEC-02) sobre MongoDB real: técnicos activos y visibles, filtros, orden y cursor.</summary>
public sealed class Spec046ListaTecnicosHttpTests
{
    private const string _route = "/api/v1/tecnicos";

    [Fact]
    public async Task Listado_Valido_Responde_200_Solo_Activos_Visibles_CA_046_06()
    {
        await using var host = await Spec020Host.StartAsync();
        await host.SeedTechnicianAsync(Guid.NewGuid().ToString("D"), "activo", false, [Spec020Host.Plumbing], host.ColoniaId);
        await host.SeedTechnicianAsync(Guid.NewGuid().ToString("D"), "suspendido", false, [Spec020Host.Plumbing], host.ColoniaId);
        await host.SeedTechnicianAsync(Guid.NewGuid().ToString("D"), "activo", true, [Spec020Host.Plumbing], host.ColoniaId);

        using var response = await host.SendAsync(HttpMethod.Get, _route);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await Spec020Host.ReadAsync(response);
        var tecnicos = json.GetProperty("tecnicos");
        tecnicos.GetArrayLength().Should().Be(1);
        tecnicos[0].GetProperty("id").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Valor_Invalido_Responde_422_Sin_Repetirlo_CA_046_07()
    {
        await using var host = await Spec020Host.StartAsync();
        using var radio = await host.SendAsync(HttpMethod.Get, $"{_route}?radioKm=7");
        radio.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await radio.Content.ReadAsStringAsync()).Should().Contain("radioKm");

        using var cursor = await host.SendAsync(HttpMethod.Get, $"{_route}?cursor=no-es-id");
        cursor.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await cursor.Content.ReadAsStringAsync()).Should().Contain("cursor");

        using var limite = await host.SendAsync(HttpMethod.Get, $"{_route}?limite=51");
        limite.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await limite.Content.ReadAsStringAsync()).Should().Contain("limite");
    }

    [Fact]
    public async Task Paginación_Por_Cursor_Con_Limite_Maximo_CA_046_08()
    {
        await using var host = await Spec020Host.StartAsync();
        foreach (var _ in Enumerable.Range(0, 3))
        {
            await host.SeedTechnicianAsync(Guid.NewGuid().ToString("D"), "activo", false, [Spec020Host.Plumbing], host.ColoniaId);
        }

        using var first = await host.SendAsync(HttpMethod.Get, $"{_route}?limite=2");
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await Spec020Host.ReadAsync(first);
        json.GetProperty("tecnicos").GetArrayLength().Should().Be(2);
        var cursor = json.GetProperty("nextCursor").GetString();
        cursor.Should().NotBeNullOrEmpty();

        using var second = await host.SendAsync(HttpMethod.Get, $"{_route}?limite=2&cursor={Uri.EscapeDataString(cursor)}");
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var next = await Spec020Host.ReadAsync(second);
        next.GetProperty("tecnicos").GetArrayLength().Should().Be(1);
        next.GetProperty("nextCursor").ValueKind.Should().Be(JsonValueKind.Null);
    }
}
