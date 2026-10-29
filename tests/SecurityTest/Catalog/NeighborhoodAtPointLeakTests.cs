using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.SecurityTest.Catalog;

public sealed class NeighborhoodAtPointLeakTests
{
    // Coordenadas SINTÉTICAS y reconocibles; también se buscan redondeadas a 4 decimales.
    private const string _lat = "20.0512347";
    private const string _lon = "-99.3456781";
    private static readonly string[] _forms = ["20.0512347", "99.3456781", "20.0512", "99.3457"];

    public static TheoryData<string, string> MalformedOrRepeated => new()
    {
        { "lat=20,05x&lon=-99.34", "20,05x" },
        { "lat=abc&lon=-99.34", "abc" },
        { "lat=20.05&lat=99.9&lon=-99.34", "99.9" },
    };

    [Fact]
    public async Task Responses_NeverEchoSubmittedCoordinates_TC_TT_API_07_07()
    {
        var catalog = new CountingNeighborhoodLocator();
        using var factory = catalog.CreateFactory(settings: TestHttp.Limits(globalPermits: 2, authPermits: 1000, window: "00:01:00"));
        using var client = factory.CreateClient();

        using var ok = await PointLookupHttp.GetAsync(factory, client, PointLookupHttp.Query(_lat, _lon), "203.0.113.71");
        using var invalid = await PointLookupHttp.GetAsync(factory, client, PointLookupHttp.Query(_lat, "-199.3456781"), "203.0.113.71");
        using var limited = await PointLookupHttp.GetAsync(factory, client, PointLookupHttp.Query(_lat, _lon), "203.0.113.71");

        new[] { ok.StatusCode, invalid.StatusCode, limited.StatusCode }
            .Should().Equal(HttpStatusCode.OK, (HttpStatusCode)422, HttpStatusCode.TooManyRequests);
        foreach (var response in new[] { ok, invalid, limited })
        {
            var exposed = await response.Content.ReadAsStringAsync()
                + string.Concat(response.Headers.Concat(response.Content.Headers).Select(h => $"{h.Key}:{string.Join(',', h.Value)}\n"));
            foreach (var form in _forms)
            {
                exposed.Should().NotContain(form, $"la respuesta {(int)response.StatusCode} no repite las coordenadas");
            }
        }
    }

    [Theory]
    [MemberData(nameof(MalformedOrRepeated))]
    public async Task MalformedOrRepeated_Returns422WithoutEchoNotAutomatic400_TC_TT_API_07_15(string query, string sent)
    {
        var catalog = new CountingNeighborhoodLocator();
        using var factory = catalog.CreateFactory();
        using var client = factory.CreateClient();

        using var response = await PointLookupHttp.GetAsync(factory, client, query);
        var body = await response.Content.ReadAsStringAsync();

        ((int)response.StatusCode).Should().Be(422, "nunca el 400 automático de [ApiController]");
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("errors").EnumerateObject().Select(e => e.Name).Should().Equal("lat");
        body.Should().NotContain(sent).And.NotContain("The value '", "ni el mensaje del enlace numérico que repite el valor");
        body.Should().NotContain("regular expression").And.NotContain("maximum length", "ni los mensajes de DataAnnotations");
        catalog.Calls.Should().Be(0);
    }
}
