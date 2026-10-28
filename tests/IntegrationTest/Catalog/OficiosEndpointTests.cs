using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using AwesomeAssertions;
using Knock.Api.Modules.Catalog.Data;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Catalog;

/// <summary>
/// TT-PRU-01: <c>GET /api/v1/oficios</c> contra documentdb-local real (un contenedor por ejecución y una base por prueba) con la
/// semilla versionada. Las pruebas verifican el orden resultante, no la consulta (spec §8 D4).
/// </summary>
public sealed class OficiosEndpointTests
{
    private const string _path = "/api/v1/oficios";

    [Fact]
    public async Task GetOficios_SeededCatalog_Returns200WithThreeTradesOrderedByName_TC_TT_PRU_01_01()
    {
        await using var host = await StartApiAsync();
        using var client = host.Factory.CreateClient();

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
        await using var host = await StartApiAsync();
        await host.AddTradeAsync(new Trade("alb", "Albañilería", "domicilio"), active: false);
        using var client = host.Factory.CreateClient();

        using var response = await TestHttp.GetAsync(client, _path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadTradesAsync(response)).Select(t => t.Clave).Should().Equal("ele", "mec", "plo");
    }

    [Fact]
    public async Task GetOficios_OrderIsByNameNotByKeyOrInsertion_TC_TT_PRU_01_03()
    {
        await using var host = await StartApiAsync();
        // Insertado al final y con la clave mayor: por clave o por inserción saldría último; por nombre, primero.
        await host.AddTradeAsync(new Trade("zap", "Carpintería", "taller"), active: true);
        // En minúsculas: con orden sensible a mayúsculas saldría después de «Plomería».
        await host.AddTradeAsync(new Trade("her", "herrería", "taller"), active: true);
        // Mismo nombre salvo mayúsculas: empata con «Electricidad» y desempata por clave (ele antes que zzz).
        await host.AddTradeAsync(new Trade("zzz", "ELECTRICIDAD", "domicilio"), active: true);
        using var client = host.Factory.CreateClient();

        using var response = await TestHttp.GetAsync(client, _path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadTradesAsync(response)).Select(t => $"{t.Nombre}/{t.Clave}").Should().Equal(
            "Carpintería/zap", "Electricidad/ele", "ELECTRICIDAD/zzz", "herrería/her", "Mecánica/mec", "Plomería/plo");
    }

    [Fact]
    public async Task GetOficios_Response_ExposesOnlyClaveNombreModalidad_TC_TT_PRU_01_04()
    {
        await using var host = await StartApiAsync();
        using var client = host.Factory.CreateClient();

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
        await using var host = await StartApiAsync();
        using var client = host.Factory.CreateClient();

        using var response = await TestHttp.GetAsync(client, _path);

        response.RequestMessage!.Headers.Authorization.Should().BeNull("la petición va sin credenciales");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadTradesAsync(response)).Should().HaveCount(3);
    }

    private static async Task<ApiHost> StartApiAsync()
    {
        var mongo = await DocumentDbFixture.SharedAsync();
        var host = new ApiHost(mongo.ConnectionString);
        await CatalogTestDatabase.SeedAsync(host.Factory.Services);
        return host;
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

    /// <summary>API de prueba con su propia base en el contenedor compartido; al terminar se borra la base.</summary>
    private sealed class ApiHost : IAsyncDisposable
    {
        private readonly string _databaseName = "market_oficios_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(4));

        public ApiHost(string connectionString) =>
            Factory = new KnockApiFactory(settings: new Dictionary<string, string?>
            {
                ["ConnectionStrings:KnockMongo"] = connectionString,
                ["Mongo:Databases:Catalog"] = _databaseName,
            });

        public KnockApiFactory Factory { get; }

        /// <summary>
        /// Oficio sintético, solo en la base de la prueba (nunca en la semilla versionada). Pasa por <see cref="CatalogSeeder"/>
        /// para que exista <c>nombreBusqueda</c>: sin él, un documento se ordena primero y TC-03 pasaría en falso.
        /// </summary>
        public async Task AddTradeAsync(Trade trade, bool active)
        {
            await using var scope = Factory.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<CatalogSeeder>().SeedAsync(
                [new BsonDocument
                {
                    { "_id", ObjectId.GenerateNewId() },
                    { "clave", trade.Clave },
                    { "nombre", trade.Nombre },
                    { "modalidad", trade.Modalidad },
                    { "activo", active },
                }],
                [],
                CancellationToken.None);
        }

        public async ValueTask DisposeAsync()
        {
            await Factory.Services.GetRequiredService<IMongoClient>().DropDatabaseAsync(_databaseName);
            await Factory.DisposeAsync();
        }
    }
}
