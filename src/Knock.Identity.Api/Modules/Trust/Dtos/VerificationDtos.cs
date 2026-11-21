namespace Knock.Identity.Api.Modules.Trust.Dtos;

/// <summary>
/// Cuerpo de <c>POST /me/verificacion</c> (spec 025, R-01). <c>aceptaBiometria</c> no tiene valor por omisión (R-02); la CURP solo se valida y nunca se guarda en
/// claro (R-05). La persona es siempre la del <c>sub</c> del token.
/// </summary>
internal sealed record CreateVerificationRequest(string? Nivel, bool? AceptaBiometria, string? VersionAviso, string? Curp)
{
    /// <summary>Tope del cuerpo: 8 KB (R-20).</summary>
    public const int MaxBodyBytes = 8 * 1024;
}

/// <summary>
/// Una foto del nivel. <c>urlSubida</c> y <c>venceEn</c> son el enlace firmado de 5 minutos (R-08); llegan con la tarea T-04 (cuenta de almacenamiento privada), así
/// que por ahora van en <see langword="null"/>.
/// </summary>
internal sealed record VerificationPhotoResponse(string Tipo, string Estado, string? UrlSubida, DateTime? VenceEn);

/// <summary>Respuesta 201 de la creación (R-08).</summary>
internal sealed record VerificationCreatedResponse(string Id, string Estado, string Nivel, IReadOnlyList<VerificationPhotoResponse> Fotos, DateTime CapturaVenceEn);

/// <summary>Una foto en la consulta del estado: solo el tipo y el estado, nunca la dirección (R-14).</summary>
internal sealed record VerificationPhotoStateResponse(string Tipo, string Estado);

/// <summary>Estado de la verificación más reciente (R-14 y R-15). Nunca trae fotos, direcciones, CURP ni su HMAC.</summary>
internal sealed record VerificationStatusResponse(
    string? Id,
    string? Nivel,
    string Estado,
    string? Motivo,
    IReadOnlyList<string> Consejos,
    string? NivelOtorgado,
    IReadOnlyList<VerificationPhotoStateResponse> Fotos,
    DateTime? CreadoEn,
    DateTime? EnviadoEn,
    DateTime? DecididoEn,
    DateTime? ExpiraEn,
    bool PuedeReintentar);
