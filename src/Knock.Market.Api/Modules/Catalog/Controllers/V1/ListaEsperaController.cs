using Asp.Versioning;
using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Catalog.Dtos;
using Knock.Market.Api.Modules.Catalog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Knock.Market.Api.Modules.Catalog.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/zonas/lista-espera")]
[Authorize]
[EnableRateLimiting(RateLimitingExtensions.WaitlistPolicy)]
internal sealed class ListaEsperaController(WaitlistService service) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(2048)]
    [ProducesResponseType<WaitlistResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<WaitlistResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Join([FromBody] WaitlistRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        var subject = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(subject))
        {
            return Unauthorized();
        }

        var result = await service.JoinAsync(subject, request, cancellationToken);
        var entry = result.Entry;
        var body = new WaitlistResponse(entry.Id, entry.Cp, entry.Municipio, entry.CreadoEn, result.AlreadyRegistered);
        return StatusCode(result.AlreadyRegistered ? StatusCodes.Status200OK : StatusCodes.Status201Created, body);
    }
}
