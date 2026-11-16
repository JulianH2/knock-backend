using System.Globalization;

namespace Knock.Market.Api.Modules.Requests.Domain;

/// <summary>
/// <c>code</c> de negocio de la visita segura (spec 022, §5): en inglés y en <c>snake_case</c>, sin datos de la persona ni de la visita. Se declaran como constantes
/// <c>...Code</c> para que la prueba de convención de errores (spec 002, CA-07) los vea.
/// </summary>
internal static class VisitErrorCodes
{
    public const string WrongCode = "wrong_code";
    public const string CodeUsedCode = "code_used";
    public const string CodeExpiredCode = "code_expired";
    public const string VisitLockedCode = "visit_locked";
    public const string VisitCancelledCode = "visit_cancelled";
    public const string VisitNotActiveCode = "visit_not_active";
    public const string VisitWithoutCodeCode = "visit_without_code";
    public const string OutsideWindowCode = "outside_window";
    public const string VisitNotNotifiableCode = "visit_not_notifiable";
    public const string NotATechnicianCode = "not_a_technician";
}

/// <summary>Por qué un intento de llegada no se acepta y no cuenta como intento (spec 022, R-07 y R-09).</summary>
internal enum ArrivalBlock
{
    CodeUsed,
    CodeExpired,
    VisitLocked,
    VisitCancelled,
    VisitNotActive,
    VisitWithoutCode,
    OutsideWindow,
}

/// <summary>Los campos de la visita que deciden si se ve el código y si se puede llegar. Sin acceso a datos: lo arma el almacén.</summary>
internal sealed record VisitState(string Estado, string Modalidad, string Dia, bool Bloqueada, bool CodigoUsado, DateTime? CodigoVenceEnUtc);

/// <summary>Reglas puras de la visita segura (spec 022, R-03, R-05, R-07 y R-09): ventana de la cita, visibilidad del código y bloqueos de la llegada.</summary>
internal static class VisitRules
{
    public const int MaxAttempts = 3;
    public const string Confirmed = "confirmada";
    public const string InProgress = "en_curso";
    public const string Domicile = "domicilio";

    private const string _timeZone = "America/Mexico_City";

    /// <summary>Inicio de la ventana de la cita: las 00:00 del día de la cita en <c>America/Mexico_City</c>, en UTC (R-05).</summary>
    public static DateTime WindowStart(string day)
    {
        var start = DateOnly.ParseExact(day, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(start, TimeZoneInfo.FindSystemTimeZoneById(_timeZone));
    }

    /// <summary>Verdadero solo en domicilio, <c>confirmada</c>, sin usar ni bloquear y dentro de la ventana, que termina en <c>codigoVenceEn</c> (R-05).</summary>
    public static bool CanArrive(VisitState visit, DateTime nowUtc) =>
        visit.Modalidad == Domicile
        && visit.Estado == Confirmed
        && !visit.Bloqueada
        && !visit.CodigoUsado
        && visit.CodigoVenceEnUtc is { } expires
        && nowUtc >= WindowStart(visit.Dia)
        && nowUtc <= expires;

    /// <summary>Verdadero si se puede preparar el aviso de confianza: solo <c>confirmada</c> o <c>en_curso</c> (R-13).</summary>
    public static bool CanNotify(string estado) => estado is Confirmed or InProgress;

    /// <summary>
    /// Motivo por el que el código no se muestra al cliente (<c>no_aplica</c>, <c>bloqueada</c>, <c>usado</c> o <c>vencido</c>), o <see langword="null"/> si se muestra (R-03).
    /// </summary>
    public static string? CodeHiddenReason(VisitState visit, DateTime nowUtc)
    {
        if (visit.Modalidad != Domicile || !CanNotify(visit.Estado))
        {
            return "no_aplica";
        }

        if (visit.Bloqueada)
        {
            return "bloqueada";
        }

        if (visit.CodigoUsado)
        {
            return "usado";
        }

        return visit.CodigoVenceEnUtc is not { } expires || nowUtc > expires ? "vencido" : null;
    }

    /// <summary>
    /// Qué impide aceptar un intento de llegada, o <see langword="null"/> si se puede evaluar el código (R-07 y R-09). Ninguno de estos casos cuenta como intento. El orden es
    /// el de la spec: estado final, código usado, bloqueo, taller, vencimiento y, por último, ventana que aún no empieza.
    /// </summary>
    public static ArrivalBlock? BlockForArrival(VisitState visit, DateTime nowUtc)
    {
        if (visit.Estado == "cancelada")
        {
            return ArrivalBlock.VisitCancelled;
        }

        if (visit.Estado is "terminada" or "no_presentado")
        {
            return ArrivalBlock.VisitNotActive;
        }

        if (visit.Estado == InProgress || visit.CodigoUsado)
        {
            return ArrivalBlock.CodeUsed;
        }

        if (visit.Bloqueada)
        {
            return ArrivalBlock.VisitLocked;
        }

        if (visit.Modalidad != Domicile)
        {
            return ArrivalBlock.VisitWithoutCode;
        }

        if (visit.CodigoVenceEnUtc is not { } expires || nowUtc > expires)
        {
            return ArrivalBlock.CodeExpired;
        }

        return nowUtc < WindowStart(visit.Dia) ? ArrivalBlock.OutsideWindow : null;
    }

    /// <summary>Texto fijo de la regla de seguridad (R-04): el código solo lo dicta el técnico en la puerta.</summary>
    public static string SafetyRule(string technicianName) =>
        $"Pídele a {technicianName} que te diga este código en la puerta. Si no lo sabe, no abras.";

    /// <summary>
    /// Texto del aviso a una persona de confianza (R-12): nombre público del técnico, su verificación, día y hora, y la recomendación de pedir el código en la puerta.
    /// Nunca lleva el código ni la dirección.
    /// </summary>
    public static string TrustNotice(string technicianName, string verificationLevel, string day, string time)
    {
        var verification = verificationLevel switch
        {
            "completa" => "con verificación completa",
            "basica" => "con verificación básica",
            _ => "sin verificar",
        };
        return $"Contraté a {technicianName} ({verification}) para una visita el {day} a las {time}. Le pediré que me diga el código en la puerta; si no lo sabe, no le abriré. Te aviso cuando llegue.";
    }
}
