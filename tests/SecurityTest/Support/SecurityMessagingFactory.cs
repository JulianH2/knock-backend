extern alias MessagingApi;
using System.Reflection;
using Asp.Versioning;
using FluentValidation;
using Knock.IntegrationTest.Support;
using MessagingApi::Knock.Messaging.Api.Hosting;
using MessagingApi::Knock.Messaging.Api.Modules.Conversaciones.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Knock.SecurityTest.Support;

/// <summary>Ruta protegida de mentira: Mensajería todavía no tiene endpoints de negocio, y la política por defecto «exigir sesión» solo se ve en uno.</summary>
[ApiController]
[ApiVersionNeutral]
[Route("sonda/protegida")]
internal sealed class ProtectedProbeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { sub = User.FindFirst("sub")?.Value });
}

/// <summary>Cuerpo de la sonda de validación (spec 030, T-04: la infraestructura de controllers de Mensajería).</summary>
internal sealed record ProbeBody(string? Texto);

/// <summary>Validador de la sonda: el segundo mensaje repite el valor enviado a propósito, para comprobar que el filtro nunca lo devuelve.</summary>
internal sealed class ProbeBodyValidator : AbstractValidator<ProbeBody>
{
    public ProbeBodyValidator()
    {
        RuleFor(x => x.Texto).NotEmpty().WithMessage("Texto is required.");
        RuleFor(x => x.Texto).MaximumLength(10).WithMessage("'{PropertyValue}' is too long.");
    }
}

/// <summary>
/// Sonda de la infraestructura de controllers de Mensajería: versión en la ruta, filtro de validación (422), <c>DomainException</c> con su <c>code</c>, <c>[PrivateNoStore]</c> y la
/// política de límite por <c>sub</c>. Mensajería aún no tiene endpoints de negocio (llegan con T-05 a T-07).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/sonda/infra")]
internal sealed class InfraProbeController : ControllerBase
{
    [HttpPost("validar")]
    public IActionResult Validate(ProbeBody body) => Ok(new { recibido = body.Texto?.Length });

    [HttpGet("dominio/{kind}")]
    public IActionResult Domain(string kind) =>
        throw new DomainException(Enum.Parse<DomainErrorKind>(kind), $"detalle interno con datos de la persona en {Request.Path}", "probe_code");

    [HttpGet("explota")]
    public IActionResult Explode() => throw new InvalidOperationException($"detalle interno que no debe salir en {Request.Path}");

    [HttpGet("privado")]
    [PrivateNoStore]
    public IActionResult Private() => Ok();

    [HttpGet("publico")]
    public IActionResult Public() => Ok(new ProbeBody("ok"));

    [HttpPost("crear")]
    [EnableRateLimiting(RateLimitingExtensions.CreateConversationPolicy)]
    public IActionResult Create() => StatusCode(StatusCodes.Status201Created);
}

/// <summary>MVC solo descubre controllers públicos; el de prueba es interno, así que se agrega al descubrimiento a mano.</summary>
internal sealed class ProtectedProbeFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        ArgumentNullException.ThrowIfNull(feature);
        // MVC lo instancia por reflexión; crearlo aquí también evita el aviso CA1812 de «clase interna nunca instanciada».
        feature.Controllers.Add(new ProtectedProbeController().GetType().GetTypeInfo());
        feature.Controllers.Add(new InfraProbeController().GetType().GetTypeInfo());
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
            services.AddSingleton<IValidator<ProbeBody>>(new ProbeBodyValidator());
            services.AddControllers().ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(new ProtectedProbeFeatureProvider()));
        });
    }
}
