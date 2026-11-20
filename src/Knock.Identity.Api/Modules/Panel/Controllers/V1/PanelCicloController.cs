using Asp.Versioning;
using Knock.Identity.Api.Modules.Panel.Authorization;
using Knock.Identity.Api.Modules.Panel.Domain;
using Knock.Identity.Api.Modules.Panel.Dtos;
using Knock.Identity.Api.Modules.Panel.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Identity.Api.Modules.Panel.Controllers.V1;

/// <summary>Renovación y cierre de la sesión del panel interno (spec 033, R-17 a R-19).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/panel/sesion")]
[PanelSesionEndpoint]
internal sealed class PanelCicloController(SesionPanelCicloService lifecycle, TimeProvider timeProvider) : ControllerBase
{
    /// <summary>Renovación (R-17 y R-18): sin cuerpo; exige la cookie, <c>X-Knock-Panel: 1</c> y el <c>Origin</c> del panel.</summary>
    [HttpPost("renovar")]
    [AllowAnonymous]
    [ProducesResponseType<PanelAccesoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Renew(
        [FromHeader(Name = PanelHttp.MarkerHeader)] string? marker,
        [FromHeader(Name = "Origin")] string? origin,
        CancellationToken cancellationToken)
    {
        var outcome = await lifecycle.RenewAsync(PanelHttp.RefreshCookie(Request), marker, origin, cancellationToken);
        switch (outcome)
        {
            case RenewOutcome.Renewed renewed:
                PanelHttp.SetRefreshCookie(Response, renewed.Grant.RefreshToken);
                return Ok(new PanelAccesoResponse(renewed.Grant.AccessToken, renewed.Grant.ExpiraEn, renewed.Grant.Permisos, renewed.Grant.Nombre));
            case RenewOutcome.PermissionsChanged:
                PanelHttp.ClearRefreshCookie(Response);
                return await PanelHttp.ProblemAsync(HttpContext, StatusCodes.Status401Unauthorized, PanelErrorCodes.PermissionsChangedCode);
            case RenewOutcome.InvalidSession invalid:
                if (invalid.ClearCookie)
                {
                    PanelHttp.ClearRefreshCookie(Response);
                }

                return await PanelHttp.ProblemAsync(HttpContext, StatusCodes.Status401Unauthorized, PanelErrorCodes.InvalidSessionCode);
            default:
                return await PanelHttp.ProblemAsync(HttpContext, StatusCodes.Status403Forbidden, PanelErrorCodes.OriginNotAllowedCode);
        }
    }

    /// <summary>Cierre (R-19): exige el token del panel y <c>X-Knock-Panel: 1</c>; revoca la sesión y borra la cookie.</summary>
    [HttpPost("cerrar")]
    [Authorize(AuthenticationSchemes = PanelBearer.Scheme)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Close([FromHeader(Name = PanelHttp.MarkerHeader)] string? marker, CancellationToken cancellationToken)
    {
        if (marker != PanelHttp.MarkerValue)
        {
            return await PanelHttp.ProblemAsync(HttpContext, StatusCodes.Status403Forbidden, PanelErrorCodes.OriginNotAllowedCode);
        }

        var sid = User.FindFirst("sid")?.Value ?? string.Empty;
        var sub = User.FindFirst("sub")?.Value ?? string.Empty;
        var remaining = TimeSpan.FromSeconds(1);
        if (long.TryParse(User.FindFirst("exp")?.Value, System.Globalization.CultureInfo.InvariantCulture, out var expiresAt))
        {
            remaining = DateTimeOffset.FromUnixTimeSeconds(expiresAt) - timeProvider.GetUtcNow();
        }

        await lifecycle.CloseAsync(sid, sub, remaining, cancellationToken);
        PanelHttp.ClearRefreshCookie(Response);
        return NoContent();
    }
}
