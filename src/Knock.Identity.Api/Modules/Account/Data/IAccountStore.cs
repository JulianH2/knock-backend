using Knock.Identity.Api.Modules.Account.Domain;
using Knock.Identity.Api.Modules.Sesiones.Services;

namespace Knock.Identity.Api.Modules.Account.Data;

/// <summary>Lo que la cuenta y los datos personales necesitan de <c>usuarios</c>, <c>sesiones</c> y <c>recuperaciones</c>; existe para probar los servicios con dobles sin MongoDB.</summary>
internal interface IAccountStore
{
    /// <summary>Cuenta por el <c>sub</c> del token; <c>null</c> si no existe.</summary>
    Task<AccountSnapshot?> FindBySubAsync(string sub, CancellationToken cancellationToken);

    /// <summary>Si algún usuario (cualquiera, también la persona que pregunta) ya tiene un celular con ese HMAC; usa el índice único <c>celularHash</c>.</summary>
    Task<bool> PhoneHashExistsAsync(byte[] phoneHash, CancellationToken cancellationToken);

    /// <summary>Ids de las sesiones abiertas de la cuenta salvo la actual (la desde la que se cambia el número).</summary>
    Task<IReadOnlyList<string>> FindOtherOpenSessionIdsAsync(string userId, string currentSessionId, DateTime now, CancellationToken cancellationToken);

    /// <summary>
    /// Guarda el celular nuevo (cifrado y HMAC) y revoca las demás sesiones abiertas en una sola transacción. Si el índice único de <c>celularHash</c>
    /// lo rechaza (otra cuenta lo tiene) no cambia nada. Devuelve los ids de las sesiones revocadas para cancelarlos en Redis.
    /// </summary>
    Task<(PhoneChangeOutcome Outcome, IReadOnlyList<string> RevokedSessionIds)> ChangePhoneAsync(
        string userId, ProtectedPhone phone, PhoneHash hash, string currentSessionId, DateTime now, CancellationToken cancellationToken);
}
