using System.Net;
using AwesomeAssertions;
using Knock.Api.Modules.Catalog.Data;
using Knock.IntegrationTest.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Knock.IntegrationTest.Data;

public sealed class PostgisInfrastructureTests
{
    [Fact]
    public async Task Migrate_InitialDatabase_CreatesPostgisAndModuleSchemas_TC_TT_BD_01_02()
    {
        await using var postgis = await PostgisFixture.StartAsync();
        using var factory = new KnockApiFactory(connectionString: postgis.ConnectionString);
        using var scope = factory.Services.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var designTimeModel = context.GetService<IDesignTimeModel>().Model;
        designTimeModel.GetDefaultSchema().Should().Be("catalog");
        designTimeModel.FindAnnotation("Npgsql:PostgresExtension:postgis").Should().NotBeNull();
        context.Model.GetDefaultSchema().Should().Be("catalog");
        await context.Database.MigrateAsync();

        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        var schemas = await ReadSchemasAsync(postgis.ConnectionString);
        var postgisVersion = await ReadPostgisVersionAsync(postgis.ConnectionString);

        pendingMigrations.Should().BeEmpty();
        schemas.Should().Contain(
            ["catalog", "identity", "requests", "messaging", "reputation", "trust", "notifications", "assistant"]);
        postgisVersion.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ReadPostgisVersion_PostgisDatabaseReturnsVersion_TC_TT_BD_01_01()
    {
        await using var postgis = await PostgisFixture.StartAsync();

        var postgisVersion = await ReadPostgisVersionAsync(postgis.ConnectionString);

        postgisVersion.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Get_HealthReady_PostgresAvailable_Returns200_TC_TT_BD_01_03()
    {
        await using var postgis = await PostgisFixture.StartAsync();
        await PostgisReadinessChecks.AssertReadyReturns200Async(postgis.ConnectionString);
    }

    [Fact]
    public async Task Get_HealthReady_PostgresUnavailable_Returns503_TC_TT_BD_01_03()
    {
        var unavailableConnectionString = new NpgsqlConnectionStringBuilder
        {
            Host = "127.0.0.1",
            Port = 1,
            Timeout = 2,
            Database = "knock",
        }.ConnectionString;
        using var factory = new KnockApiFactory(connectionString: unavailableConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Start_PostgisContainer_UsesPinnedPostgisImage_TC_TT_BD_01_04()
    {
        await using var postgis = await PostgisFixture.StartAsync();
        var postgisVersion = await ReadPostgisVersionAsync(postgis.ConnectionString);

        postgisVersion.Should().Contain("3.4");
    }

    private static async Task<List<string>> ReadSchemasAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT schema_name FROM information_schema.schemata",
            connection);
        await using var reader = await command.ExecuteReaderAsync();
        var schemas = new List<string>();
        while (await reader.ReadAsync())
        {
            schemas.Add(reader.GetString(0));
        }

        return schemas;
    }

    private static async Task<string?> ReadPostgisVersionAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT postgis_version()", connection);
        return (string?)await command.ExecuteScalarAsync();
    }
}

public sealed class MigrationConnectionGuardTests
{
    [Fact]
    public void ValidateConnection_RemoteHost_RejectsMigration_TC_TT_BD_01_05()
    {
        var remoteConnectionString = new NpgsqlConnectionStringBuilder
        {
            Host = "database.example.com",
        }.ConnectionString;
        var act = () => LocalMigrationConnectionGuard.EnsureLocalHost(remoteConnectionString);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("127.0.0.1")]
    public void ValidateConnection_LocalHost_AllowsMigration_TC_TT_BD_01_05(string host)
    {
        var connectionString = new NpgsqlConnectionStringBuilder
        {
            Host = host,
        }.ConnectionString;

        var act = () => LocalMigrationConnectionGuard.EnsureLocalHost(connectionString);

        act.Should().NotThrow();
    }
}
