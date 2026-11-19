using Asp.Versioning;
using Knock.Identity.Api.Modules.Panel.Authorization;
using Knock.Identity.Api.Modules.Panel.Domain;
using Knock.Identity.Api.Modules.Panel.Dtos;
using Knock.Identity.Api.Modules.Panel.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Identity.Api.Modules.Panel.Controllers.V1;

/// <summary>Activación del TOTP con el enlace de un solo uso (spec 033, R-29).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/panel/activacion")]
[PanelSesionEndpoint]
internal sealed class PanelActivacionController(ActivacionPanelService activation) : ControllerBase
{
    /// <summary>Límite del cuerpo: 1 KiB (R-29).</summary>
    public const int MaxBodyBytes = 1024;

    /// <summary>Consume el enlace y entrega el secreto una sola vez. 410 <c>activation_invalid</c> con el mismo cuerpo para cualquier enlace inservible.</summary>
    [HttpPost]
    [AllowAnonymous]
    [RequestSizeLimit(MaxBodyBytes)]
    [ProducesResponseType<PanelActivacionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status410Gone)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Activate([FromBody] PanelActivacionRequest request, CancellationToken cancellationToken)
    {
        var outcome = await activation.ActivateAsync(request.Token ?? string.Empty, cancellationToken);
        return outcome is ActivationOutcome.Activated activated
            ? Ok(new PanelActivacionResponse(activated.OtpauthUri, activated.QrSvg))
            : await PanelHttp.ProblemAsync(HttpContext, StatusCodes.Status410Gone, PanelErrorCodes.ActivationInvalidCode);
    }
}
