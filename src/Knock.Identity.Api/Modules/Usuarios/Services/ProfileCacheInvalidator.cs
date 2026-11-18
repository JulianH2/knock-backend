using Knock.Identity.Api.Modules.Usuarios.Data;
using StackExchange.Redis;

namespace Knock.Identity.Api.Modules.Usuarios.Services;

/// <summary>Borra el perfil de la caché cuando otro módulo cambia el estado de la cuenta (spec 039). Un fallo de Redis no deshace una escritura ya confirmada.</summary>
internal sealed class ProfileCacheInvalidator(IProfileCache cache)
{
    /// <returns>Falso si Redis no respondió (el perfil caduca solo por su tiempo de vida).</returns>
    public async Task<bool> TryForgetAsync(string sub, CancellationToken cancellationToken)
    {
        try
        {
            await cache.RemoveAsync(sub, cancellationToken);
            return true;
        }
        catch (Exception exception) when (exception is RedisException or TimeoutException)
        {
            return false;
        }
    }
}
