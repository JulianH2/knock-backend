using Knock.Api.Modules.Identity.Domain;

namespace Knock.Api.Modules.Identity.Data;

/// <summary>Lo que el canje necesita de la base de identidad; existe para probar el flujo con dobles sin MongoDB.</summary>
internal interface IIdentityStore
{
    Task<UserAccount?> FindBySubAsync(string sub, CancellationToken cancellationToken);

    Task<UserAccount?> FindByPhoneHashAsync(byte[] phoneHash, CancellationToken cancellationToken);

    Task<AccountCreation> CreateAccountAsync(NewAccount account, CancellationToken cancellationToken);

    Task AddRefreshTokenAsync(RefreshTokenRecord refresh, CancellationToken cancellationToken);
}
