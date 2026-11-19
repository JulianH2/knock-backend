using Asp.Versioning;
using Knock.Identity.Api.Modules.Panel.Authorization;
using Knock.Identity.Api.Modules.Panel.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Identity.Api.Modules.Panel.Controllers.V1;

/// <summary>Activación del TOTP con el enlace de un solo uso (spec 033, R-29). T-01: respuesta simulada; T-06 la reemplaza.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/panel/activacion")]
[PanelSesionEndpoint]
internal sealed class PanelActivacionController : ControllerBase
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
    public IActionResult Activate([FromBody] PanelActivacionRequest request) =>
        Ok(new PanelActivacionResponse("simulado", "<svg/>"));
}
