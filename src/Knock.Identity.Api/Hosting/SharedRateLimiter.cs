// jscpd:ignore-start -- EXC-025
// Copia temporal: se mueve a la biblioteca compartida en KNOCK-251
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using StackExchange.Redis;

namespace Knock.Identity.Api.Hosting;

internal sealed class SharedRateLimiter : RateLimiter
{
    private static readonly Action<ILogger, Exception?> _unavailable = LoggerMessage.Define(LogLevel.Warning, new EventId(247, "RateLimitFallback"), "Redis rate limiting unavailable; using per-instance limit.");
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private readonly string _key;
    private readonly RateLimitPolicySettings _policy;
    private readonly FixedWindowRateLimiter _fallback;

    public SharedRateLimiter(IServiceProvider services, string partition, RateLimitPolicySettings policy)
    {
        _services = services;
        _logger = services.GetRequiredService<ILoggerFactory>().CreateLogger<SharedRateLimiter>();
        _key = "ratelimit:identity:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(partition)));
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
        try
        {
            var redis = _services.GetService<IConnectionMultiplexer>();
            if (redis is null || !redis.IsConnected)
            {
                return await _fallback.AcquireAsync(permitCount, cancellationToken);
            }

            var (count, remaining) = await new RedisRequestCounter(redis).HitAsync(_key, _policy.Window, cancellationToken);
            return new SharedLease(count <= _policy.PermitLimit, remaining);
        }
        catch (Exception error) when (error is RedisException or TimeoutException)
        {
            return await UseFallbackAsync(permitCount, cancellationToken);
        }
    }

    private ValueTask<RateLimitLease> UseFallbackAsync(int permitCount, CancellationToken cancellationToken)
    {
        _unavailable(_logger, null);
        return _fallback.AcquireAsync(permitCount, cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fallback.Dispose();
        }

        base.Dispose(disposing);
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
// jscpd:ignore-end
