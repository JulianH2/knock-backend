using System.Globalization;
using StackExchange.Redis;

namespace Knock.Identity.Api.Modules.Panel.Data;

/// <summary>
/// Desafío de un solo uso (<c>panel:desafio:{id}</c>, hash con el <c>sub</c>) y fallos por IP (<c>panel:fallos-ip:{ip}</c>) en Redis (spec 033, §6, R-07, R-11).
/// El consumo y el contador usan scripts para que la lectura y el borrado, o el incremento y el vencimiento, sean una sola operación atómica.
/// </summary>
internal sealed class RedisPanelDesafios(IConnectionMultiplexer redis) : IPanelDesafios
{
    public const string ChallengePrefix = "panel:desafio:";
    public const string IpFailuresPrefix = "panel:fallos-ip:";

    private const string _subField = "sub";

    private const string _consumeScript = """
        local sub = redis.call('HGET', KEYS[1], 'sub')
        if sub then redis.call('DEL', KEYS[1]) end
        return sub
        """;

    private const string _failureScript = """
        local count = redis.call('INCR', KEYS[1])
        if count == 1 then redis.call('PEXPIRE', KEYS[1], ARGV[1]) end
        return count
        """;

    public static string ChallengeKey(string challengeId) => ChallengePrefix + challengeId;

    public static string IpKey(string ip) => IpFailuresPrefix + ip;

    public async Task CreateAsync(string challengeId, string sub, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var database = redis.GetDatabase();
        var key = ChallengeKey(challengeId);
        var batch = database.CreateBatch();
        var set = batch.HashSetAsync(key, [new HashEntry(_subField, sub)]);
        var expire = batch.KeyExpireAsync(key, lifetime);
        batch.Execute();
        await Task.WhenAll(set, expire);
    }

    public async Task<string?> PeekAsync(string challengeId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var value = await redis.GetDatabase().HashGetAsync(ChallengeKey(challengeId), _subField);
        return value.HasValue ? value.ToString() : null;
    }

    public async Task<string?> ConsumeAsync(string challengeId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = await redis.GetDatabase().ScriptEvaluateAsync(_consumeScript, [new RedisKey(ChallengeKey(challengeId))]);
        return result.IsNull ? null : result.ToString();
    }

    public async Task<long> IpFailuresAsync(string ip, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var value = await redis.GetDatabase().StringGetAsync(IpKey(ip));
        return value.HasValue && long.TryParse(value.ToString(), CultureInfo.InvariantCulture, out var count) ? count : 0;
    }

    public async Task RegisterIpFailureAsync(string ip, TimeSpan window, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await redis.GetDatabase().ScriptEvaluateAsync(
            _failureScript,
            [new RedisKey(IpKey(ip))],
            [new RedisValue(((long)window.TotalMilliseconds).ToString(CultureInfo.InvariantCulture))]);
    }
}

/// <summary>Caché de la cuenta (<c>panel:cuenta:{sub}</c>, 60 segundos, R-24). Solo roles, estado, versión y nombre: nada secreto.</summary>
internal sealed class RedisPanelCuentaCache(IConnectionMultiplexer redis) : IPanelCuentaCache
{
    public const string KeyPrefix = "panel:cuenta:";

    public static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(60);

    public static string Key(string sub) => KeyPrefix + sub;

    public async Task<CuentaPanelEnCache?> GetAsync(string sub, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var entries = await redis.GetDatabase().HashGetAllAsync(Key(sub));
        if (entries.Length == 0)
        {
            return null;
        }

        var map = entries.ToDictionary(entry => entry.Name.ToString(), entry => entry.Value.ToString(), StringComparer.Ordinal);
        if (!map.TryGetValue("roles", out var roles) || !map.TryGetValue("estado", out var state) || !map.TryGetValue("permisosVersion", out var version)
            || !map.TryGetValue("nombre", out var name) || !int.TryParse(version, CultureInfo.InvariantCulture, out var parsed))
        {
            return null;
        }

        return new CuentaPanelEnCache(roles.Split(',', StringSplitOptions.RemoveEmptyEntries), state, parsed, name);
    }

    public async Task SetAsync(string sub, CuentaPanelEnCache value, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(value);
        cancellationToken.ThrowIfCancellationRequested();
        var database = redis.GetDatabase();
        var key = Key(sub);
        var batch = database.CreateBatch();
        var set = batch.HashSetAsync(
            key,
            [
                new HashEntry("roles", string.Join(',', value.Roles)),
                new HashEntry("estado", value.Estado),
                new HashEntry("permisosVersion", value.PermisosVersion),
                new HashEntry("nombre", value.Nombre),
            ]);
        var expire = batch.KeyExpireAsync(key, Lifetime);
        batch.Execute();
        await Task.WhenAll(set, expire);
    }

    public async Task DeleteAsync(string sub, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await redis.GetDatabase().KeyDeleteAsync(Key(sub));
    }
}

/// <summary>Escribe <c>sesion-cancelada:{sid}</c> con tiempo de vida: la lista nunca crece más de lo que se emite en 15 minutos.</summary>
internal sealed class RedisPanelSidCanceller(IConnectionMultiplexer redis) : IPanelSidCanceller
{
    public const string KeyPrefix = "sesion-cancelada:";

    public async Task CancelAsync(string sessionId, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        cancellationToken.ThrowIfCancellationRequested();
        await redis.GetDatabase().StringSetAsync(KeyPrefix + sessionId, "1", lifetime);
    }

    public async Task<bool> IsCancelledAsync(string sessionId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        cancellationToken.ThrowIfCancellationRequested();
        return await redis.GetDatabase().KeyExistsAsync(KeyPrefix + sessionId);
    }
}
