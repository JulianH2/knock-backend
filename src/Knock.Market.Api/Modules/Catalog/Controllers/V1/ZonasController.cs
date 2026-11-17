using Asp.Versioning;
using Knock.Market.Api.Modules.Catalog.Dtos;
using Knock.Market.Api.Modules.Catalog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Market.Api.Modules.Catalog.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/zonas/cp")]
[Authorize]
internal sealed class ZonasController(ZonaService service) : ControllerBase
{
    [HttpGet("{cp?}")]
    [ProducesResponseType<ZonaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Get([FromRoute] PostalCodeRoute route, [FromHeader(Name = "If-None-Match")] string? ifNoneMatch, CancellationToken cancellationToken)
    {
        var body = await service.FindAsync(route.Cp!, cancellationToken);
        var etag = OficiosController.ETagPayload(body);
        Response.Headers.ETag = etag;
        Response.Headers.CacheControl = "private, no-cache";
        return OficiosController.Matches(ifNoneMatch ?? string.Empty, etag) ? StatusCode(StatusCodes.Status304NotModified) : Ok(body);
    }
}
