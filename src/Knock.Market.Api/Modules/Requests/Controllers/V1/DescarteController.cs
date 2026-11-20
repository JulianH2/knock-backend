using Asp.Versioning;
using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Requests.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace Knock.Market.Api.Modules.Requests.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/solicitudes/{id}/descartar")]
[Authorize]
[PrivateNoStore]
internal sealed class DescarteController(BandejaService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(409)]
    [ProducesResponseType<ProblemDetails>(429)]
    public async Task<IActionResult> Discard(string id, CancellationToken cancellationToken)
    {
        var sub = User.FindFirst("sub")?.Value ?? string.Empty;
        if (!await service.IsTechnicianAsync(sub, cancellationToken)) return Error(403, "not_a_technician");
        if (!ObjectId.TryParse(id, out var requestId) || Request.ContentLength > 0
            || (Request.ContentLength is null && HttpContext.Features.Get<IHttpRequestBodyDetectionFeature>()?.CanHaveBody == true)) return BadRequest();
        var state = await service.DiscardAsync(sub, requestId, cancellationToken);
        return state switch
        {
            "descartado" => NoContent(),
            "cotizado" => Error(409, "request_not_dismissable"),
            _ => NotFound(),
        };
    }

    private ObjectResult Error(int status, string code)
    {
        var problem = new ProblemDetails { Status = status };
        problem.Extensions["code"] = code;
        return StatusCode(status, problem);
    }
}
