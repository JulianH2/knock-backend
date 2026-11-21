using System.Text.RegularExpressions;

namespace Knock.Identity.Api.Modules.Trust.Domain;

/// <summary>Niveles de verificación (spec 025, R-03): <c>basica</c> pide INE frente y selfie; <c>completa</c>, además el reverso.</summary>
internal static class VerificationLevels
{
    public const string Basic = "basica";
    public const string Full = "completa";

    public static bool IsKnown(string? level) => level is Basic or Full;

    /// <summary>Tipos de foto que pide cada nivel, en el orden en que la app los captura.</summary>
    public static IReadOnlyList<string> PhotosFor(string level) =>
        level == Full ? [PhotoTypes.IdFront, PhotoTypes.IdBack, PhotoTypes.Selfie] : [PhotoTypes.IdFront, PhotoTypes.Selfie];
}

/// <summary>Estados de una verificación (spec 025, §5).</summary>
internal static class VerificationStates
{
    public const string NotVerified = "sin_verificar";
    public const string Capturing = "en_captura";
    public const string UnderReview = "en_revision";
    public const string Verified = "verificada";
    public const string Rejected = "no_verificada";
}

internal static class PhotoTypes
{
    public const string IdFront = "ine_frente";
    public const string IdBack = "ine_reverso";
    public const string Selfie = "selfie";
}

internal static class PhotoStates
{
    public const string Pending = "pendiente";
    public const string Encrypted = "cifrada";
    public const string Deleted = "borrada";
}

/// <summary>Códigos de negocio de la spec 025 (§5): en inglés y en snake_case, sin datos de la persona.</summary>
internal static class VerificationErrors
{
    public const string ConsentRequiredCode = "consent_required";
    public const string CurpInvalidCode = "curp_invalid";
    public const string UnderReviewCode = "verification_under_review";
    public const string AlreadyVerifiedCode = "already_verified";
    public const string AlreadyFullyVerifiedCode = "already_fully_verified";
}

/// <summary>
/// Motivos de un rechazo o de un borrado y sus consejos (spec 025, R-15 y §14 punto 9): un catálogo del servidor, para que la app no invente motivos y el panel
/// (HU-067) elija de una lista. Cada motivo trae de uno a tres consejos.
/// </summary>
internal static class VerificationReasons
{
    public const string PhotosDeleted = "fotos_borradas";
    public const string CaptureReplaced = "captura_reemplazada";

    private static readonly Dictionary<string, string[]> _tips = new(StringComparer.Ordinal)
    {
        ["foto_ilegible"] = ["Toma la foto con buena luz y sin reflejos.", "Que se lean todos los datos de la INE."],
        ["datos_no_coinciden"] = ["Usa tu INE vigente.", "Revisa que la CURP sea la de tu INE."],
        ["selfie_no_coincide"] = ["Toma la selfie de frente, sin lentes ni gorra.", "Que tu cara se vea completa y con buena luz."],
        ["documento_no_valido"] = ["Usa una INE original, no una copia ni una foto de pantalla."],
        [PhotosDeleted] = ["Tus fotos se borraron porque nadie las revisó en 7 días. Puedes enviarlas de nuevo."],
    };

    public static IReadOnlyList<string> TipsFor(string? reason) =>
        reason is not null && _tips.TryGetValue(reason, out var tips) ? tips : [];
}

/// <summary>
/// Formato de la CURP (spec 025, R-04): cuatro letras, seis dígitos de fecha, <c>H</c> o <c>M</c>, dos letras de entidad, tres consonantes, un carácter alfanumérico
/// y un dígito. Se compara sin espacios a los lados y en mayúsculas. Esta clase nunca guarda ni devuelve la CURP.
/// </summary>
internal static partial class Curp
{
    public const int Length = 18;

    public static bool IsValid(string? curp) =>
        curp is not null && curp.Trim().Length == Length && Pattern().IsMatch(curp.Trim().ToUpperInvariant());

    [GeneratedRegex(@"\A[A-Z][AEIOUX][A-Z]{2}\d{2}(0[1-9]|1[0-2])(0[1-9]|[12]\d|3[01])[HM](AS|BC|BS|CC|CL|CM|CS|CH|DF|DG|GT|GR|HG|JC|MC|MN|MS|NT|NL|OC|PL|QT|QR|SP|SL|SR|TC|TS|TL|VZ|YN|ZS|NE)[B-DF-HJ-NP-TV-Z]{3}[0-9A-Z]\d\z",
        RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex Pattern();
}
