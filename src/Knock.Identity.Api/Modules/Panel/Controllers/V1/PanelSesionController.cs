using Asp.Versioning;
using Knock.Identity.Api.Modules.Panel.Authorization;
using Knock.Identity.Api.Modules.Panel.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Identity.Api.Modules.Panel.Controllers.V1;

/// <summary>
/// Sesión del panel interno (spec 033). T-01: contrato publicado con respuestas simuladas para que frontend (spec 023) genere su cliente;
/// T-06 reemplaza el cuerpo de cada acción por los servicios reales.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/panel/sesion")]
[PanelSesionEndpoint]
internal sealed class PanelSesionController : ControllerBase
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
    public IActionResult VerifyPhone([FromBody] PanelCelularRequest request) =>
        Ok(new PanelCelularResponse(new string('A', 22), true, DateTime.UtcNow.AddMinutes(5)));

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
    public IActionResult VerifyTotp([FromBody] PanelTotpRequest request) =>
        Ok(new PanelAccesoResponse("simulado", DateTime.UtcNow.AddMinutes(15), [], "Simulado"));

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
