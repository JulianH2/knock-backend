using Asp.Versioning;
using Knock.Api.Modules.Identity.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Api.Modules.Identity.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/me")]
[Authorize]
internal sealed class MeController : ControllerBase
{
    /// <summary>El id sale del <c>sub</c> del JWT, nunca de un parámetro de la petición.</summary>
    [HttpGet]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public IActionResult Get() =>
        User.FindFirst("sub")?.Value is { Length: > 0 } id ? Ok(new MeResponse(id)) : Unauthorized();
}
