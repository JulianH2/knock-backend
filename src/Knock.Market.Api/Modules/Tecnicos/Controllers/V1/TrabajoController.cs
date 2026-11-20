using Asp.Versioning;
using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Tecnicos.Dtos;
using Knock.Market.Api.Modules.Tecnicos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Market.Api.Modules.Tecnicos.Controllers.V1;

/// <summary>Resumen privado del trabajo del técnico autenticado.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tecnicos/me/trabajo")]
[Authorize]
[PrivateNoStore]
internal sealed class TrabajoController(TecnicoTrabajoService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<TrabajoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get([FromQuery] string? cursorTerminadas, [FromQuery] int limit = 20, CancellationToken cancellationToken = default)
    {
        var result = await service.GetAsync(User.FindFirst("sub")?.Value ?? string.Empty, cursorTerminadas, limit, cancellationToken);
        if (result.Response is not null) return Ok(result.Response);
        var status = result.Error switch
        {
            "not_a_technician" => StatusCodes.Status403Forbidden,
            "invalid_cursor" => StatusCodes.Status400BadRequest,
            "invalid_limit" => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status503ServiceUnavailable,
        };
        var problem = new ProblemDetails { Status = status, Title = "Request cannot be completed." };
        problem.Extensions["code"] = result.Error;
        return StatusCode(status, problem);
    }
}
