using System.Net;
using AwesomeAssertions;
using MongoDB.Bson;
using Xunit;

namespace Knock.IntegrationTest.Requests;

/// <summary>Spec 021, T-05: el listado de cotizaciones del cliente dueño (MongoDB real).</summary>
public sealed class Spec021ListadoTests
{
    [Fact]
    public async Task Lista_Cotizaciones_CA_021_01()
    {
        await using var host = await Spec020Host.StartAsync();
        var (requestId, _, _) = await host.QuotedRequestAsync(0);
        var verified = await host.SeedPublicTechnicianAsync("basica", 4.5);
        await host.SeedQuoteAsync(requestId, verified, "fijo", 900);
        await host.SeedQuoteAsync(requestId, await host.SeedPublicTechnicianAsync(), "rango", price: null, min: 700, max: 1200);
        await host.SeedQuoteAsync(requestId, await host.SeedPublicTechnicianAsync(), "visita", 250);

        using var response = await host.CompareAsync(requestId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        response.Headers.Contains("ETag").Should().BeFalse();
        var json = await Spec020Host.ReadAsync(response);
        json.GetProperty("totalCotizaciones").GetInt32().Should().Be(3);
        json.GetProperty("cotizaciones").GetArrayLength().Should().Be(3);
        json.GetProperty("puedeElegir").GetBoolean().Should().BeTrue();
        json.GetProperty("tiposMixtos").GetBoolean().Should().BeTrue();
        foreach (var quote in json.GetProperty("cotizaciones").EnumerateArray())
        {
            quote.GetProperty("cubre").GetString().Should().NotBeNullOrWhiteSpace();
            quote.GetProperty("etiquetas").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Array);
        }

        var first = json.GetProperty("cotizaciones")[0];
        first.GetProperty("tipoPrecio").GetString().Should().Be("fijo", "el técnico verificado con calificación va primero en «recomendadas»");
        first.GetProperty("tecnico").GetProperty("verificado").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Vencida_Sin_Eleccion_CA_021_11()
    {
        await using var host = await Spec020Host.StartAsync();
        var (requestId, _, _) = await host.QuotedRequestAsync(2);
        await host.SetRequestAsync(requestId, "cotizando", DateTime.UtcNow.AddMinutes(-1));

        using var response = await host.CompareAsync(requestId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await Spec020Host.ReadAsync(response);
        json.GetProperty("puedeElegir").GetBoolean().Should().BeFalse();
        json.GetProperty("motivo").GetString().Should().Be("request_expired");
    }

    [Fact]
    public async Task Sin_Cotizaciones_Y_Ya_Elegida_Dan_Su_Motivo_R_08()
    {
        await using var host = await Spec020Host.StartAsync();
        var empty = await host.CreateRequestAsync();
        var (chosenRequest, quoteIds, _) = await host.QuotedRequestAsync(2);
        (await host.ChooseAsync(chosenRequest, quoteIds[0])).Dispose();

        using var withoutQuotes = await host.CompareAsync(empty);
        using var chosen = await host.CompareAsync(chosenRequest);

        (await Spec020Host.ReadAsync(withoutQuotes)).GetProperty("motivo").GetString().Should().Be("request_without_quotes");
        var json = await Spec020Host.ReadAsync(chosen);
        json.GetProperty("puedeElegir").GetBoolean().Should().BeFalse();
        json.GetProperty("motivo").GetString().Should().Be("request_already_chosen");
        json.GetProperty("visitaId").GetString().Should().NotBeNullOrEmpty();
        var states = json.GetProperty("cotizaciones").EnumerateArray().Select(q => q.GetProperty("estado").GetString()).ToList();
        states.Should().BeEquivalentTo("aceptada", "rechazada");
        json.GetProperty("cotizaciones").EnumerateArray().Single(q => q.GetProperty("estado").GetString() == "rechazada").GetProperty("motivoRechazo").GetString().Should().Be("other_quote_chosen");
    }

    [Fact]
    public async Task Incluye_VisitaId_Solo_Elegida_CA_021_19()
    {
        await using var host = await Spec020Host.StartAsync();
        var (chosenRequest, quoteIds, _) = await host.QuotedRequestAsync(2);
        var (otherRequest, _, _) = await host.QuotedRequestAsync(1);
        using var chosen = await host.ChooseAsync(chosenRequest, quoteIds[0]);
        var visitId = (await Spec020Host.ReadAsync(chosen)).GetProperty("visitaId").GetString();

        using var list = await host.SendAsync(HttpMethod.Get, "/api/v1/solicitudes", host.ClientSub);
        using var stranger = await host.SendAsync(HttpMethod.Get, "/api/v1/solicitudes", Guid.NewGuid().ToString("D"));

        var items = (await Spec020Host.ReadAsync(list)).GetProperty("items").EnumerateArray().ToDictionary(i => i.GetProperty("id").GetString()!);
        items[chosenRequest.ToString()].GetProperty("visitaId").GetString().Should().Be(visitId);
        items[otherRequest.ToString()].TryGetProperty("visitaId", out var missing).Should().BeFalse("solo la que ya eligió trae visitaId");
        missing.ValueKind.Should().Be(System.Text.Json.JsonValueKind.Undefined);
        (await stranger.Content.ReadAsStringAsync()).Should().NotContain(visitId!, "la lista de otra persona nunca trae ese identificador");
    }

    [Fact]
    public async Task Devuelve_Cinco_Y_Total_En_El_Limite_R_03()
    {
        await using var host = await Spec020Host.StartAsync();
        var (requestId, _, _) = await host.QuotedRequestAsync(5);

        using var response = await host.CompareAsync(requestId);

        var json = await Spec020Host.ReadAsync(response);
        json.GetProperty("cotizaciones").GetArrayLength().Should().Be(5);
        json.GetProperty("totalCotizaciones").GetInt32().Should().Be(5);
        (await host.CountAsync("cotizaciones", MongoDB.Driver.Builders<BsonDocument>.Filter.Eq("solicitudId", requestId))).Should().Be(5);
    }
}
