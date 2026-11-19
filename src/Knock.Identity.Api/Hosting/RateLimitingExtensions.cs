// jscpd:ignore-start -- EXC-015
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Knock.Identity.Api.Hosting;

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

    /// <summary>Política propia de <c>/health/ready</c> (hace un ping a MongoDB por petición anónima, así que no puede quedar sin límite).</summary>
    public RateLimitPolicySettings Ready { get; set; } = new();
}

internal static class RateLimitingExtensions
{
    /// <summary>Spec 039: comprobar disponibilidad del número nuevo (USR-03), 3 por hora por persona (R-20).</summary>
    public const string PhoneChangeCheckPolicy = "cambio-numero-comprobar";

    /// <summary>Spec 039: confirmar el cambio de número (USR-04). Valor propuesto: la spec no fija límite, solo registra el 429.</summary>
    public const string PhoneChangeConfirmPolicy = "cambio-numero-confirmar";

    /// <summary>Spec 039: recuperación de acceso (USR-05), 3 por hora por IP (R-21); el límite por número lo aplica el servicio.</summary>
    public const string AccessRecoveryPolicy = "recuperacion-acceso";

    /// <summary>Spec 039: pedir una exportación (USR-06). Valor propuesto: la spec no fija límite, solo registra el 429.</summary>
    public const string ExportPolicy = "exportaciones";

    /// <summary>Spec 039: programar la eliminación de la cuenta (USR-10). Valor propuesto: la spec no fija límite, solo registra el 429.</summary>
    public const string DeletionPolicy = "eliminacion";

    private const string _healthPath = "/health";
    private const string _readyPath = "/health/ready";
    private const string _unknownClient = "unknown";

    /// <summary>
    /// Límite por IP con el limitador del framework: política global (cualquier ruta salvo <c>/health</c> y <c>/health/ready</c>), política estricta
    /// para <c>/api/v{n}/auth/*</c> y política propia para <c>/health/ready</c>; <c>/health</c> es la única exención. Usa Redis compartido y respaldo en memoria por réplica. Responde 429 en ProblemDetails.
    /// </summary>
    public static IServiceCollection AddKnockRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<RateLimitSettings>()
            .BindConfiguration(RateLimitSettings.SectionName)
            .Validate(
                s => s.Global.IsValid && s.Auth.IsValid && s.Ready.IsValid,
                "RateLimiting:Global, RateLimiting:Auth and RateLimiting:Ready need a PermitLimit > 0 and a Window > 0.")
            .ValidateOnStart();
        services.AddRateLimiter(_ => { });
        services.AddOptions<RateLimiterOptions>().Configure<IOptions<RateLimitSettings>, IServiceProvider>((options, settings, services) =>
        {
            var global = settings.Value.Global;
            var auth = settings.Value.Auth;
            var ready = settings.Value.Ready;
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(PhoneChangeCheckPolicy, SubjectPolicy(3, TimeSpan.FromHours(1), services));
            options.AddPolicy(PhoneChangeConfirmPolicy, SubjectPolicy(5, TimeSpan.FromHours(1), services));
            options.AddPolicy(AccessRecoveryPolicy, context => SharedPartition(
                $"recuperacion:{ClientKey(context)}", new RateLimitPolicySettings { PermitLimit = 3, Window = TimeSpan.FromHours(1) }, services));
            options.AddPolicy(ExportPolicy, SubjectPolicy(3, TimeSpan.FromHours(1), services));
            options.AddPolicy(DeletionPolicy, SubjectPolicy(5, TimeSpan.FromHours(1), services));
            options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                PartitionedRateLimiter.Create<HttpContext, string>(context => GlobalPartition(context, global, ready, services)),
                PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    IsAuthPath(context.Request.Path)
                        ? SharedPartition($"auth:{ClientKey(context)}", auth, services)
                        : RateLimitPartition.GetNoLimiter("outside-auth")));
            options.OnRejected = async (context, _) =>
            {
                var fallback = WindowOf(context.HttpContext.Request.Path, global, auth, ready);
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var hint) ? hint : fallback;
                var seconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
                context.HttpContext.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
                // Cuerpo uniforme: sin IP, clave de partición, límite ni contador.
                await ProblemResponses.WriteAsync(context.HttpContext, StatusCodes.Status429TooManyRequests, "Too Many Requests");
            };
        });
        return services;
    }

    /// <summary>Política por <c>sub</c> del token; sin <c>sub</c> no limita (la autenticación ya respondió).</summary>
    private static Func<HttpContext, RateLimitPartition<string>> SubjectPolicy(int permitLimit, TimeSpan window, IServiceProvider services) =>
        context => context.User.FindFirst("sub")?.Value is not { Length: > 0 } subject
            ? RateLimitPartition.GetNoLimiter(_unknownClient)
            : SharedPartition($"persona:{subject}", new RateLimitPolicySettings { PermitLimit = permitLimit, Window = window }, services);

    /// <summary>Partición del límite general: <c>/health</c> sin límite, <c>/health/ready</c> con el suyo y el resto con el global.</summary>
    private static RateLimitPartition<string> GlobalPartition(HttpContext context, RateLimitPolicySettings global, RateLimitPolicySettings ready, IServiceProvider services)
    {
        var path = context.Request.Path;
        if (IsHealthPath(path))
        {
            return RateLimitPartition.GetNoLimiter("health");
        }

        return IsReadyPath(path)
            ? SharedPartition($"ready:{ClientKey(context)}", ready, services)
            : SharedPartition($"global:{ClientKey(context)}", global, services);
    }

    /// <summary>Ventana de la política que aplica a la ruta (respaldo de <c>Retry-After</c> cuando el limitador no da una pista).</summary>
    private static TimeSpan WindowOf(PathString path, RateLimitPolicySettings global, RateLimitPolicySettings auth, RateLimitPolicySettings ready)
    {
        if (IsAuthPath(path))
        {
            return auth.Window;
        }

        return IsReadyPath(path) ? ready.Window : global.Window;
    }

    private static RateLimitPartition<string> SharedPartition(string key, RateLimitPolicySettings policy, IServiceProvider services) =>
        RateLimitPartition.Get(key, partition => new SharedRateLimiter(services, partition, policy));

    private static bool IsHealthPath(PathString path) => path.Equals(_healthPath, StringComparison.OrdinalIgnoreCase);

    private static bool IsReadyPath(PathString path) => path.Equals(_readyPath, StringComparison.OrdinalIgnoreCase);

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
// jscpd:ignore-end
