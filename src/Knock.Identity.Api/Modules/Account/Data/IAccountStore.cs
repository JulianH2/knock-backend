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

    /// <summary>Cuenta que tiene ese HMAC de celular; <c>null</c> si ninguna.</summary>
    Task<AccountSnapshot?> FindByPhoneHashAsync(byte[] phoneHash, CancellationToken cancellationToken);

    /// <summary>
    /// Crea el caso de recuperación en estado <c>pendiente</c> salvo que la persona ya tenga uno pendiente (la regla de estado evita duplicados por un
    /// reintento): en ese caso conserva el primero y no escribe nada. Devuelve si se creó uno nuevo.
    /// </summary>
    Task<bool> CreateRecoveryCaseIfNoneOpenAsync(RecoveryCase recoveryCase, CancellationToken cancellationToken);

    /// <summary>
    /// Marca la exportación en curso con una sola operación condicionada a que no haya otra (una por persona, spec 039 R-22): si ya hay una, no escribe
    /// nada. La marca la quita la tarea <c>PrepararExportacion</c> (spec 054) al terminar.
    /// </summary>
    Task<ExportStartOutcome> TryStartExportAsync(string userId, DateTime now, CancellationToken cancellationToken);

    /// <summary>Quita la marca de exportación en curso (se usa si la tarea no se pudo encolar).</summary>
    Task ClearExportAsync(string userId, CancellationToken cancellationToken);
}
