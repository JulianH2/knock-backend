using Asp.Versioning;
using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Requests.Dtos;
using Knock.Market.Api.Modules.Requests.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Knock.Market.Api.Modules.Requests.Controllers.V1;

/// <summary>
/// Comparar las cotizaciones de una solicitud propia (spec 021, HU-032): hasta cinco, con orden y etiquetas, sin contacto ni direcciones. Recibe, valida el formato y
/// delega en <see cref="QuoteComparisonService"/>; no toca MongoDB. Nunca se cachea (<c>Cache-Control: private, no-store</c>).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/solicitudes/{id}/cotizaciones")]
[Authorize]
[PrivateNoStore]
internal sealed class QuoteComparisonController(QuoteComparisonService service) : ControllerBase
{
    private const string _problemJson = "application/problem+json";

    /// <summary>Cotizaciones de una solicitud propia, con el orden pedido. El <c>sub</c> del token debe ser el del dueño; cualquier otro recibe 404.</summary>
    [HttpGet]
    [EnableRateLimiting(RateLimitingExtensions.CompareQuotesPolicy)]
    [ProducesResponseType<QuoteComparison>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, _problemJson)]
    public async Task<ActionResult<QuoteComparison>> Compare(string id, [FromQuery] QuoteComparisonQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await service.CompareAsync(User.FindFirst("sub")?.Value ?? string.Empty, id, query.Orden, cancellationToken) is { } comparison ? comparison : NotFound();
    }
}
