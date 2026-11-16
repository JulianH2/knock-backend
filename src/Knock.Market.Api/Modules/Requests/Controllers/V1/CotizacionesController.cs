using Asp.Versioning;
using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Requests.Services;
using Knock.Market.Api.Modules.Requests.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using MongoDB.Bson;

namespace Knock.Market.Api.Modules.Requests.Controllers.V1;

internal sealed record QuoteRequest(string? TipoPrecio, decimal? Precio, decimal? PrecioMin, decimal? PrecioMax,
    DateOnly? Dia, string? Hora, string? Nota);

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/solicitudes/{id}/cotizaciones")]
[Authorize]
[EnableRateLimiting(RateLimitingExtensions.QuotePolicy)]
internal sealed class CotizacionesController(QuoteService service) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(8192)]
    [ProducesResponseType<QuoteResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Send(string id, [FromBody] QuoteRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        var subject = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(subject)) return Unauthorized();
        if (!ObjectId.TryParse(id, out _)) return BadRequest();
        var input = new QuoteValidationInput(request.TipoPrecio, request.Precio, request.PrecioMin, request.PrecioMax,
            request.Dia, request.Hora, request.Nota, default);
        var result = await service.SendAsync(subject, id, input, cancellationToken);
        return result switch
        {
            QuoteCheckResult.Created => StatusCode(StatusCodes.Status201Created, service.CreatedQuote),
            QuoteCheckResult.NotATechnician => Error(403, "not_a_technician"),
            QuoteCheckResult.NotFound => NotFound(),
            QuoteCheckResult.RequestNotQuotable => Error(409, "request_not_quotable"),
            QuoteCheckResult.QuoteDuplicate => Error(409, "quote_duplicate"),
            QuoteCheckResult.QuoteQuotaFull => Error(409, "quote_quota_full"),
            QuoteCheckResult.Invalid => UnprocessableEntity(new ValidationProblemDetails(service.Errors) { Status = 422 }),
            QuoteCheckResult.ContentRejected => Error(422, "content_rejected"),
            QuoteCheckResult.ModerationUnavailable => ModerationUnavailable(),
            _ => throw new InvalidOperationException("Unknown quote result."),
        };
    }

    private ObjectResult Error(int status, string code)
    {
        var problem = new ProblemDetails { Status = status, Title = ReasonPhrases.GetReasonPhrase(status) };
        problem.Extensions["code"] = code;
        return StatusCode(status, problem);
    }

    private ObjectResult ModerationUnavailable()
    {
        Response.Headers.RetryAfter = "60";
        return Error(503, "moderation_unavailable");
    }
}
