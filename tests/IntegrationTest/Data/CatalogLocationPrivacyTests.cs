using System.Globalization;
using AwesomeAssertions;
using Knock.Api.Modules.Catalog;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Knock.IntegrationTest.Data;

public sealed class CatalogLocationPrivacyTests
{
    // Coordenada sintética dentro del polígono piloto aproximado de Centro (db/seeds/colonias_tula.geojson).
    private const double _latitude = 20.051573;
    private const double _longitude = -99.344554;

    [Fact]
    public async Task NeighborhoodLookupDoesNotPersistOrLogCoordinates_TC_TT_BD_02_06()
    {
        await using var postgis = await PostgisFixture.StartAsync();
        using var factory = new KnockApiFactory(connectionString: postgis.ConnectionString);
        await CatalogTestDatabase.MigrateAndSeedAsync(factory.Services);
        var before = await ReadStorageSnapshotAsync(postgis.ConnectionString);

        string? neighborhood = null;
        var output = await ConsoleCapture.RunAsync(async () =>
        {
            await using var scope = factory.Services.CreateAsyncScope();
            neighborhood = await scope.ServiceProvider.GetRequiredService<ICatalogApi>()
                .FindNeighborhoodAtAsync(_latitude, _longitude, CancellationToken.None);
        });

        neighborhood.Should().Be("Centro");
        output.Should().NotBeNullOrWhiteSpace("la consulta registra el comando SQL, sin los valores de sus parámetros");
        output.Should().NotContain(_latitude.ToString("0.######", CultureInfo.InvariantCulture));
        output.Should().NotContain(Math.Abs(_longitude).ToString("0.######", CultureInfo.InvariantCulture));
        (await ReadStorageSnapshotAsync(postgis.ConnectionString)).Should().Be(before);
    }

    /// <summary>Tablas de los esquemas de la aplicación y filas de cada una: la consulta no debe crear ni cambiar nada.</summary>
    private static async Task<string> ReadStorageSnapshotAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT string_agg(table_schema || '.' || table_name, ',' ORDER BY table_schema, table_name)
                || '|' || (SELECT count(*) FROM catalog.colonias)
                || '|' || (SELECT count(*) FROM catalog.oficios)
                || '|' || (SELECT max(xmin::text::bigint) FROM catalog.colonias)
            FROM information_schema.tables
            WHERE table_schema NOT IN ('pg_catalog', 'information_schema', 'tiger', 'tiger_data', 'topology')
            """,
            connection);
        return (string)(await command.ExecuteScalarAsync())!;
    }
}
