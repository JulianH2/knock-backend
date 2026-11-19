using Asp.Versioning;
using Knock.Identity.Api.Modules.Panel.Authorization;
using Knock.Identity.Api.Modules.Panel.Domain;
using Knock.Identity.Api.Modules.Panel.Dtos;
using Knock.Identity.Api.Modules.Panel.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Identity.Api.Modules.Panel.Controllers.V1;

/// <summary>
/// Sesión del panel interno (spec 033). Los dos pasos de acceso (T-06) son reales; <c>renovar</c> y <c>cerrar</c> siguen simulados hasta T-07.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/panel/sesion")]
[PanelSesionEndpoint]
internal sealed class PanelSesionController(AccesoPanelService access) : ControllerBase
{
    /// <summary>Límite del cuerpo del primer paso: 8 KiB (R-04).</summary>
    public const int CelularMaxBodyBytes = 8192;

    /// <summary>Límite del cuerpo del segundo paso: 1 KiB (R-08).</summary>
    public const int TotpMaxBodyBytes = 1024;

    /// <summary>Primer paso (R-04 a R-07): valida el <c>idToken</c> de Firebase y abre un desafío de un solo uso.</summary>
    [HttpPost("celular")]
    [AllowAnonymous]
    [RequestSizeLimit(CelularMaxBodyBytes)]
    [ProducesResponseType<PanelCelularResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> VerifyPhone([FromBody] PanelCelularRequest request, CancellationToken cancellationToken)
    {
        // El ValidationFilter ya garantizó que idToken viene y tiene de 1 a 4 096 caracteres.
        var outcome = await access.VerifyPhoneAsync(request.IdToken ?? string.Empty, PanelHttp.ClientIp(HttpContext), cancellationToken);
        return outcome switch
        {
            PhoneOutcome.Challenge challenge => Ok(new PanelCelularResponse(challenge.ChallengeId, true, challenge.ExpiresAt)),
            PhoneOutcome.InvalidCredentials => await PanelHttp.ProblemAsync(HttpContext, StatusCodes.Status401Unauthorized, PanelErrorCodes.InvalidCredentialsCode),
            PhoneOutcome.Locked locked => await PanelHttp.ProblemAsync(HttpContext, StatusCodes.Status429TooManyRequests, PanelErrorCodes.LockedCode, locked.RetryAfter),
            PhoneOutcome.LimitExceeded limited => await PanelHttp.ProblemAsync(HttpContext, StatusCodes.Status429TooManyRequests, PanelErrorCodes.LimitExceededCode, limited.RetryAfter),
            _ => await PanelHttp.ProblemAsync(HttpContext, StatusCodes.Status403Forbidden, PanelErrorCodes.NoPanelAccessCode),
        };
    }

    /// <summary>Segundo paso (R-08 a R-12): valida el TOTP y abre la sesión del panel; la cookie de renovación va en <c>Set-Cookie</c>.</summary>
    [HttpPost("totp")]
    [AllowAnonymous]
    [RequestSizeLimit(TotpMaxBodyBytes)]
    [ProducesResponseType<PanelAccesoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> VerifyTotp([FromBody] PanelTotpRequest request, CancellationToken cancellationToken)
    {
        var outcome = await access.VerifyTotpAsync(request.DesafioId ?? string.Empty, request.Codigo ?? string.Empty, PanelHttp.ClientIp(HttpContext), cancellationToken);
        switch (outcome)
        {
            case TotpOutcome.Granted granted:
                PanelHttp.SetRefreshCookie(Response, granted.Grant.RefreshToken);
                return Ok(new PanelAccesoResponse(granted.Grant.AccessToken, granted.Grant.ExpiraEn, granted.Grant.Permisos, granted.Grant.Nombre));
            case TotpOutcome.ChallengeExpired:
                return await PanelHttp.ProblemAsync(HttpContext, StatusCodes.Status401Unauthorized, PanelErrorCodes.ChallengeExpiredCode);
            case TotpOutcome.InvalidCode:
                return await PanelHttp.ProblemAsync(HttpContext, StatusCodes.Status401Unauthorized, PanelErrorCodes.InvalidCodeCode);
            case TotpOutcome.Locked locked:
                return await PanelHttp.ProblemAsync(HttpContext, StatusCodes.Status429TooManyRequests, PanelErrorCodes.LockedCode, locked.RetryAfter);
            case TotpOutcome.LimitExceeded limited:
                return await PanelHttp.ProblemAsync(HttpContext, StatusCodes.Status429TooManyRequests, PanelErrorCodes.LimitExceededCode, limited.RetryAfter);
            default:
                return await PanelHttp.ProblemAsync(HttpContext, StatusCodes.Status403Forbidden, PanelErrorCodes.NoPanelAccessCode);
        }
    }

    /// <summary>Renovación (R-17 y R-18): sin cuerpo; exige la cookie, <c>X-Knock-Panel: 1</c> y el <c>Origin</c> del panel.</summary>
    [HttpPost("renovar")]
    [AllowAnonymous]
    [ProducesResponseType<PanelAccesoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public IActionResult Renew() =>
        Ok(new PanelAccesoResponse("simulado", DateTime.UtcNow.AddMinutes(15), [], "Simulado"));

    /// <summary>Cierre (R-19): exige el token del panel y <c>X-Knock-Panel: 1</c>; revoca la sesión y borra la cookie.</summary>
    [HttpPost("cerrar")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public IActionResult Close() => NoContent();
}
