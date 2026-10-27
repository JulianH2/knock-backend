using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Knock.IntegrationTest.Data;
using Knock.IntegrationTest.Support;
using Npgsql;
using Xunit;

namespace Knock.IntegrationTest.Catalog;

/// <summary>
/// TT-PRU-01: <c>GET /api/v1/oficios</c> contra PostGIS real (un contenedor por prueba) con migraciones y la semilla versionada.
/// Las pruebas verifican el orden resultante, no el SQL (spec §8 D4).
/// </summary>
public sealed class OficiosEndpointTests
{
    private const string _path = "/api/v1/oficios";

    [Fact]
    public async Task GetOficios_SeededCatalog_Returns200WithThreeTradesOrderedByName_TC_TT_PRU_01_01()
    {
        await using var postgis = await PostgisFixture.StartAsync();
        using var factory = await StartApiAsync(postgis);
        using var client = factory.CreateClient();

        using var response = await TestHttp.GetAsync(client, _path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        (await ReadTradesAsync(response)).Should().Equal(
            new Trade("ele", "Electricidad", "domicilio"),
            new Trade("mec", "Mecánica", "taller"),
            new Trade("plo", "Plomería", "domicilio"));
    }

    [Fact]
    public async Task GetOficios_InactiveTrade_IsExcluded_TC_TT_PRU_01_02()
    {
        await using var postgis = await PostgisFixture.StartAsync();
        using var factory = await StartApiAsync(postgis);
        await InsertTradeAsync(postgis, new Trade("alb", "Albañilería", "domicilio"), active: false);
        using var client = factory.CreateClient();

        using var response = await TestHttp.GetAsync(client, _path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadTradesAsync(response)).Select(t => t.Clave).Should().Equal("ele", "mec", "plo");
    }

    [Fact]
    public async Task GetOficios_OrderIsByNameNotByKeyOrInsertion_TC_TT_PRU_01_03()
    {
        await using var postgis = await PostgisFixture.StartAsync();
        using var factory = await StartApiAsync(postgis);
        // Insertado al final y con la clave mayor: por clave o por inserción saldría último; por nombre, primero.
        await InsertTradeAsync(postgis, new Trade("zap", "Carpintería", "taller"), active: true);
        // En minúsculas: con orden sensible a mayúsculas saldría después de «Plomería».
        await InsertTradeAsync(postgis, new Trade("her", "herrería", "taller"), active: true);
        // Mismo nombre salvo mayúsculas: empata con «Electricidad» y desempata por clave (ele antes que zzz).
        await InsertTradeAsync(postgis, new Trade("zzz", "ELECTRICIDAD", "domicilio"), active: true);
        using var client = factory.CreateClient();

        using var response = await TestHttp.GetAsync(client, _path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadTradesAsync(response)).Select(t => $"{t.Nombre}/{t.Clave}").Should().Equal(
            "Carpintería/zap", "Electricidad/ele", "ELECTRICIDAD/zzz", "herrería/her", "Mecánica/mec", "Plomería/plo");
    }

    [Fact]
    public async Task GetOficios_Response_ExposesOnlyClaveNombreModalidad_TC_TT_PRU_01_04()
    {
        await using var postgis = await PostgisFixture.StartAsync();
        using var factory = await StartApiAsync(postgis);
        using var client = factory.CreateClient();

        using var response = await TestHttp.GetAsync(client, _path);
        using var body = await TestHttp.ReadJsonAsync(response);

        body.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        body.RootElement.GetArrayLength().Should().Be(3);
        foreach (var trade in body.RootElement.EnumerateArray())
        {
            trade.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("clave", "nombre", "modalidad");
            trade.EnumerateObject().Should().OnlyContain(p => p.Value.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(p.Value.GetString()));
        }
    }

    [Fact]
    public async Task GetOficios_WithoutCredentials_Returns200_TC_TT_PRU_01_05()
    {
        await using var postgis = await PostgisFixture.StartAsync();
        using var factory = await StartApiAsync(postgis);
        using var client = factory.CreateClient();

        using var response = await TestHttp.GetAsync(client, _path);

        response.RequestMessage!.Headers.Authorization.Should().BeNull("la petición va sin credenciales");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadTradesAsync(response)).Should().HaveCount(3);
    }

    private static async Task<KnockApiFactory> StartApiAsync(PostgisFixture postgis)
    {
        var factory = new KnockApiFactory(connectionString: postgis.ConnectionString);
        await CatalogTestDatabase.MigrateAndSeedAsync(factory.Services);
        return factory;
    }

    /// <summary>Oficio sintético insertado solo en el contenedor de la prueba (nunca en la semilla versionada).</summary>
    private static async Task InsertTradeAsync(PostgisFixture postgis, Trade trade, bool active)
    {
        await using var connection = new NpgsqlConnection(postgis.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "INSERT INTO catalog.oficios (clave, nombre, modalidad, activo) VALUES (@clave, @nombre, @modalidad, @activo)",
            connection);
        command.Parameters.AddWithValue("clave", trade.Clave);
        command.Parameters.AddWithValue("nombre", trade.Nombre);
        command.Parameters.AddWithValue("modalidad", trade.Modalidad);
        command.Parameters.AddWithValue("activo", active);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<IReadOnlyList<Trade>> ReadTradesAsync(HttpResponseMessage response)
    {
        using var body = await TestHttp.ReadJsonAsync(response);
        return [.. body.RootElement.EnumerateArray().Select(t => new Trade(
            t.GetProperty("clave").GetString()!,
            t.GetProperty("nombre").GetString()!,
            t.GetProperty("modalidad").GetString()!))];
    }

    private sealed record Trade(string Clave, string Nombre, string Modalidad);
}
