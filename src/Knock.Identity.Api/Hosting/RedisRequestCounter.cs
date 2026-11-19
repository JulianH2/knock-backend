// jscpd:ignore-start -- EXC-025
// Copia temporal: se mueve a la biblioteca compartida en KNOCK-251
using System.Globalization;
using StackExchange.Redis;

namespace Knock.Identity.Api.Hosting;

internal sealed class RedisRequestCounter(IConnectionMultiplexer redis)
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
// jscpd:ignore-end
