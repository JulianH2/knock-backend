using Knock.Identity.Api.Modules.Sesiones.Data;
using Knock.Identity.Api.Modules.Sesiones.Domain;

namespace Knock.Identity.Api.Modules.Sesiones.Services;

/// <summary>Implementación de <see cref="ISesionesApi"/> sobre el almacén de cuentas y el HMAC del celular.</summary>
internal sealed class SesionesApi(ISesionesStore store, PhoneProtector phones) : ISesionesApi
{
    public async Task<AppAccount?> FindAccountBySubAsync(string subject, CancellationToken cancellationToken)
    {
        var account = await store.FindBySubAsync(subject, cancellationToken);
        return account is null ? null : new AppAccount(account.Sub, account.State == AccountState.Suspended);
    }

    public async Task<string?> FindSubByPhoneAsync(string phone, CancellationToken cancellationToken)
    {
        var account = await store.FindByPhoneHashAsync(phones.Hash(phone).Value, cancellationToken);
        return account?.Sub;
    }
}
