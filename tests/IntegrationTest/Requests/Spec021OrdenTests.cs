using System.Net;
using AwesomeAssertions;
using Xunit;

namespace Knock.IntegrationTest.Requests;

/// <summary>Spec 021, T-02 y T-05: orden y etiquetas del listado, y validación de <c>orden</c> y del id (MongoDB real).</summary>
public sealed class Spec021OrdenTests
{
    [Fact]
    public async Task Orden_Por_Precio_CA_021_02()
    {
        await using var host = await Spec020Host.StartAsync();
        var (requestId, _, _) = await host.QuotedRequestAsync(0);
        var visit = await host.SeedQuoteAsync(requestId, await host.SeedPublicTechnicianAsync(), "visita", 250);
        var second = await host.SeedQuoteAsync(requestId, await host.SeedPublicTechnicianAsync(), "fijo", 1100);
        var first = await host.SeedQuoteAsync(requestId, await host.SeedPublicTechnicianAsync(), "fijo", 900);

        using var response = await host.CompareAsync(requestId, "precio");

        var items = (await Spec020Host.ReadAsync(response)).GetProperty("cotizaciones").EnumerateArray().ToList();
        items.Select(i => i.GetProperty("id").GetString()).Should().Equal(first.ToString(), second.ToString(), visit.ToString());
        items[0].GetProperty("etiquetas").EnumerateArray().Select(t => t.GetString()).Should().Contain("mejor_precio");
        items[2].GetProperty("etiquetas").EnumerateArray().Select(t => t.GetString()).Should().NotContain("mejor_precio", "una visita nunca es la mejor");
    }

    [Fact]
    public async Task Recomendadas_Estable_CA_021_14()
    {
        await using var host = await Spec020Host.StartAsync();
        var (requestId, _, _) = await host.QuotedRequestAsync(0);
        var plainFirst = await host.SeedQuoteAsync(requestId, await host.SeedPublicTechnicianAsync(), "fijo", 900);
        var plainSecond = await host.SeedQuoteAsync(requestId, await host.SeedPublicTechnicianAsync(), "fijo", 900);
        var verified = await host.SeedQuoteAsync(requestId, await host.SeedPublicTechnicianAsync("basica"), "fijo", 900);

        var orders = new List<string?[]>();
        for (var i = 0; i < 3; i++)
        {
            using var response = await host.CompareAsync(requestId);
            orders.Add([.. (await Spec020Host.ReadAsync(response)).GetProperty("cotizaciones").EnumerateArray().Select(q => q.GetProperty("id").GetString())]);
        }

        orders.Should().AllSatisfy(order => order.Should().Equal(verified.ToString(), plainFirst.ToString(), plainSecond.ToString()));
    }

    [Theory]
    [InlineData("calificacion")]
    [InlineData("pronto")]
    [InlineData("recomendadas")]
    public async Task Acepta_Los_Cuatro_Ordenes_R_04(string order)
    {
        await using var host = await Spec020Host.StartAsync();
        var (requestId, _, _) = await host.QuotedRequestAsync(2);

        using var response = await host.CompareAsync(requestId, order);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Orden_E_Id_Invalidos_CA_021_15()
    {
        await using var host = await Spec020Host.StartAsync();
        var (requestId, _, _) = await host.QuotedRequestAsync(1);

        using var badOrder = await host.CompareAsync(requestId, "barato");
        using var badId = await host.SendAsync(HttpMethod.Get, "/api/v1/solicitudes/no-es-id/cotizaciones", host.ClientSub);

        badOrder.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await Spec020Host.ReadAsync(badOrder)).GetProperty("errors").TryGetProperty("orden", out _).Should().BeTrue();
        (await badOrder.Content.ReadAsStringAsync()).Should().NotContain("barato", "el motivo nunca repite el valor enviado");
        badId.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
