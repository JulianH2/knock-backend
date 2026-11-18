using Asp.Versioning;
using Knock.Identity.Api.Modules.HomeAndCar.Dtos;
using Knock.Identity.Api.Modules.HomeAndCar.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Identity.Api.Modules.HomeAndCar.Controllers.V1;

/// <summary>
/// Mi casa y mi auto (spec 041). La ruta nunca lleva el identificador de la persona: es siempre la del <c>sub</c> del token (R-20), así nadie puede pedir ni cambiar lo de otra.
/// Un elemento ajeno responde igual que uno inexistente (404, R-16); por eso ninguna ruta responde 403.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/me/casa-y-auto")]
[Authorize]
internal sealed class CasaYAutoController(HomeAndCarService service) : ControllerBase
{
    /// <summary>Lista los autos y las cosas de la casa de la persona (CAS-01).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        return Sub() is { } sub ? Ok(await service.ListAsync(sub, cancellationToken)) : Unauthorized();
    }

    private string? Sub() => User.FindFirst("sub")?.Value is { Length: > 0 } sub ? sub : null;
}
