using System.Text.Json;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.IntegrationTest.Catalog;

public sealed class NeighborhoodAtPointValidationTests
{
    /// <summary>Query cruda y el campo que debe salir en <c>errors</c>.</summary>
    public static TheoryData<string, string> InvalidQueries => new()
    {
        { "lon=-99.34", "lat" },
        { "lat=20.05", "lon" },
        { "lat=&lon=-99.34", "lat" },
        { "lat=20.05&lon=", "lon" },
        { "lat=20.05&lat=99.9&lon=-99.34", "lat" },
        { "lat=20.05&lon=-99.34&lon=-99.9", "lon" },
        { "lat=91&lon=-99.34", "lat" },
        { "lat=20.05&lon=-180.5", "lon" },
        { "lat=20,05&lon=-99.34", "lat" },
        { "lat=NaN&lon=-99.34", "lat" },
        { "lat=20.05&lon=1e1", "lon" },
        { "LAT=20.05&LAT=20.06&lon=-99.34", "lat" },
    };

    [Theory]
    [MemberData(nameof(InvalidQueries))]
    public async Task InvalidOrRepeatedQuery_Returns422AndNeverCallsCatalog_TC_TT_API_07_06(string query, string field)
    {
        var catalog = new CountingNeighborhoodLocator();
        using var factory = catalog.CreateFactory();
        using var client = factory.CreateClient();

        using var response = await PointLookupHttp.GetAsync(factory, client, query);

        ((int)response.StatusCode).Should().Be(422, "nunca 400, 500 ni un 200 con el primer valor");
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("errors").TryGetProperty(field, out _).Should().BeTrue($"errors.{field}");
        catalog.Calls.Should().Be(0, "el 422 sale antes de llamar al catálogo");
    }
}
