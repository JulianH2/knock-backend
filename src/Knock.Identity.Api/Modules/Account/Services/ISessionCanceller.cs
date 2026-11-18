using Knock.Contracts.Sessions;
using StackExchange.Redis;

namespace Knock.Identity.Api.Modules.Account.Services;

/// <summary>Cancela sesiones en la lista de Redis que lee el gateway (spec 016): igual que el cierre de sesión, con la clave de <see cref="SessionRevocationKey"/>.</summary>
internal interface ISessionCanceller
{
    Task CancelAsync(string sessionId, TimeSpan lifetime, CancellationToken cancellationToken);
}

internal sealed class RedisSessionCanceller(IConnectionMultiplexer redis) : ISessionCanceller
{
    public async Task CancelAsync(string sessionId, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        cancellationToken.ThrowIfCancellationRequested();
        await redis.GetDatabase().StringSetAsync(SessionRevocationKey.Prefix + sessionId, "1", lifetime);
    }
}
