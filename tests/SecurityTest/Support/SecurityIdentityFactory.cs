extern alias IdentityApi;

using System.Net;
using System.Security.Cryptography;
using IdentityApi::Knock.Identity.Api.Modules.Account.Services;
using IdentityApi::Knock.Identity.Api.Modules.Sesiones.Data;
using IdentityApi::Knock.Identity.Api.Modules.Sesiones.Services;
using Knock.Contracts.Colonias;
using Knock.IntegrationTest.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using StackExchange.Redis;

namespace Knock.SecurityTest.Support;

/// <summary>Catálogo de colonias de Mercado de mentira para Identidad: conoce unas pocas y puede «caerse».</summary>
internal sealed class FakeColoniaCatalog : IColoniaCatalog
{
    public HashSet<string> Known { get; } = ["Centro", "Tultengo"];

    public Task<bool> ExistsAsync(string colonia, CancellationToken cancellationToken) => Task.FromResult(Known.Contains(colonia));
}

/// <summary>
/// Host de <c>Knock.Identity.Api</c> para las pruebas de seguridad: MongoDB (documentdb-local) y Redis reales de Testcontainers, una base propia por host,
/// y dobles solo donde no hay infraestructura local: el verificador de Firebase (<see cref="FakeFirebaseVerifier"/>) y el catálogo de colonias. Corre en
/// <c>Production</c> por defecto, para medir lo que ve un cliente real (sin <c>stackTrace</c> ni detalle de excepciones). Solo datos ficticios.
/// </summary>
internal sealed class SecurityIdentityFactory(IReadOnlyDictionary<string, string?> settings, string environment) : WebApplicationFactory<LoginService>
{
    /// <summary>Encabezado que solo entiende el host de pruebas: fija <c>Connection.RemoteIpAddress</c> (TestServer no trae IP).</summary>
    public const string RemoteIpHeader = "X-Test-Remote-Ip";

    private const string _highLimit = "1000000";

    private static readonly byte[] _phoneEncryptionKey = RandomNumberGenerator.GetBytes(32);
    private static readonly byte[] _phoneHmacKey = RandomNumberGenerator.GetBytes(32);

    public FakeFirebaseVerifier Firebase { get; } = new();

    public FakeColoniaCatalog Colonias { get; } = new();

    /// <summary>Mercado de mentira para las consultas internas de la cuenta (spec 039): las rutas reales todavía no existen.</summary>
    public FakeMarketAccountClient MarketAccount { get; } = new();

    public IMongoDatabase Database => Services.GetRequiredService<IMongoClient>().GetDatabase(settings["Mongo:Databases:Identity"]!);

    public IMongoCollection<BsonDocument> Users => Database.GetCollection<BsonDocument>(SesionesStore.UsersCollection);

    public IMongoCollection<BsonDocument> Sessions => Database.GetCollection<BsonDocument>(SesionesStore.SessionsCollection);

    public IDatabase Redis => Services.GetRequiredService<IConnectionMultiplexer>().GetDatabase();

    /// <summary>Crea el host. <paramref name="overrides"/> reemplaza cualquier valor de configuración (por ejemplo, el límite de peticiones).</summary>
    public static async Task<SecurityIdentityFactory> CreateAsync(IReadOnlyDictionary<string, string?>? overrides = null, string environment = "Production")
    {
        var mongo = await DocumentDbFixture.SharedAsync();
        var redis = await RedisTestFixture.SharedAsync();
        SecurityHostConfiguration.ClearFirebaseEmulatorHost();

        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:KnockMongo"] = mongo.ConnectionString,
            ["ConnectionStrings:KnockRedis"] = redis.ConnectionString,
            ["Mongo:Databases:Identity"] = "identity_sec_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6)),
        };
        foreach (var (key, value) in overrides ?? new Dictionary<string, string?>())
        {
            values[key] = value;
        }

        var factory = new SecurityIdentityFactory(values, environment);
        await factory.Services.GetRequiredService<SesionesStore>().EnsureIndexesAsync(CancellationToken.None);
        return factory;
    }

    /// <summary>Entra con un celular ficticio nuevo y devuelve la sesión (JWT, token de renovación y <c>sub</c>). El <c>uid</c> de Firebase es único por llamada.</summary>
    public async Task<SignedInPerson> SignInAsync(HttpClient client, string? phone = null, string? deviceId = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        var number = phone ?? NewPhone();
        var uid = "uid-sec-" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(8));
        using var response = await SecureHttp.PostJsonAsync(
            client,
            "/api/v1/auth/firebase",
            new { firebaseIdToken = Firebase.Issue(uid, number), deviceId = deviceId ?? Guid.NewGuid().ToString("D"), aceptaAviso = true, dispositivo = "Teléfono de prueba" });
        response.EnsureSuccessStatusCode();
        var body = await SecureHttp.ReadJsonAsync(response);
        var access = body.GetProperty("accessToken").GetString()!;
        return new SignedInPerson(access, body.GetProperty("refreshToken").GetString()!, TokenForge.Claim(access, "sub"), TokenForge.Claim(access, "sid"), number, uid);
    }

    /// <summary>Celular mexicano ficticio y distinto en cada llamada.</summary>
    public static string NewPhone() => "+5255" + RandomNumberGenerator.GetInt32(10_000_000, 100_000_000).ToString(System.Globalization.CultureInfo.InvariantCulture);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseEnvironment(environment);
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            SecurityHostConfiguration.Apply(
                configuration,
                _highLimit,
                settings,
                new Dictionary<string, string?>
                {
                    ["Identity:PhoneProtection:EncryptionKeys:1"] = Convert.ToBase64String(_phoneEncryptionKey),
                    ["Identity:PhoneProtection:HmacKeys:1"] = Convert.ToBase64String(_phoneHmacKey),
                    ["Identity:PhoneProtection:EncryptionKeyVersion"] = "1",
                    ["Identity:PhoneProtection:HmacKeyVersion"] = "1",
                    ["Perfil:HostsFoto:0"] = "cdn.knock.test",
                });
        });
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IStartupFilter>(new RemoteIpStartupFilter());
            services.AddSingleton<IFirebaseIdTokenVerifier>(Firebase);
            services.AddSingleton<IColoniaCatalog>(Colonias);
            services.AddSingleton<IMarketAccountClient>(MarketAccount);
        });
    }
}

/// <summary>Persona que ya entró: lo que una prueba necesita para llamar a la API como ella.</summary>
internal sealed record SignedInPerson(string AccessToken, string RefreshToken, string Sub, string SessionId, string Phone, string Uid);

/// <summary>Pone la IP de conexión desde <see cref="SecurityIdentityFactory.RemoteIpHeader"/> antes del resto del pipeline de la API.</summary>
internal sealed class RemoteIpStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        ArgumentNullException.ThrowIfNull(next);
        return app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                if (context.Request.Headers.TryGetValue(SecurityIdentityFactory.RemoteIpHeader, out var value)
                    && IPAddress.TryParse(value.ToString(), out var address))
                {
                    context.Connection.RemoteIpAddress = address;
                }

                return nextMiddleware(context);
            });
            next(app);
        };
    }
}
