using Knock.Contracts.Sessions;
using Knock.Identity.Api.Modules.Account.Services;
using StackExchange.Redis;

namespace Knock.Identity.Api.Modules.Account.Data;

/// <summary>Clave <c>sesion-cancelada:{sid}</c> con tiempo de vida, la misma que escribe el cierre de sesión (spec 016) y lee el gateway.</summary>
internal sealed class RedisSessionCanceller(IConnectionMultiplexer redis) : ISessionCanceller
{
    public async Task CancelAsync(string sessionId, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        cancellationToken.ThrowIfCancellationRequested();
        await redis.GetDatabase().StringSetAsync(SessionRevocationKey.Prefix + sessionId, "1", lifetime);
    }
}
