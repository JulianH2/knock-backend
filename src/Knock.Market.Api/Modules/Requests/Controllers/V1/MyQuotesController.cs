using Asp.Versioning;
using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Requests.Dtos;
using Knock.Market.Api.Modules.Requests.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Market.Api.Modules.Requests.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tecnicos/me/cotizaciones")]
[Authorize]
[PrivateNoStore]
internal sealed class MyQuotesController(MyQuotesService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<MyQuotePage>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> List([FromQuery] MyQuotesQuery query, CancellationToken cancellationToken)
    {
        var subject = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(subject)) return Unauthorized();
        var result = await service.ListAsync(subject, query, cancellationToken);
        return result is null ? Forbid() : Ok(result);
    }
}
