using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Knock.Gateway.Hosting;

/// <summary>Una política de límite: peticiones por ventana fija.</summary>
internal sealed class RateLimitPolicy
{
    public int PermitLimit { get; set; }

    public TimeSpan Window { get; set; }

    public bool IsValid => PermitLimit > 0 && Window > TimeSpan.Zero;
}

/// <summary>Valores de <c>RateLimiting</c> (sin secretos): por IP antes de autenticar, estricto para <c>/auth</c>, por usuario después (spec 009, R-04) y propio de la búsqueda (spec 027, R-14).</summary>
internal sealed class RateLimitSettings
{
    public const string SectionName = "RateLimiting";

    public RateLimitPolicy Ip { get; set; } = new();

    public RateLimitPolicy Auth { get; set; } = new();

    public RateLimitPolicy User { get; set; } = new();

    public RateLimitPolicy Search { get; set; } = new();
}

/// <summary>Contador de ventana fija compartido entre réplicas. Existe para probar el límite con dobles sin Redis.</summary>
internal interface IRequestCounter
{
    /// <summary>Cuenta una petición en la ventana de <paramref name="key"/> y devuelve cuántas van y cuánto falta para que termine la ventana.</summary>
    Task<(long Count, TimeSpan Remaining)> HitAsync(string key, TimeSpan window, CancellationToken cancellationToken);
}

/// <summary>
/// Contadores en Redis (<c>ratelimit:*</c>) con un script: el incremento y la expiración de la ventana van juntos, así una caída entre los dos no deja
/// una clave sin vencimiento que bloquee a alguien para siempre.
/// </summary>
internal sealed class RedisRequestCounter(IConnectionMultiplexer redis) : IRequestCounter
{
    private const string _script = """
        local count = redis.call('INCR', KEYS[1])
        if count == 1 then redis.call('PEXPIRE', KEYS[1], ARGV[1]) end
        return {count, redis.call('PTTL', KEYS[1])}
        """;

    public async Task<(long Count, TimeSpan Remaining)> HitAsync(string key, TimeSpan window, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = (long[]?)await redis.GetDatabase().ScriptEvaluateAsync(
            _script,
            [new RedisKey(key)],
            [new RedisValue(((long)window.TotalMilliseconds).ToString(CultureInfo.InvariantCulture))])
            ?? throw new InvalidOperationException("Redis returned no counter.");
        var remaining = result[1] > 0 ? TimeSpan.FromMilliseconds(result[1]) : window;
        return (result[0], remaining);
    }
}

internal static class RateLimitingExtensions
{
    public static IServiceCollection AddKnockGatewayRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<RateLimitSettings>()
            .BindConfiguration(RateLimitSettings.SectionName)
            .Validate(
                s => s.Ip.IsValid && s.Auth.IsValid && s.User.IsValid && s.Search.IsValid,
                "RateLimiting:Ip, RateLimiting:Auth, RateLimiting:User and RateLimiting:Search need a PermitLimit > 0 and a Window > 0.")
            .ValidateOnStart();
        services.AddSingleton<IRequestCounter, RedisRequestCounter>();
        return services;
    }

    /// <summary>Es <c>/api/v{version}/{segment}</c> o lo que cuelga de ahí; sin distinguir mayúsculas porque el enrutador tampoco las distingue.</summary>
    internal static bool IsApiPath(PathString path, string segment)
    {
        var segments = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? [];
        return segments.Length >= 3
            && segments[0].Equals("api", StringComparison.OrdinalIgnoreCase)
            && segments[1][0] is 'v' or 'V'
            && segments[2].Equals(segment, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>IP del cliente: la de la conexión (ya sustituida por la reenviada solo si viene de un proxy de confianza). IPv4 mapeada se normaliza y las IPv6 se agrupan por /64.</summary>
    internal static string ClientKey(HttpContext context)
    {
        var address = context.Connection.RemoteIpAddress;
        if (address is null)
        {
            return "unknown";
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

/// <summary>
/// Límite por IP, antes de autenticar: política general para toda ruta salvo <c>/health</c> y la estricta, además, para <c>/api/v{n}/auth/*</c>.
/// Responde 429 con <c>Retry-After</c> en segundos (spec 009, R-09). Si Redis no responde, responde 503: sin contadores no se puede limitar.
/// </summary>
internal sealed class IpRateLimitMiddleware(RequestDelegate next, IRequestCounter counter, IOptions<RateLimitSettings> options)
{
    private static readonly PathString _health = new("/health");

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Request.Path.Equals(_health, StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var settings = options.Value;
        var client = RateLimitingExtensions.ClientKey(context);
        if (await RateLimitCheck.RejectedAsync(context, counter, $"ratelimit:ip:{client}", settings.Ip)
            || (IsAuthPath(context.Request.Path) && await RateLimitCheck.RejectedAsync(context, counter, $"ratelimit:auth:{client}", settings.Auth)))
        {
            return;
        }

        await next(context);
    }

    /// <summary>
    /// <c>/api/v{version}/auth</c> y lo que cuelga de ahí; sin distinguir mayúsculas porque el enrutador tampoco las distingue.
    /// </summary>
    internal static bool IsAuthPath(PathString path) => RateLimitingExtensions.IsApiPath(path, "auth");
}

/// <summary>
/// Límite por usuario (<c>sub</c> del JWT), después de autenticar; las peticiones sin sesión ya las limitó la IP (spec 009, R-04). La búsqueda de expertos
/// (<c>/api/v{n}/busqueda</c>) suma su propio contador por <c>sub</c> (spec 027, R-14).
/// </summary>
internal sealed class UserRateLimitMiddleware(RequestDelegate next, IRequestCounter counter, IOptions<RateLimitSettings> options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var sub = context.User.Identity?.IsAuthenticated == true ? context.User.FindFirst("sub")?.Value : null;
        if (sub is not null
            && (await RateLimitCheck.RejectedAsync(context, counter, $"ratelimit:user:{sub}", options.Value.User)
                || (RateLimitingExtensions.IsApiPath(context.Request.Path, "busqueda") && await RateLimitCheck.RejectedAsync(context, counter, $"ratelimit:busqueda:{sub}", options.Value.Search))))
        {
            return;
        }

        await next(context);
    }
}

internal static class RateLimitCheck
{
    /// <summary>Cuenta la petición; si supera el límite escribe el 429 (o el 503 si Redis no responde) y devuelve <c>true</c>. El cuerpo no revela IP, clave, límite ni contador.</summary>
    public static async Task<bool> RejectedAsync(HttpContext context, IRequestCounter counter, string key, RateLimitPolicy policy)
    {
        (long Count, TimeSpan Remaining) hit;
        try
        {
            hit = await counter.HitAsync(key, policy.Window, context.RequestAborted);
        }
        catch (Exception exception) when (exception is RedisException or TimeoutException)
        {
            context.Response.Headers.RetryAfter = "5";
            await ProblemResponses.WriteAsync(context, StatusCodes.Status503ServiceUnavailable, "Service Unavailable");
            return true;
        }

        if (hit.Count <= policy.PermitLimit)
        {
            return false;
        }

        var seconds = Math.Max(1, (int)Math.Ceiling(hit.Remaining.TotalSeconds));
        context.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
        await ProblemResponses.WriteAsync(context, StatusCodes.Status429TooManyRequests, "Too Many Requests");
        return true;
    }
}
