namespace Knock.Api.Modules.Identity.Domain;

/// <summary>Estados de una cuenta (spec HU-001 §5). Otros estados (eliminación pendiente) los agregan sus historias.</summary>
internal static class UserState
{
    public const string Active = "activo";
    public const string Suspended = "suspendido";
}

/// <summary>
/// Documentos que la persona acepta al registrarse y su versión (decisión 8 del líder): constantes, no configuración. La versión es la fecha
/// <c>AAAA-MM-DD</c> de publicación del texto y debe coincidir con la página de privacidad de TT-WEB-02. <b>Valores provisionales</b>: el
/// real lo confirma quien tenga el texto publicado (D7); cambiarlos es una línea.
/// </summary>
internal static class ConsentDocuments
{
    public const string PrivacyNotice = "aviso_privacidad";
    public const string Terms = "terminos";

    public const string PrivacyNoticeVersion = "2026-10-01";
    public const string TermsVersion = "2026-10-01";
}

/// <summary>Eventos de auditoría de la base de identidad (solo inserción).</summary>
internal static class AuditEvents
{
    public const string UserRegistered = "USER_REGISTERED";
}

/// <summary>Cuenta tal como la necesita el canje: sin el celular, solo el hash para compararlo.</summary>
/// <param name="Id"><c>_id</c> (ObjectId) en hexadecimal; es interno de Identity y no sale del módulo.</param>
/// <param name="Sub">Identificador público de la cuenta (UUID v8, 36 caracteres).</param>
/// <param name="State"><see cref="UserState"/>.</param>
/// <param name="ProfileComplete">Si ya completó el perfil (lo cambia HU-002).</param>
/// <param name="PhoneHash">HMAC-SHA256 del celular.</param>
internal sealed record UserAccount(string Id, string Sub, string State, bool ProfileComplete, byte[] PhoneHash);

/// <summary>Huella de una sesión (decisión 3): el refresh token nunca se guarda, solo su SHA-256.</summary>
/// <param name="UserId"><c>_id</c> (ObjectId en hexadecimal) del usuario dueño de la sesión.</param>
/// <param name="FamilyId">Familia de sesión (ObjectId en hexadecimal); cada canje abre una nueva.</param>
/// <param name="Fingerprint">SHA-256 de los 32 bytes del refresh token.</param>
/// <param name="DeviceId">UUID que genera la app; no se autentica (D2).</param>
/// <param name="CreatedAt">Fecha UTC de emisión; vence 30 días después.</param>
internal sealed record RefreshTokenRecord(string UserId, string FamilyId, byte[] Fingerprint, string DeviceId, DateTime CreatedAt)
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    public DateTime ExpiresAt => CreatedAt + Lifetime;
}

/// <summary>Alta de una cuenta nueva: se guarda entera o no se guarda nada (usuario, dos consentimientos, auditoría y huella).</summary>
internal sealed record NewAccount(
    string Id,
    string Sub,
    byte[] PhoneCiphertext,
    int PhoneCiphertextVersion,
    byte[] PhoneHash,
    int PhoneHashVersion,
    string DeviceId,
    string CorrelationId,
    string TraceId,
    DateTime CreatedAt,
    RefreshTokenRecord Refresh);

/// <summary>Resultado del alta: si ya existía el <c>sub</c> o el celular, un índice único lo rechazó y no se guardó nada.</summary>
internal enum AccountCreation
{
    Created,
    AlreadyExists,
}
