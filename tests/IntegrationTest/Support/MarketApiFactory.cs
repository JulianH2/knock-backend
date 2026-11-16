extern alias MarketApi;

using System.Security.Cryptography;
using MarketApi::Knock.Market.Api.Modules.Requests.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.IntegrationTest.Support;

/// <summary>
/// Host de pruebas de <c>Knock.Market.Api</c> sobre documentdb-local y Redis (contenedores compartidos): una base propia por host, para que las
/// pruebas no se pisen. El tipo <see cref="RequestsApi"/> solo ubica el ensamblado del servicio: así no choca con el <c>Program</c> de otro
/// servicio en este mismo proyecto. Las colecciones y los índices del módulo Requests los crea el script <c>db/020-requests-solicitudes-cotizaciones.js</c>
/// en la entrega; el host de pruebas los crea con <see cref="CreateAsync"/> a partir de las mismas definiciones. Solo datos ficticios.
/// </summary>
internal sealed class MarketApiFactory(IReadOnlyDictionary<string, string?> settings) : WebApplicationFactory<RequestsApi>
{
    /// <summary>Límite alto por defecto: las pruebas que no miden el limitador no deben recibir 429 (el limitador sigue activo).</summary>
    private const string _highLimit = "1000000";

    // Clave de firma sintética: 32 bytes aleatorios por proceso, nunca se escriben en disco.
    private static readonly byte[] _signingKey = RandomNumberGenerator.GetBytes(32);

    /// <summary>Servicios que la prueba sustituye en el host (por ejemplo, un reloj fijo).</summary>
    public Action<IServiceCollection>? ExtraServices { get; private init; }

    /// <summary>Base de este host: la usan a la vez Catalog y Requests (hoy ambos viven en «market»).</summary>
    public string DatabaseName => settings["Mongo:Databases:Requests"]!;

    public static string SigningKeyBase64 => Convert.ToBase64String(_signingKey);

    public IMongoDatabase Database => Services.GetRequiredService<IMongoClient>().GetDatabase(DatabaseName);

    public IMongoCollection<BsonDocument> Requests => Database.GetCollection<BsonDocument>("solicitudes");

    public IMongoCollection<BsonDocument> Quotes => Database.GetCollection<BsonDocument>("cotizaciones");

    /// <summary>Crea el host con una base propia y los índices del módulo Requests. <paramref name="overrides"/> reemplaza cualquier valor de configuración.</summary>
    public static async Task<MarketApiFactory> CreateAsync(IReadOnlyDictionary<string, string?>? overrides = null, Action<IServiceCollection>? configureServices = null)
    {
        var mongo = await DocumentDbFixture.SharedAsync();
        var redis = await RedisTestFixture.SharedAsync();
        var database = "market_it_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:KnockMongo"] = mongo.ConnectionString,
            ["ConnectionStrings:KnockRedis"] = redis.ConnectionString,
            ["Mongo:Databases:Catalog"] = database,
            ["Mongo:Databases:Requests"] = database,
        };
        var factory = new MarketApiFactory(Merge(values, overrides)) { ExtraServices = configureServices };
        await EnsureRequestsIndexesAsync(factory.Database);
        return factory;
    }

    /// <summary>
    /// Host sin contenedores: las cadenas de MongoDB y Redis apuntan a un puerto cerrado (el cliente conecta de forma perezosa y la prueba nunca las usa) y
    /// <paramref name="configureServices"/> sustituye lo que tocaría la base. Sirve para probar el canal HTTP (autenticación, límites, errores y encabezados).
    /// </summary>
    public static MarketApiFactory CreateOffline(Action<IServiceCollection> configureServices, IReadOnlyDictionary<string, string?>? overrides = null)
    {
        ArgumentNullException.ThrowIfNull(configureServices);
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:KnockMongo"] = "mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=500",
            ["ConnectionStrings:KnockRedis"] = GatewayTestSupport.DummyRedisConnectionString,
            ["Mongo:Databases:Catalog"] = "market_offline",
            ["Mongo:Databases:Requests"] = "market_offline",
        };
        return new MarketApiFactory(Merge(values, overrides)) { ExtraServices = configureServices };
    }

    private static Dictionary<string, string?> Merge(Dictionary<string, string?> values, IReadOnlyDictionary<string, string?>? overrides)
    {
        foreach (var (key, value) in overrides ?? new Dictionary<string, string?>())
        {
            values[key] = value;
        }

        return values;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var values = new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "knock-api",
                ["Jwt:Audience"] = "knock-app",
                ["Jwt:SigningKey"] = SigningKeyBase64,
                ["RateLimiting:Global:PermitLimit"] = _highLimit,
                ["RateLimiting:Auth:PermitLimit"] = _highLimit,
            };
            foreach (var (key, value) in settings)
            {
                values[key] = value;
            }

            configuration.AddInMemoryCollection(values);
        });
        builder.ConfigureServices(services => ExtraServices?.Invoke(services));
    }

    /// <summary>Mismas definiciones que <c>db/020-requests-solicitudes-cotizaciones.js</c> (spec 020, §6); crear un índice igual dos veces no falla.</summary>
    private static async Task EnsureRequestsIndexesAsync(IMongoDatabase database)
    {
        var requests = database.GetCollection<BsonDocument>("solicitudes").Indexes;
        await requests.CreateManyAsync(
        [
            Index("clienteId_1_creadoEn_-1", new BsonDocument { ["clienteId"] = 1, ["creadoEn"] = -1 }),
            Index("estado_1_venceEn_1", new BsonDocument { ["estado"] = 1, ["venceEn"] = 1 }),
            Index("oficioClave_1_estado_1", new BsonDocument { ["oficioClave"] = 1, ["estado"] = 1 }),
        ]);
        var quotes = database.GetCollection<BsonDocument>("cotizaciones").Indexes;
        await quotes.CreateManyAsync(
        [
            Index("solicitudId_1_tecnicoId_1", new BsonDocument { ["solicitudId"] = 1, ["tecnicoId"] = 1 }, unique: true),
            Index("solicitudId_1_estado_1_creadoEn_1", new BsonDocument { ["solicitudId"] = 1, ["estado"] = 1, ["creadoEn"] = 1 }),
            Index("tecnicoUsuarioId_1_creadoEn_-1", new BsonDocument { ["tecnicoUsuarioId"] = 1, ["creadoEn"] = -1 }),
        ]);
    }

    private static CreateIndexModel<BsonDocument> Index(string name, BsonDocument keys, bool unique = false) =>
        new(keys, new CreateIndexOptions { Name = name, Unique = unique });
}
