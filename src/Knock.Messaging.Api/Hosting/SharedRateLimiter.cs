using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using StackExchange.Redis;

namespace Knock.Messaging.Api.Hosting;

/// <summary>
/// Límite por persona con contador compartido en Redis (ventana fija, un solo script atómico): dos copias del servicio comparten el mismo contador y no duplican el límite. Si Redis no
/// responde, la copia usa su propio contador en memoria (límite más laxo pero nunca sin límite). La clave de Redis es un hash de la partición: nunca lleva el <c>sub</c> en claro.
/// </summary>
internal sealed class SharedRateLimiter : RateLimiter
{
    private const string _script = """
        local count = redis.call('INCR', KEYS[1])
        if count == 1 then redis.call('PEXPIRE', KEYS[1], ARGV[1]) end
        return {count, redis.call('PTTL', KEYS[1])}
        """;

    private static readonly Action<ILogger, Exception?> _unavailable = LoggerMessage.Define(LogLevel.Warning, new EventId(247, "RateLimitFallback"), "Redis rate limiting unavailable; using per-instance limit.");

    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private readonly string _key;
    private readonly RateLimitPolicySettings _policy;
    private readonly FixedWindowRateLimiter _fallback;

    public SharedRateLimiter(IServiceProvider services, string partition, RateLimitPolicySettings policy)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(policy);
        _services = services;
        _logger = services.GetRequiredService<ILoggerFactory>().CreateLogger<SharedRateLimiter>();
        _key = "ratelimit:messaging:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(partition)));
        _policy = policy;
        _fallback = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = policy.PermitLimit,
            Window = policy.Window,
            QueueLimit = 0,
            AutoReplenishment = true,
        });
    }

    public override TimeSpan? IdleDuration => _fallback.IdleDuration;

    public override RateLimiterStatistics? GetStatistics() => _fallback.GetStatistics();

    protected override RateLimitLease AttemptAcquireCore(int permitCount) => new SharedLease(false, _policy.Window);

    protected override async ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken)
    {
        var redis = _services.GetService<IConnectionMultiplexer>();
        if (redis is null || !redis.IsConnected)
        {
            return await _fallback.AcquireAsync(permitCount, cancellationToken);
        }

        try
        {
            var result = (long[]?)await redis.GetDatabase().ScriptEvaluateAsync(
                _script,
                [new RedisKey(_key)],
                [new RedisValue(((long)_policy.Window.TotalMilliseconds).ToString(CultureInfo.InvariantCulture))]);
            if (result is not { Length: 2 })
            {
                return await UseFallbackAsync(permitCount, cancellationToken);
            }

            var remaining = result[1] > 0 ? TimeSpan.FromMilliseconds(result[1]) : _policy.Window;
            return new SharedLease(result[0] <= _policy.PermitLimit, remaining);
        }
        catch (Exception error) when (error is RedisException or TimeoutException)
        {
            return await UseFallbackAsync(permitCount, cancellationToken);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fallback.Dispose();
        }

        base.Dispose(disposing);
    }

    private ValueTask<RateLimitLease> UseFallbackAsync(int permitCount, CancellationToken cancellationToken)
    {
        _unavailable(_logger, null);
        return _fallback.AcquireAsync(permitCount, cancellationToken);
    }

    private sealed class SharedLease(bool acquired, TimeSpan remaining) : RateLimitLease
    {
        public override bool IsAcquired => acquired;

        public override IEnumerable<string> MetadataNames => [MetadataName.RetryAfter.Name];

        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            metadata = metadataName == MetadataName.RetryAfter.Name ? remaining : null;
            return metadata is not null;
        }
    }
}
