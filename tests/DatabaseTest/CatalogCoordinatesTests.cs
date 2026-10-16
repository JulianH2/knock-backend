using AwesomeAssertions;
using Knock.Api.Modules.Catalog;
using Knock.Api.Modules.Catalog.Data;
using Knock.Api.Modules.Catalog.Domain;
using Knock.DatabaseTest.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Knock.DatabaseTest;

public sealed class CatalogCoordinatesTests : IAsyncLifetime
{
    private readonly CatalogDatabaseFixture _database = new();

    public Task InitializeAsync() => _database.InitializeAsync();

    public Task DisposeAsync() => _database.DisposeAsync();

    [Fact]
    public async Task PostgisMapsLatitudeLongitudeToXLongitudeYLatitudeAndRejectsOutOfRange_TC_TT_BD_02_08()
    {
        // Latitud y longitud sintéticas distintas, dentro del cuadrado «Beta Borde».
        const double latitude = 10.005;
        const double longitude = 30.005;
        await using var scope = _database.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var catalog = scope.ServiceProvider.GetRequiredService<ICatalogApi>();
        var point = CatalogCoordinates.ToPoint(latitude, longitude);

        var axes = await context.Database.SqlQuery<string>(
            $"""SELECT ST_X({point}::geometry)::text || ' ' || ST_Y({point}::geometry)::text AS "Value" """)
            .ToListAsync();
        var neighborhood = await catalog.FindNeighborhoodAtAsync(latitude, longitude, CancellationToken.None);
        var (swappedLatitude, swappedLongitude) = (longitude, latitude);
        var swapped = await catalog.FindNeighborhoodAtAsync(swappedLatitude, swappedLongitude, CancellationToken.None);

        axes.Should().Equal("30.005 10.005");
        neighborhood.Should().Be("Beta Borde");
        swapped.Should().BeNull("con los ejes invertidos el punto cae en otro sitio");
        await AssertRejectedBeforeQueryingAsync(91, 0);
        await AssertRejectedBeforeQueryingAsync(-91, 0);
        await AssertRejectedBeforeQueryingAsync(0, 181);
        await AssertRejectedBeforeQueryingAsync(0, -181);
        await AssertRejectedBeforeQueryingAsync(double.NaN, 0);
    }

    /// <summary>Con una base inaccesible: si se consultara PostGIS fallaría la conexión, no la validación.</summary>
    private static async Task AssertRejectedBeforeQueryingAsync(double latitude, double longitude)
    {
        var unavailable = new NpgsqlConnectionStringBuilder { Host = "127.0.0.1", Port = 1, Timeout = 2 }.ConnectionString;
        await using var services = CatalogDatabaseFixture.BuildCatalogServices(unavailable);
        await using var scope = services.CreateAsyncScope();
        var catalog = scope.ServiceProvider.GetRequiredService<ICatalogApi>();

        var act = () => catalog.FindNeighborhoodAtAsync(latitude, longitude, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
}
