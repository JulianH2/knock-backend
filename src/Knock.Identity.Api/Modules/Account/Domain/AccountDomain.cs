using Knock.Identity.Api.Modules.Sesiones.Services;

namespace Knock.Identity.Api.Modules.Account.Domain;

/// <summary>Códigos de negocio de la spec 039 (§5): en inglés y en snake_case, sin datos de la persona.</summary>
internal static class AccountErrorCodes
{
    public const string PhoneAlreadyRegistered = "phone_already_registered";
    public const string ExportInProgress = "export_in_progress";
    public const string DeletionNotAllowed = "deletion_not_allowed";
    public const string NoDeletionScheduled = "no_deletion_scheduled";
    public const string MarketUnavailable = "market_unavailable";
}

/// <summary>Cuenta tal como la necesita el módulo: sin el celular, solo el hash para compararlo (spec 039, R-27: la persona es siempre la del <c>sub</c>).</summary>
/// <param name="Id"><c>_id</c> (ObjectId en hexadecimal); interno del servicio.</param>
/// <param name="Sub">Identificador público de la cuenta.</param>
/// <param name="State"><c>estado</c> de la cuenta (spec 015, R-07).</param>
/// <param name="IsTechnician">Marca de técnico (spec 015).</param>
/// <param name="PhoneHash">HMAC-SHA256 del celular vigente.</param>
/// <param name="DeleteAt">Cuándo vencen los 7 días de gracia; solo con la eliminación programada.</param>
internal sealed record AccountSnapshot(string Id, string Sub, string State, bool IsTechnician, byte[] PhoneHash, DateTime? DeleteAt)
{
    /// <summary>Sin datos: un registro imprimiría el hash del celular.</summary>
    public sealed override string ToString() => nameof(AccountSnapshot);
}

/// <summary>Consentimientos que se pueden activar o retirar (spec 039, R-23); los obligatorios no se retiran por esta ruta.</summary>
internal static class ConsentTypes
{
    public const string Promotions = "promociones";
    public const string Surveys = "encuestas";
    public const string Recommendations = "recomendaciones";
    public const string PrivacyNotice = "aviso_privacidad";
    public const string Terms = "terminos";

    public static readonly IReadOnlyList<string> Optional = [Promotions, Surveys, Recommendations];

    public static readonly IReadOnlyList<string> Mandatory = [PrivacyNotice, Terms];

    public static bool IsOptional(string? type) => type is not null && Optional.Contains(type, StringComparer.Ordinal);
}

/// <summary>Un consentimiento con su versión y fecha. Un opcional que la persona nunca activó aparece con <c>Active</c> falso y sin versión ni fecha.</summary>
internal sealed record ConsentEntry(string Type, bool Active, bool Mandatory, string? Version, DateTime? ChangedAt);

/// <summary>Versiones de los consentimientos opcionales (<c>Consentimientos:Opcionales</c>); el aviso y los términos usan las de <see cref="ConsentVersionsOptions"/>.</summary>
internal sealed class OptionalConsentOptions
{
    public const string SectionName = "Consentimientos";

    public string Opcionales { get; set; } = "1.0";
}

/// <summary>Resultado de cambiar el celular (spec 039, R-20): el índice único de <c>celularHash</c> decide si otra cuenta ya lo tiene.</summary>
internal enum PhoneChangeOutcome
{
    Changed,
    PhoneTaken,
}

/// <summary>Resultado de pedir una exportación: una sola en curso por persona (spec 039, R-22).</summary>
internal enum ExportStartOutcome
{
    Started,
    AlreadyInProgress,
}

/// <summary>Estados de un caso de recuperación (spec 039, §6).</summary>
internal static class RecoveryStates
{
    public const string Pending = "pendiente";
    public const string Approved = "aprobada";
    public const string Rejected = "rechazada";
    public const string Expired = "vencida";
}

/// <summary>Quién pide recuperar el acceso: el tipo de cuenta que dice tener (spec 039, R-21).</summary>
internal static class RecoveryTypes
{
    public const string Technician = "tecnico";
    public const string Client = "cliente";

    public static bool IsValid(string? type) => type is Technician or Client;
}

/// <summary>Caso de recuperación de acceso (spec 039, §6 <c>recuperaciones</c>): el celular nuevo se guarda protegido igual que en <c>usuarios</c>.</summary>
internal sealed record RecoveryCase(string UserId, ProtectedPhone NewPhone, PhoneHash NewPhoneHash, string Type, DateTime CreatedAt)
{
    /// <summary>Sin datos: un registro imprimiría el celular cifrado y su hash.</summary>
    public sealed override string ToString() => nameof(RecoveryCase);
}

/// <summary>Lo que Mercado reporta como abierto de una persona (spec 039, R-24): solo conteos e identificadores propios.</summary>
internal sealed record AccountPending(
    int AppointmentTotal,
    IReadOnlyList<string> AppointmentIds,
    int QuoteTotal,
    IReadOnlyList<string> QuoteIds,
    bool VisitInProgress);
