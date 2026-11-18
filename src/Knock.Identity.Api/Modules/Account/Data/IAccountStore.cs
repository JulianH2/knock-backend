using Knock.Identity.Api.Modules.Account.Domain;

namespace Knock.Identity.Api.Modules.Account.Data;

/// <summary>Lo que la cuenta y los datos personales necesitan de <c>usuarios</c>, <c>sesiones</c> y <c>recuperaciones</c>; existe para probar los servicios con dobles sin MongoDB.</summary>
internal interface IAccountStore
{
    /// <summary>Cuenta por el <c>sub</c> del token; <c>null</c> si no existe.</summary>
    Task<AccountSnapshot?> FindBySubAsync(string sub, CancellationToken cancellationToken);

    /// <summary>Si algún usuario (cualquiera, también la persona que pregunta) ya tiene un celular con ese HMAC; usa el índice único <c>celularHash</c>.</summary>
    Task<bool> PhoneHashExistsAsync(byte[] phoneHash, CancellationToken cancellationToken);
}
