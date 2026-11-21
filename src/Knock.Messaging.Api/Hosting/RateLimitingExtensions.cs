// jscpd:ignore-start -- EXC-021: variante reducida de src/Knock.Market.Api/Hosting/RateLimitingExtensions.cs (KNOCK-241); se unifica en la biblioteca técnica de KNOCK-251.
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading.RateLimiting;
using Knock.Messaging.Api.Common.Errors;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Knock.Messaging.Api.Hosting;

/// <summary>Una política de ventana fija por IP: peticiones permitidas y duración de la ventana (sección <c>RateLimiting:*</c> de la configuración).</summary>
internal sealed class RateLimitPolicySettings
{
    public int PermitLimit { get; set; }

    public TimeSpan Window { get; set; }

    internal bool IsValid => PermitLimit > 0 && Window > TimeSpan.Zero;
}

/// <summary>Valores del limitador; sin secretos. No hay valores por defecto en código: sin configuración válida el servicio no arranca.</summary>
internal sealed class RateLimitSettings
{
    public const string SectionName = "RateLimiting";

    public RateLimitPolicySettings Global { get; set; } = new();

    /// <summary>Política propia de <c>/health/ready</c> (hace un ping a MongoDB y a Redis por petición anónima, así que no puede quedar sin límite).</summary>
    public RateLimitPolicySettings Ready { get; set; } = new();
}

internal static class RateLimitingExtensions
{
    /// <summary>Crear conversaciones: 20 por hora por persona (spec 030, R-05), además del límite general por IP. Contador compartido en Redis.</summary>
    public const string CreateConversationPolicy = "conversaciones-crear";

    private const string _healthPath = "/health";
    private const string _readyPath = "/health/ready";
    private const string _unknownClient = "unknown";

    /// <summary>
    /// Límite por IP con el limitador del framework: política global (cualquier ruta salvo <c>/health</c> y <c>/health/ready</c>) y política propia
    /// para <c>/health/ready</c>; <c>/health</c> es la única exención. Vive en memoria del proceso (con varias réplicas el límite efectivo se multiplica).
    /// Responde 429 con <c>Retry-After</c> en ProblemDetails.
    /// </summary>
    public static IServiceCollection AddKnockRateLimiting(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddOptions<RateLimitSettings>()
            .BindConfiguration(RateLimitSettings.SectionName)
            .Validate(
                s => s.Global.IsValid && s.Ready.IsValid,
                "RateLimiting:Global and RateLimiting:Ready need a PermitLimit > 0 and a Window > 0.")
            .ValidateOnStart();
        services.AddRateLimiter(_ => { });
        services.AddOptions<RateLimiterOptions>().Configure<IOptions<RateLimitSettings>, IServiceProvider>((options, settings, provider) =>
        {
            var global = settings.Value.Global;
            var ready = settings.Value.Ready;
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(CreateConversationPolicy, SubjectPolicy(CreateConversationPolicy, 20, TimeSpan.FromHours(1), provider));
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context => Partition(context, global, ready));
            options.OnRejected = async (context, _) =>
            {
                var fallback = IsReadyPath(context.HttpContext.Request.Path) ? ready.Window : global.Window;
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var hint) ? hint : fallback;
                var seconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
                context.HttpContext.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
                // Cuerpo uniforme: sin IP, clave de partición, límite ni contador.
                await ProblemResponses.WriteAsync(context.HttpContext, StatusCodes.Status429TooManyRequests, "Too Many Requests", ErrorCodes.RateLimitedCode);
            };
        });
        return services;
    }

    /// <summary>Política por <c>sub</c> del token con contador compartido; sin <c>sub</c> no limita (la autenticación ya respondió 401).</summary>
    private static Func<HttpContext, RateLimitPartition<string>> SubjectPolicy(string name, int permitLimit, TimeSpan window, IServiceProvider services) =>
        context => context.User.FindFirst("sub")?.Value is { Length: > 0 } subject
            ? RateLimitPartition.Get($"{name}:{subject}", partition => new SharedRateLimiter(services, partition, new RateLimitPolicySettings { PermitLimit = permitLimit, Window = window }))
            : RateLimitPartition.GetNoLimiter(_unknownClient);

    private static RateLimitPartition<string> Partition(HttpContext context, RateLimitPolicySettings global, RateLimitPolicySettings ready)
    {
        var path = context.Request.Path;
        if (path.Equals(_healthPath, StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetNoLimiter("health");
        }

        return IsReadyPath(path)
            ? RateLimitPartition.GetFixedWindowLimiter($"ready:{ClientKey(context)}", _ => FixedWindow(ready))
            : RateLimitPartition.GetFixedWindowLimiter($"global:{ClientKey(context)}", _ => FixedWindow(global));
    }

    private static FixedWindowRateLimiterOptions FixedWindow(RateLimitPolicySettings policy) => new()
    {
        PermitLimit = policy.PermitLimit,
        Window = policy.Window,
        QueueLimit = 0,
        AutoReplenishment = true,
    };

    private static bool IsReadyPath(PathString path) => path.Equals(_readyPath, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// IP de la conexión (ya sustituida por la reenviada solo si viene de un proxy de confianza). IPv4 mapeada se normaliza y las IPv6
    /// se agrupan por /64 para que rotar direcciones de un mismo prefijo no evada el límite.
    /// </summary>
    internal static string ClientKey(HttpContext context)
    {
        var address = context.Connection.RemoteIpAddress;
        if (address is null)
        {
            return _unknownClient;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return address.ToString();
        }

        var bytes = address.GetAddressBytes();
        Array.Clear(bytes, 8, 8);
        return $"{new IPAddress(bytes)}/64";
    }
}
// jscpd:ignore-end -- EXC-021
