extern alias MessagingApi;

using System.Reflection;
using Knock.IntegrationTest.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Knock.SecurityTest.Support;

/// <summary>Ruta protegida de mentira: Mensajería todavía no tiene endpoints de negocio, y la política por defecto «exigir sesión» solo se ve en uno.</summary>
[ApiController]
[Route("sonda/protegida")]
internal sealed class ProtectedProbeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { sub = User.FindFirst("sub")?.Value });
}

/// <summary>MVC solo descubre controllers públicos; el de prueba es interno, así que se agrega al descubrimiento a mano.</summary>
internal sealed class ProtectedProbeFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        ArgumentNullException.ThrowIfNull(feature);
        // MVC lo instancia por reflexión; crearlo aquí también evita el aviso CA1812 de «clase interna nunca instanciada».
        feature.Controllers.Add(new ProtectedProbeController().GetType().GetTypeInfo());
    }
}

/// <summary>
/// Host de <c>Knock.Messaging.Api</c> para las pruebas de seguridad (BUG-004): Redis real con contraseña para la lista de sesiones canceladas, una ruta
/// protegida de mentira y la IP de conexión fijada por encabezado. MongoDB no se usa en estas rutas (cadena sintética). Solo datos ficticios.
/// </summary>
internal sealed class SecurityMessagingFactory(IReadOnlyDictionary<string, string?> settings) : WebApplicationFactory<MessagingApi::Program>
{
    private const string _highLimit = "1000000";

    public static async Task<SecurityMessagingFactory> CreateAsync(IReadOnlyDictionary<string, string?>? overrides = null)
    {
        var redis = await RedisTestFixture.SharedAsync();
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:KnockRedis"] = redis.ConnectionString,
            ["ConnectionStrings:KnockMongo"] = "mongodb://127.0.0.1:27099/?serverSelectionTimeoutMS=500",
        };
        foreach (var (key, value) in overrides ?? new Dictionary<string, string?>())
        {
            values[key] = value;
        }

        return new SecurityMessagingFactory(values);
    }

    /// <summary>Marca la sesión como cancelada, como lo hace Identidad al cerrar sesión.</summary>
    public async Task RevokeSessionAsync(string sessionId) =>
        await Services.GetRequiredService<IConnectionMultiplexer>().GetDatabase()
            .StringSetAsync(Knock.Contracts.Sessions.SessionRevocationKey.Prefix + sessionId, "1", TimeSpan.FromMinutes(15));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseEnvironment("Production");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var values = new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = TokenForge.Issuer,
                ["Jwt:Audience"] = TokenForge.Audience,
                ["Jwt:SigningKey"] = Convert.ToBase64String(TokenForge.SigningKey),
                ["RateLimiting:Global:PermitLimit"] = _highLimit,
                ["RateLimiting:Ready:PermitLimit"] = _highLimit,
                // Spec 026: el arranque exige decir si el chat está encendido y dónde está Mercado; estas pruebas no usan el hub.
                ["Chat:Habilitado"] = "false",
                ["Services:Market:BaseUrl"] = "http://127.0.0.1:5234/",
            };
            foreach (var (key, value) in settings)
            {
                values[key] = value;
            }

            configuration.AddInMemoryCollection(values);
        });
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IStartupFilter>(new RemoteIpStartupFilter());
            services.AddControllers().ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(new ProtectedProbeFeatureProvider()));
        });
    }
}
