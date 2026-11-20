namespace Knock.Identity.Api.Modules.Panel.Dtos;

/// <summary>
/// Primer paso del acceso al panel (spec 033, R-04). El campo es anulable a propósito: si falta, el validador responde 422 en su campo (no el 400
/// automático de MVC). <see cref="ToString"/> no incluye el token para que ningún registro lo reciba por accidente.
/// </summary>
internal sealed record PanelCelularRequest(string? IdToken = null)
{
    public override string ToString() => nameof(PanelCelularRequest);
}

/// <summary>Respuesta del primer paso (R-07): el desafío de un solo uso y cuándo vence (UTC).</summary>
internal sealed record PanelCelularResponse(string DesafioId, bool RequiereTotp, DateTime VenceEn)
{
    public override string ToString() => nameof(PanelCelularResponse);
}

/// <summary>Segundo paso (R-08): el desafío y el código TOTP de 6 dígitos. Los campos son anulables por la misma razón que en el primer paso.</summary>
internal sealed record PanelTotpRequest(string? DesafioId = null, string? Codigo = null)
{
    public override string ToString() => nameof(PanelTotpRequest);
}

/// <summary>Sesión del panel (R-12 y R-17): token de acceso, su vencimiento (UTC), los permisos efectivos en el orden de la lista cerrada y el nombre visible.</summary>
internal sealed record PanelAccesoResponse(string AccessToken, DateTime ExpiraEn, IReadOnlyList<string> Permisos, string Nombre)
{
    public override string ToString() => nameof(PanelAccesoResponse);
}

/// <summary>Consumo del enlace de activación del TOTP (R-29). El token llega en el cuerpo, nunca en la URL.</summary>
internal sealed record PanelActivacionRequest(string? Token = null)
{
    public override string ToString() => nameof(PanelActivacionRequest);
}

/// <summary>Secreto del TOTP en forma de dirección <c>otpauth://</c> y de código QR (SVG); solo se entrega una vez (R-29).</summary>
internal sealed record PanelActivacionResponse(string OtpauthUri, string QrSvg)
{
    public override string ToString() => nameof(PanelActivacionResponse);
}
