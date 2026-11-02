using System.Globalization;
using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Knock.Api.Modules.Catalog.Services;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Catalog;

public sealed class NeighborhoodAtPointEndpointTests
{
    private const int _repetitions = 20;

    [Fact]
    public async Task PointInsidePilotNeighborhood_Returns200WithItsNeighborhood_TC_TT_API_07_03()
    {
        var (factory, databaseName) = await PointLookupHttp.CreateSeededFactoryAsync((await DocumentDbFixture.SharedAsync()).ConnectionString);
        using var _ = factory;
        using var client = factory.CreateClient();
        var database = factory.Services.GetRequiredService<IMongoClient>().GetDatabase(databaseName);

        foreach (var (colonia, lat, lon) in PointLookupHttp.PilotInteriorPoints)
        {
            using var response = await PointLookupHttp.GetAsync(factory, client, PointLookupHttp.Query(lat, lon));
            var body = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.OK, colonia);
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
            var expected = await PointLookupHttp.ReadColoniaAsync(database, colonia);
            using var json = JsonDocument.Parse(body);
            json.RootElement.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("enCobertura", "colonia");
            json.RootElement.GetProperty("enCobertura").GetBoolean().Should().BeTrue();
            var found = json.RootElement.GetProperty("colonia");
            found.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(
                ["id", "nombre", "cp", "municipio"], "sin geom, centro, nombreBusqueda ni version");
            (found.GetProperty("id").GetString(), found.GetProperty("nombre").GetString(), found.GetProperty("cp").GetString(), found.GetProperty("municipio").GetString())
                .Should().Be(expected);
            found.GetProperty("id").GetString().Should().MatchRegex("^[0-9a-f]{24}$");
            body.Should().NotContain(lat).And.NotContain(lon.TrimStart('-'), "la respuesta no repite las coordenadas");
        }

        await factory.Services.GetRequiredService<IMongoClient>().DropDatabaseAsync(databaseName);
    }

    [Fact]
    public async Task PointOutOfCoverage_Returns200WithNullNeighborhood_TC_TT_API_07_04()
    {
        var (factory, databaseName) = await PointLookupHttp.CreateSeededFactoryAsync((await DocumentDbFixture.SharedAsync()).ConnectionString);
        using var _ = factory;
        using var client = factory.CreateClient();

        foreach (var (lat, lon) in PointLookupHttp.OutOfCoverage)
        {
            using var response = await PointLookupHttp.GetAsync(factory, client, PointLookupHttp.Query(lat, lon));

            response.StatusCode.Should().Be(HttpStatusCode.OK, "fuera de cobertura es una respuesta válida, no un 404");
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            json.RootElement.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("enCobertura", "colonia");
            json.RootElement.GetProperty("enCobertura").GetBoolean().Should().BeFalse();
            json.RootElement.GetProperty("colonia").ValueKind.Should().Be(JsonValueKind.Null);
        }

        await factory.Services.GetRequiredService<IMongoClient>().DropDatabaseAsync(databaseName);
    }

    [Fact]
    public async Task BoundaryAndOverlap_ReturnTheSameNeighborhoodAsTheInternalLookup_TC_TT_API_07_05()
    {
        var (factory, databaseName) = await PointLookupHttp.CreateSeededFactoryAsync((await DocumentDbFixture.SharedAsync()).ConnectionString);
        using var _ = factory;
        using var client = factory.CreateClient();
        var locator = factory.Services.GetRequiredService<NeighborhoodLocator>();

        foreach (var (lat, lon) in PointLookupHttp.BoundaryAndOverlapPoints)
        {
            var expected = await locator.FindAtAsync(double.Parse(lat, CultureInfo.InvariantCulture), double.Parse(lon, CultureInfo.InvariantCulture), CancellationToken.None);
            expected.Should().NotBeNull("los puntos de borde y traslape están dentro de algún polígono");

            for (var i = 0; i < _repetitions; i++)
            {
                using var response = await PointLookupHttp.GetAsync(factory, client, PointLookupHttp.Query(lat, lon));
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var found = json.RootElement.GetProperty("colonia");
                (found.GetProperty("id").GetString(), found.GetProperty("nombre").GetString(), found.GetProperty("cp").GetString(), found.GetProperty("municipio").GetString())
                    .Should().Be((expected.Id, expected.Nombre, expected.Cp, expected.Municipio), $"repetición {i + 1}: el endpoint no altera el desempate");
            }
        }

        await factory.Services.GetRequiredService<IMongoClient>().DropDatabaseAsync(databaseName);
    }
}
