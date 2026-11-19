using Asp.Versioning;
using Knock.Market.Api.Modules.Requests.Dtos;
using Knock.Market.Api.Modules.Requests.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Market.Api.Modules.Requests.Controllers.V1;

/// <summary>
/// Lectura de la visita (spec 022, R-01): el cliente dueño y el técnico elegido ven cada uno su vista. Recibe, valida el formato y delega en <see cref="VisitService"/>; no
/// toca MongoDB ni descifra nada. Nunca se cachea (<c>Cache-Control: private, no-store</c>).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/visitas")]
[Authorize]
[Hosting.PrivateNoStore]
internal sealed class VisitasController(VisitService service) : BusinessRuleController
{
    /// <summary>Vista del cliente (con el código cuando corresponde) o del técnico (con la dirección y sin código). Cualquier otra persona recibe 404.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType<VisitResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, ProblemJson)]
    public async Task<ActionResult<VisitResponse>> Get(string id, CancellationToken cancellationToken) =>
        await service.GetAsync(Subject(), id, cancellationToken) is { } visit ? visit : NotFound();
}
