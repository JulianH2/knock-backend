using StackExchange.Redis;

namespace Knock.Messaging.Api.Modules.Chat.Data;

/// <summary>
/// Resultado de cobrar un mensaje al límite (spec 026, R-14): si se permite y, si no, cuántos segundos faltan para que la ventana se renueve. <c>Unavailable</c> es verdadero cuando
/// Redis no respondió: sin Redis no hay límite, así que el mensaje no se retransmite (falla cerrado).
/// </summary>
internal sealed record ChatLimitResult(bool Allowed, int RetryAfterSeconds, bool Unavailable = false);

/// <summary>
/// Las llaves de Redis del chat (spec 026, §6): el contador <c>chat:limite:{sub}</c> con ventana fija y la marca <c>chat:visita-cerrada:{visitaId}</c>. Es el único punto del módulo con
/// acceso a Redis (constitución, principio 10). No guarda ningún mensaje. Un fallo de Redis no sale como excepción: se devuelve como «no disponible» para que el servicio falle cerrado.
/// </summary>
internal sealed class ChatRedisStore(IConnectionMultiplexer redis)
{
    public const string LimitPrefix = "chat:limite:";
    public const string ClosedPrefix = "chat:visita-cerrada:";
    public static readonly TimeSpan ClosedLifetime = TimeSpan.FromDays(30);

    // Incrementa y fija el vencimiento en una sola operación atómica (como los contadores del gateway): dos copias comparten el contador y no pueden duplicar el límite.
    private const string _incrementScript = """
        local count = redis.call('INCR', KEYS[1])
        if count == 1 then redis.call('EXPIRE', KEYS[1], ARGV[1]) end
        return {count, redis.call('TTL', KEYS[1])}
        """;

    /// <summary>Cuenta un mensaje de <paramref name="sub"/> en la ventana actual y dice si cabe en <paramref name="limit"/>.</summary>
    public async Task<ChatLimitResult> ChargeAsync(string sub, int limit, int windowSeconds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sub);
        try
        {
            var result = (long[]?)await redis.GetDatabase().ScriptEvaluateAsync(_incrementScript, [(RedisKey)(LimitPrefix + sub)], [windowSeconds]);
            if (result is not { Length: 2 })
            {
                return new ChatLimitResult(false, windowSeconds, Unavailable: true);
            }

            return new ChatLimitResult(result[0] <= limit, (int)Math.Clamp(result[1], 1, windowSeconds));
        }
        catch (Exception exception) when (exception is RedisException or TimeoutException)
        {
            return new ChatLimitResult(false, windowSeconds, Unavailable: true);
        }
    }

    /// <summary>Marca la visita como cerrada para el chat (cancelada, terminada, no presentada o bloqueada) durante 30 días (R-10).</summary>
    public async Task MarkClosedAsync(string visitId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(visitId);
        await redis.GetDatabase().StringSetAsync(ClosedPrefix + visitId, "1", ClosedLifetime);
    }

    /// <summary>Si la visita está marcada como cerrada; <see langword="null"/> si Redis no respondió (el llamador falla cerrado).</summary>
    public async Task<bool?> IsClosedAsync(string visitId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(visitId);
        try
        {
            return await redis.GetDatabase().KeyExistsAsync(ClosedPrefix + visitId);
        }
        catch (Exception exception) when (exception is RedisException or TimeoutException)
        {
            return null;
        }
    }
}
