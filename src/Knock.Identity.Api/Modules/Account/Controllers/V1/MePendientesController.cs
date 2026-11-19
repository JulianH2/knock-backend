using Asp.Versioning;
using Knock.Identity.Api.Modules.Account.Dtos;
using Knock.Identity.Api.Modules.Account.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Identity.Api.Modules.Account.Controllers.V1;

/// <summary>Pendientes de la persona antes de eliminar su cuenta (spec 039, USR-09).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/me/pendientes")]
[Authorize]
internal sealed class MePendientesController(PendingService pending) : AccountControllerBase
{
    /// <summary>USR-09. Lista lo que hay pendiente antes de eliminar la cuenta: citas y cotizaciones abiertas (solo conteos e identificadores propios, máximo 50).</summary>
    [HttpGet]
    [ProducesResponseType<PendingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        if (!TryGetCaller(out var sub, out _))
        {
            return UnauthorizedProblem();
        }

        var result = await pending.ListAsync(sub, cancellationToken);
        return Ok(new PendingResponse(
            new PendingGroupResponse(result.AppointmentTotal, result.AppointmentIds),
            new PendingGroupResponse(result.QuoteTotal, result.QuoteIds)));
    }
}
