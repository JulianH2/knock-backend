using Knock.Api.Modules.Catalog;
using Knock.Api.Modules.Catalog.Data;
using Knock.Api.Modules.Catalog.Domain;
using Knock.IntegrationTest.Data;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;
using Xunit;

namespace Knock.DatabaseTest.Support;

/// <summary>
/// PostGIS real con las migraciones y semillas de Catalog aplicadas, más colonias sintéticas lejos de Tula
/// (latitud 10, CP 99999) para los casos de borde y de respaldo por centro. Sin coordenadas de usuarios.
/// </summary>
internal sealed class CatalogDatabaseFixture : IAsyncLifetime
{
    public const string SyntheticPostalCode = "99999";

    private PostgisFixture? _postgis;
    private ServiceProvider? _services;

    public IServiceProvider Services => _services ?? throw new InvalidOperationException("Fixture not initialized.");

    public async Task InitializeAsync()
    {
        _postgis = await PostgisFixture.StartAsync();
        _services = BuildCatalogServices(_postgis.ConnectionString);
        await CatalogTestDatabase.MigrateAndSeedAsync(_services);
        await InsertSyntheticNeighborhoodsAsync();
    }

    public async Task DisposeAsync()
    {
        if (_services is not null)
        {
            await _services.DisposeAsync();
        }

        if (_postgis is not null)
        {
            await _postgis.DisposeAsync();
        }
    }

    /// <summary>Solo el módulo Catalog, como lo registra la API.</summary>
    public static ServiceProvider BuildCatalogServices(string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:KnockDatabase"] = connectionString })
            .Build();
        return new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddCatalogModule()
            .BuildServiceProvider(validateScopes: true);
    }

    private async Task InsertSyntheticNeighborhoodsAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        context.Colonias.AddRange(
            // Dos cuadrados que comparten el borde longitud 30.01; con orden binario «Beta» iría antes que «alfa»; normalizado, gana «alfa».
            Synthetic("Beta Borde", Square(30.00, 10.00), Point(30.005, 10.005)),
            Synthetic("alfa Borde", Square(30.01, 10.00), Point(30.015, 10.005)),
            // Centro justo sobre el borde compartido: si el respaldo por centro se aplicara antes que ST_Covers, ganaría.
            Synthetic("Centro Sobre Borde", geom: null, Point(30.01, 10.005)),
            // Solo centro, separados unos 1.6 km.
            Synthetic("Respaldo Cercana", geom: null, Point(20.000, 10.000)),
            Synthetic("Respaldo Lejana", geom: null, Point(20.015, 10.000)));
        await context.SaveChangesAsync();
    }

    private static Colonia Synthetic(string nombre, MultiPolygon? geom, Point centro) =>
        Colonia.Create(nombre, SyntheticPostalCode, "Sintético", geom, centro);

    private static Point Point(double longitude, double latitude) => new(longitude, latitude) { SRID = 4326 };

    /// <summary>Cuadrado de 0.01° con esquina suroeste en (longitud, latitud).</summary>
    private static MultiPolygon Square(double longitude, double latitude)
    {
        const double size = 0.01;
        var ring = new LinearRing(
        [
            new Coordinate(longitude, latitude),
            new Coordinate(longitude + size, latitude),
            new Coordinate(longitude + size, latitude + size),
            new Coordinate(longitude, latitude + size),
            new Coordinate(longitude, latitude),
        ]);
        return new MultiPolygon([new Polygon(ring)]) { SRID = 4326 };
    }
}
