using Asp.Versioning;
using Knock.Api.Modules.Catalog.Dtos;
using Knock.Api.Modules.Catalog.Services;
using Knock.Api.Modules.Catalog.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Api.Modules.Catalog.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/colonias")]
[Authorize]
internal sealed class ColoniasController(INeighborhoodLocator locator) : ControllerBase
{
    private const string _problemJson = "application/problem+json";

    /// <summary>
    /// Colonia del punto o «fuera de cobertura» (200 en ambos casos). Las coordenadas se convierten en colonia y se descartan: no se
    /// guardan, no se registran, no se cachean y no vuelven en la respuesta. El 422 lo da <c>ValidationFilter</c> antes de esta acción.
    /// </summary>
    [HttpGet("en-punto")]
    // Solo `no-store` (spec §4): `Location = None` agregaría `no-cache` y `Pragma`.
    [ResponseCache(NoStore = true)]
    [ProducesResponseType<NeighborhoodAtPointResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, _problemJson)]
    public async Task<ActionResult<NeighborhoodAtPointResponse>> GetAtPoint(
        [FromQuery] NeighborhoodAtPointQuery query, CancellationToken cancellationToken)
    {
        var colonia = await locator.FindAtAsync(
            NeighborhoodAtPointQueryValidator.Parse(query.Lat!), NeighborhoodAtPointQueryValidator.Parse(query.Lon!), cancellationToken);
        return colonia is null
            ? new NeighborhoodAtPointResponse(false, null)
            : new NeighborhoodAtPointResponse(true, new NeighborhoodAtPointColoniaResponse(colonia.Id, colonia.Nombre, colonia.Cp, colonia.Municipio));
    }
}
