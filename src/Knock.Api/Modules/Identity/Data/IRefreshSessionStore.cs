using Knock.Api.Modules.Identity.Domain;

namespace Knock.Api.Modules.Identity.Data;

/// <summary>Lo que la renovación (HU-003) y el cierre de sesión (HU-004) necesitan de <c>refresh_tokens</c>; existe para probar sus reglas con dobles.</summary>
internal interface IRefreshSessionStore
{
    /// <summary>
    /// Consume el refresh token con esa huella y guarda el siguiente de la misma familia, en una transacción. Si ya estaba reemplazado o revocado
    /// revoca su familia (<see cref="RotationStatus.Reused"/>); si no existe o venció no toca nada (<see cref="RotationStatus.Missing"/>).
    /// </summary>
    Task<RefreshRotation> RotateAsync(byte[] fingerprint, byte[] nextFingerprint, DateTime now, CancellationToken cancellationToken);

    /// <summary>Revoca todos los refresh tokens de la familia que todavía no estaban revocados.</summary>
    Task RevokeFamilyAsync(string familyId, string reason, DateTime now, CancellationToken cancellationToken);

    /// <summary>Sesión del refresh token con esa huella (aunque esté reemplazado o revocado), o <c>null</c> si no existe. No escribe nada.</summary>
    Task<StoredRefresh?> FindByFingerprintAsync(byte[] fingerprint, CancellationToken cancellationToken);

    /// <summary>Cuenta por su <c>_id</c> (ObjectId en hexadecimal).</summary>
    Task<UserAccount?> FindByIdAsync(string userId, CancellationToken cancellationToken);
}
