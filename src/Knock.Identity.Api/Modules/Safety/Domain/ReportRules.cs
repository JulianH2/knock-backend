namespace Knock.Identity.Api.Modules.Safety.Domain;

/// <summary>Estados de un reporte (spec 043, §6). Son los valores que se guardan.</summary>
internal static class ReportStates
{
    public const string Open = "abierto";
    public const string Decided = "decidido";
    public const string Appealed = "en_apelacion";
}

/// <summary>Referencias que un reporte puede llevar: a una solicitud, una visita o una opinión (spec 043, R-20).</summary>
internal static class ReportReferenceKinds
{
    public const string Request = "solicitud";
    public const string Visit = "visita";
    public const string Review = "opinion";

    public static readonly IReadOnlyList<string> All = [Request, Visit, Review];
}

/// <summary>
/// Reglas puras de los reportes (spec 043, R-20 y R-21). La lista cerrada de motivos no la fija la spec: se elige la más corta que cubre los casos del documento (acoso, fraude, mal
/// servicio, comportamiento inapropiado, peligro y otro); <c>peligro</c> es el único que implica riesgo y por eso la respuesta trae el aviso de llamar al 911.
/// </summary>
internal static class ReportRules
{
    public const string DangerReason = "peligro";
    public const int MinTextLength = 8;
    public const int MaxTextLength = 500;
    public const int MaxEvidence = 5;

    public const string EmergencyNotice = "Si estás en peligro ahora mismo, llama al 911. Knock no es un servicio de emergencias.";

    public static readonly IReadOnlyList<string> Reasons = ["acoso", "fraude", "mal_servicio", "comportamiento_inapropiado", DangerReason, "otro"];

    /// <summary>El aviso de emergencia si el motivo implica peligro; si no, <see langword="null"/>.</summary>
    public static string? NoticeFor(string reason) => string.Equals(reason, DangerReason, StringComparison.Ordinal) ? EmergencyNotice : null;

    /// <summary>Texto recortado, sin caracteres de control salvo el salto de línea, de 8 a 500 caracteres.</summary>
    public static bool IsValidText(string? text) =>
        text?.Trim() is { } trimmed
        && trimmed.Length is >= MinTextLength and <= MaxTextLength
        && !trimmed.Any(c => char.IsControl(c) && c != '\n');

    /// <summary>Una apelación solo cabe sobre una decisión ya tomada y una sola vez (R-21).</summary>
    public static bool CanBeAppealed(string state, bool hasDecision, bool hasAppeal) => hasDecision && !hasAppeal && string.Equals(state, ReportStates.Decided, StringComparison.Ordinal);
}
