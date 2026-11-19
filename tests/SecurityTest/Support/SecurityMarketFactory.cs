extern alias MarketApi;

using System.Security.Cryptography;
using Knock.IntegrationTest.Support;
using MarketApi::Knock.Market.Api.Modules.Catalog.Data;
using MarketApi::Knock.Market.Api.Modules.Catalog.Domain;
using MarketApi::Knock.Market.Api.Modules.Catalog.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using StackExchange.Redis;

namespace Knock.SecurityTest.Support;

/// <summary>Localizador de colonias de mentira: cuenta las llamadas y guarda las coordenadas recibidas para comprobar que una entrada inválida nunca llega.</summary>
internal sealed class RecordingNeighborhoodLocator : INeighborhoodLocator
{
    private readonly List<(double Latitude, double Longitude)> _calls = [];

    public IReadOnlyList<(double Latitude, double Longitude)> Calls
    {
        get
        {
            lock (_calls)
            {
                return [.. _calls];
            }
        }
    }

    /// <summary>Si se define, la consulta lanza esta excepción (simula la base caída).</summary>
    public Exception? Failure { get; set; }

    public Colonia? Answer { get; set; } = new("665f1b2c3d4e5f6071829304", "Centro", "86000", "Centro");

    public Task<Colonia?> FindAtAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        lock (_calls)
        {
            _calls.Add((latitude, longitude));
        }

        return Failure is null ? Task.FromResult(Answer) : Task.FromException<Colonia?>(Failure);
    }
}

/// <summary>
/// Host de <c>Knock.Market.Api</c> para las pruebas de seguridad: MongoDB real (documentdb-local) para el catálogo de oficios y un localizador de colonias
/// de mentira (la consulta geográfica se prueba en DatabaseTest). Corre en <c>Production</c> por defecto. Solo datos ficticios.
/// </summary>
internal sealed class SecurityMarketFactory(IReadOnlyDictionary<string, string?> settings, string environment) : WebApplicationFactory<CatalogStore>
{
    private const string _highLimit = "1000000";

    private static int _redisDatabaseCounter;

    public RecordingNeighborhoodLocator Locator { get; } = new();

    public IMongoDatabase Database => Services.GetRequiredService<IMongoClient>().GetDatabase(settings["Mongo:Databases:Catalog"]!);

    public IMongoCollection<BsonDocument> Oficios => Database.GetCollection<BsonDocument>(CatalogStore.OficiosCollection);

    public static async Task<SecurityMarketFactory> CreateAsync(IReadOnlyDictionary<string, string?>? overrides = null, string environment = "Production")
    {
        SecurityHostConfiguration.ClearFirebaseEmulatorHost();
        var mongo = await DocumentDbFixture.SharedAsync();
        var redis = await RedisTestFixture.SharedAsync();
        // Las claves del catálogo son fijas: cada fábrica usa su propia base lógica de Redis (la 1 a la 15, en ciclo) y la limpia al
        // crearse; sin esto, dos fábricas del mismo contenedor compartían la base 0 y una prueba veía la caché de otra (KNOCK-205).
        var index = (Interlocked.Increment(ref _redisDatabaseCounter) % 15) + 1;
        var redisConnectionString = redis.ConnectionString + $",defaultDatabase={index},allowAdmin=true";
        await using (var multiplexer = await ConnectionMultiplexer.ConnectAsync(redisConnectionString))
        {
            await multiplexer.GetServer(multiplexer.GetEndPoints()[0]).FlushDatabaseAsync();
        }
        var values = new Dictionary<string, string?>
        {
            ["Busqueda:Cursor:Key"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            ["ConnectionStrings:KnockMongo"] = mongo.ConnectionString,
            ["ConnectionStrings:KnockRedis"] = redisConnectionString,
            ["Mongo:Databases:Catalog"] = "market_sec_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6)),
        };
        foreach (var (key, value) in overrides ?? new Dictionary<string, string?>())
        {
            values[key] = value;
        }

        var factory = new SecurityMarketFactory(values, environment);
        await factory.Services.GetRequiredService<CatalogStore>().EnsureIndexesAsync(CancellationToken.None);
        return factory;
    }

    /// <summary>Inserta un oficio del catálogo con la forma de las semillas.</summary>
    public Task AddOficioAsync(string clave, string nombre, string modalidad, bool activo) =>
        Oficios.InsertOneAsync(new BsonDocument
        {
            [CatalogStore.ClaveField] = clave,
            [CatalogStore.NombreField] = nombre,
            [CatalogStore.NombreBusquedaField] = nombre.ToUpperInvariant(),
            [CatalogStore.ModalidadField] = modalidad,
            [CatalogStore.ActivoField] = activo,
            [CatalogStore.VersionField] = 1,
        });

    /// <summary>Inserta una colonia del catálogo con lo mínimo que consulta «¿existe esta colonia?»: nombre, nombre normalizado, código postal y municipio.</summary>
    public Task AddColoniaAsync(string nombre, string cp, string municipio) =>
        Database.GetCollection<BsonDocument>(CatalogStore.ColoniasCollection).InsertOneAsync(new BsonDocument
        {
            [CatalogStore.NombreField] = nombre,
            [CatalogStore.NombreBusquedaField] = SearchNameNormalizer.Normalize(nombre),
            [CatalogStore.CpField] = cp,
            [CatalogStore.MunicipioField] = municipio,
            [CatalogStore.VersionField] = 1,
        });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseEnvironment(environment);
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            SecurityHostConfiguration.Apply(configuration, _highLimit, settings);
        });
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<Microsoft.AspNetCore.Hosting.IStartupFilter>(new RemoteIpStartupFilter());
            services.AddSingleton<INeighborhoodLocator>(Locator);
        });
    }
}
