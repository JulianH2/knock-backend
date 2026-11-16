using Asp.Versioning;
using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Requests.Domain;
using Knock.Market.Api.Modules.Requests.Dtos;
using Knock.Market.Api.Modules.Requests.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Knock.Market.Api.Modules.Requests.Controllers.V1;

/// <summary>
/// Elegir una cotización (spec 021, HU-033): la solicitud pasa a <c>cita_confirmada</c>, se crea la visita y se revela el contacto público del técnico y el lugar. Recibe,
/// valida el formato y delega en <see cref="ChooseQuoteService"/>; no toca MongoDB. Nunca se cachea (<c>Cache-Control: private, no-store</c>).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/solicitudes/{id}/elegir")]
[Authorize]
[PrivateNoStore]
internal sealed class ChooseQuoteController(ChooseQuoteService service) : BusinessRuleController
{
    /// <summary>Elige la cotización de una solicitud propia; la elección y sus efectos ocurren en una sola transacción.</summary>
    [HttpPost]
    [RequestSizeLimit(ChooseQuoteBody.MaxBodyBytes)]
    [EnableRateLimiting(RateLimitingExtensions.ChooseQuotePolicy)]
    [ProducesResponseType<Appointment>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, ProblemJson)]
    public async Task<IActionResult> Choose(string id, [FromBody] ChooseQuoteBody body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var outcome = await service.ChooseAsync(Subject(), id, body.CotizacionId!, cancellationToken);
        return outcome.Status switch
        {
            ChooseStatus.Chosen => Ok(outcome.Appointment),
            ChooseStatus.NotFound => NotFound(),
            ChooseStatus.AlreadyChosen => Rule(StatusCodes.Status409Conflict, RequestErrorCodes.RequestAlreadyChosenCode),
            ChooseStatus.Expired => Rule(StatusCodes.Status409Conflict, RequestErrorCodes.RequestExpiredCode),
            ChooseStatus.Cancelled => Rule(StatusCodes.Status409Conflict, RequestErrorCodes.RequestCancelledCode),
            ChooseStatus.WithoutQuotes => Rule(StatusCodes.Status409Conflict, RequestErrorCodes.RequestWithoutQuotesCode),
            ChooseStatus.QuoteUnavailable => Rule(StatusCodes.Status409Conflict, RequestErrorCodes.QuoteUnavailableCode),
            _ => Rule(StatusCodes.Status503ServiceUnavailable, RequestErrorCodes.ServiceUnavailableCode),
        };
    }
}
