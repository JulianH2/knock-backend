using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Knock.Api.Hosting;

/// <summary>Una política de ventana fija por IP: peticiones permitidas y duración de la ventana (sección <c>RateLimiting:*</c> de la configuración).</summary>
internal sealed class RateLimitPolicySettings
{
    public int PermitLimit { get; set; }

    public TimeSpan Window { get; set; }

    internal bool IsValid => PermitLimit > 0 && Window > TimeSpan.Zero;
}

/// <summary>Valores del limitador; sin secretos. No hay valores por defecto en código: sin configuración válida la API no arranca.</summary>
internal sealed class RateLimitSettings
{
    public const string SectionName = "RateLimiting";

    public RateLimitPolicySettings Global { get; set; } = new();

    public RateLimitPolicySettings Auth { get; set; } = new();
}

internal static class RateLimitingExtensions
{
    private const string _healthPath = "/health";
    private const string _readyPath = "/health/ready";
    private const string _unknownClient = "unknown";

    /// <summary>
    /// Límite por IP con el limitador del framework: política global (cualquier ruta salvo <c>/health</c> y <c>/health/ready</c>) y política estricta
    /// para <c>/api/v{n}/auth/*</c>. Vive en memoria del proceso (con varias réplicas el límite efectivo se multiplica). Responde 429 en ProblemDetails.
    /// </summary>
    public static IServiceCollection AddKnockRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<RateLimitSettings>()
            .BindConfiguration(RateLimitSettings.SectionName)
            .Validate(s => s.Global.IsValid && s.Auth.IsValid, "RateLimiting:Global and RateLimiting:Auth need a PermitLimit > 0 and a Window > 0.")
            .ValidateOnStart();
        services.AddRateLimiter(_ => { });
        services.AddOptions<RateLimiterOptions>().Configure<IOptions<RateLimitSettings>>((options, settings) =>
        {
            var global = settings.Value.Global;
            var auth = settings.Value.Auth;
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    IsHealthPath(context.Request.Path)
                        ? RateLimitPartition.GetNoLimiter("health")
                        : RateLimitPartition.GetFixedWindowLimiter($"global:{ClientKey(context)}", _ => FixedWindow(global))),
                PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    IsAuthPath(context.Request.Path)
                        ? RateLimitPartition.GetFixedWindowLimiter($"auth:{ClientKey(context)}", _ => FixedWindow(auth))
                        : RateLimitPartition.GetNoLimiter("outside-auth")));
            options.OnRejected = async (context, _) =>
            {
                var fallback = IsAuthPath(context.HttpContext.Request.Path) ? auth.Window : global.Window;
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var hint) ? hint : fallback;
                var seconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
                context.HttpContext.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
                // Cuerpo uniforme: sin IP, clave de partición, límite ni contador.
                await ProblemResponses.WriteAsync(context.HttpContext, StatusCodes.Status429TooManyRequests, "Too Many Requests");
            };
        });
        return services;
    }

    private static FixedWindowRateLimiterOptions FixedWindow(RateLimitPolicySettings policy) => new()
    {
        PermitLimit = policy.PermitLimit,
        Window = policy.Window,
        QueueLimit = 0,
        AutoReplenishment = true,
    };

    private static bool IsHealthPath(PathString path) =>
        path.Equals(_healthPath, StringComparison.OrdinalIgnoreCase) || path.Equals(_readyPath, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// <c>/api/v{version}/auth</c> y lo que cuelga de ahí. Toda la comparación ignora mayúsculas porque el router de ASP.NET Core
    /// no las distingue: <c>/api/V1/AUTH/firebase</c> llega al mismo endpoint y debe contar para el límite estricto.
    /// </summary>
    private static bool IsAuthPath(PathString path)
    {
        var segments = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? [];
        return segments.Length >= 3
            && segments[0].Equals("api", StringComparison.OrdinalIgnoreCase)
            && (segments[1][0] is 'v' or 'V')
            && segments[2].Equals("auth", StringComparison.OrdinalIgnoreCase);
    }

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
