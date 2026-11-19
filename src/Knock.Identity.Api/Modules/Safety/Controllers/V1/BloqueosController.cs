using Asp.Versioning;
using Knock.Identity.Api.Hosting;
using Knock.Identity.Api.Modules.Safety.Dtos;
using Knock.Identity.Api.Modules.Safety.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Knock.Identity.Api.Modules.Safety.Controllers.V1;

/// <summary>
/// Bloqueos entre personas (spec 043, BLO-01 a BLO-03): bloquear, desbloquear y listar a quién bloqueó quien llama. Recibe, valida el formato y delega en <see cref="BlockService"/>; no toca
/// MongoDB. La persona es siempre la del <c>sub</c> del token: ninguna ruta, consulta ni cuerpo acepta el identificador de otra persona para actuar en su nombre (R-24). Nunca se cachea.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize]
internal sealed class BloqueosController(BlockService service) : ControllerBase
{
    private const string _problemJson = "application/problem+json";

    /// <summary>Bloquea a la persona de la ruta. 201; 404 <c>user_not_found</c>; 409 <c>already_blocked</c>; 422 si es uno mismo o el identificador no es válido.</summary>
    [HttpPost("api/v{version:apiVersion}/usuarios/{id}/bloqueo")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, _problemJson)]
    public async Task<IActionResult> Block([FromRoute] BlockRoute route, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        if (await service.BlockAsync(Subject(), route.Id!, cancellationToken) == BlockOutcome.Yourself)
        {
            return await ValidationFilter.WriteErrorsAsync(HttpContext, new Dictionary<string, string[]> { ["id"] = [ValidationFilter.GenericReason] });
        }

        return Created(new Uri("/api/v1/me/bloqueos", UriKind.Relative), value: null);
    }

    /// <summary>Quita el bloqueo («Deshacer»). 204; 404 <c>block_not_found</c> si no había.</summary>
    [HttpDelete("api/v{version:apiVersion}/usuarios/{id}/bloqueo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, _problemJson)]
    public async Task<IActionResult> Unblock([FromRoute] BlockRoute route, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        await service.UnblockAsync(Subject(), route.Id!, cancellationToken);
        return NoContent();
    }

    /// <summary>A quién tiene bloqueado quien llama, de lo más reciente a lo más antiguo, con paginación por cursor y solo con identificador, nombre público y fecha.</summary>
    [HttpGet("api/v{version:apiVersion}/me/bloqueos")]
    [ProducesResponseType<BlockedPage>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, _problemJson)]
    public async Task<ActionResult<BlockedPage>> List([FromQuery] BlocksQuery query, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        return await service.ListAsync(Subject(), query, cancellationToken);
    }

    private string Subject() => User.FindFirst("sub")?.Value ?? string.Empty;
}
