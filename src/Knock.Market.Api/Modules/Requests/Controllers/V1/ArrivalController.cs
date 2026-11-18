using Asp.Versioning;
using Knock.Market.Api.Modules.Requests.Domain;
using Knock.Market.Api.Modules.Requests.Dtos;
using Knock.Market.Api.Modules.Requests.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Knock.Market.Api.Modules.Requests.Controllers.V1;

/// <summary>
/// Confirmación de llegada con el código de cuatro dígitos (spec 022, R-07 a R-11). Recibe, valida el formato y delega en <see cref="ArrivalService"/>; no descifra ni toca
/// MongoDB. Nunca se cachea (<c>Cache-Control: private, no-store</c>).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/visitas/{id}/llegada")]
[Authorize]
[Hosting.PrivateNoStore]
internal sealed class ArrivalController(ArrivalService service) : BusinessRuleController
{
    /// <summary>
    /// El técnico elegido confirma que llegó. Código incorrecto: 422 <c>wrong_code</c> con <c>intentosRestantes</c>; al tercero la visita queda bloqueada.
    /// Estados no válidos responden 409 con <c>outside_window</c>, <c>code_used</c>, <c>code_expired</c>, <c>visit_locked</c>, <c>visit_cancelled</c>,
    /// <c>visit_not_active</c> o <c>visit_without_code</c>.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(ArrivalBody.MaxBodyBytes)]
    [EnableRateLimiting(Hosting.RateLimitingExtensions.ArrivalPolicy)]
    [ProducesResponseType<VisitResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, ProblemJson)]
    public async Task<IActionResult> Arrive(string id, [FromBody] ArrivalBody body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var outcome = await service.ArriveAsync(Subject(), id, body.Codigo!, cancellationToken);
        return outcome.Status switch
        {
            ArrivalStatus.Arrived => Ok(outcome.Visit),
            ArrivalStatus.NotATechnician => Rule(StatusCodes.Status403Forbidden, VisitErrorCodes.NotATechnicianCode),
            ArrivalStatus.NotFound => NotFound(),
            ArrivalStatus.WrongCode => Rule(StatusCodes.Status422UnprocessableEntity, VisitErrorCodes.WrongCode, "intentosRestantes", outcome.AttemptsLeft),
            ArrivalStatus.CodeUsed => Rule(StatusCodes.Status409Conflict, VisitErrorCodes.CodeUsedCode),
            ArrivalStatus.CodeExpired => Rule(StatusCodes.Status409Conflict, VisitErrorCodes.CodeExpiredCode),
            ArrivalStatus.VisitLocked => Rule(StatusCodes.Status409Conflict, VisitErrorCodes.VisitLockedCode),
            ArrivalStatus.VisitCancelled => Rule(StatusCodes.Status409Conflict, VisitErrorCodes.VisitCancelledCode),
            ArrivalStatus.VisitNotActive => Rule(StatusCodes.Status409Conflict, VisitErrorCodes.VisitNotActiveCode),
            ArrivalStatus.VisitWithoutCode => Rule(StatusCodes.Status409Conflict, VisitErrorCodes.VisitWithoutCodeCode),
            _ => Rule(StatusCodes.Status409Conflict, VisitErrorCodes.OutsideWindowCode),
        };
    }
}
