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

    /// <summary>Consentimientos guardados de la cuenta; <c>null</c> si la cuenta no existe.</summary>
    Task<IReadOnlyList<StoredConsent>?> FindConsentsAsync(string sub, CancellationToken cancellationToken);

    /// <summary>
    /// Activa o retira un consentimiento opcional guardando tipo, versión y fecha. Lee y escribe con la <c>version</c> del documento como condición y
    /// reintenta si otra escritura se adelantó, así dos cambios simultáneos no se pisan. <c>null</c> si la cuenta no existe.
    /// </summary>
    Task<StoredConsent?> SetOptionalConsentAsync(string sub, string type, bool active, string version, DateTime now, CancellationToken cancellationToken);

    /// <summary>Cuándo se abrió la sesión <paramref name="sessionId"/> de la cuenta (su <c>creadoEn</c>); <c>null</c> si no existe o no es de esa cuenta.</summary>
    Task<DateTime?> FindSessionStartAsync(string sessionId, string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Programa la eliminación con una sola operación condicionada a que la cuenta esté <c>activa</c>: pasa a <c>eliminacion_programada</c> con
    /// <c>eliminarEn</c> (el compuesto <c>estado + eliminarEn</c> es lo que lee el proceso de borrado). Falso si ya estaba programada o no está activa.
    /// </summary>
    Task<bool> ScheduleDeletionAsync(string userId, DateTime deleteAt, DateTime now, CancellationToken cancellationToken);

    /// <summary>
    /// Cancela la eliminación con una sola operación condicionada a que esté programada y su gracia no haya vencido: la cuenta vuelve a <c>activa</c> y
    /// se borra <c>eliminarEn</c>. Falso si no había nada que cancelar.
    /// </summary>
    Task<bool> CancelDeletionAsync(string userId, DateTime now, CancellationToken cancellationToken);
}
