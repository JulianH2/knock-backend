using Asp.Versioning;
using Knock.Identity.Api.Hosting;
using Knock.Identity.Api.Modules.Account.Dtos;
using Knock.Identity.Api.Modules.Account.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Knock.Identity.Api.Modules.Account.Controllers.V1;

/// <summary>Eliminación de la cuenta con 7 días de gracia (spec 039, USR-10 y USR-11).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/me/eliminacion")]
[Authorize]
internal sealed class MeEliminacionController(AccountDeletionService deletion) : AccountControllerBase
{
    /// <summary>Límite del cuerpo de la eliminación: 8 KiB (un token de Firebase, la confirmación y un motivo de 200 caracteres).</summary>
    public const int MaxBodyBytes = 8192;

    /// <summary>Cancelar no lleva cuerpo: cualquiera que llegue se corta en 1 KiB (413).</summary>
    public const int MaxCancelBodyBytes = 1024;

    /// <summary>
    /// USR-10. Programa la eliminación con 7 días de gracia y cancela de inmediato las solicitudes abiertas y las cotizaciones pendientes. Se confirma con el
    /// texto <c>ELIMINAR</c>; con una sesión de más de 24 horas pide además el token de Firebase.
    /// </summary>
    [HttpPost]
    [EnableRateLimiting(RateLimitingExtensions.DeletionPolicy)]
    [RequestSizeLimit(MaxBodyBytes)]
    [ProducesResponseType<DeletionScheduledResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Schedule([FromBody] DeleteAccountRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        if (!TryGetCaller(out var sub, out var sessionId))
        {
            return UnauthorizedProblem();
        }

        // El ValidationFilter ya garantizó que la confirmación es ELIMINAR y que el token y el motivo no pasan de su largo.
        var outcome = await deletion.ScheduleAsync(sub, sessionId, new DeleteAccountInput(request.FirebaseIdToken), cancellationToken);
        return outcome switch
        {
            DeletionOutcome.Scheduled scheduled => Accepted(new DeletionScheduledResponse(scheduled.DeleteAt)),
            DeletionOutcome.ReauthRequired => await ReauthErrorAsync("Is required because the session is older than 24 hours."),
            _ => await ReauthErrorAsync("The token is not valid."),
        };
    }

    /// <summary>USR-11. Cancela la eliminación durante los 7 días de gracia.</summary>
    [HttpPost("cancelar")]
    [RequestSizeLimit(MaxCancelBodyBytes)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Cancel(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        if (!TryGetCaller(out var sub, out _))
        {
            return UnauthorizedProblem();
        }

        await deletion.CancelAsync(sub, cancellationToken);
        return Ok();
    }

    private async Task<IActionResult> ReauthErrorAsync(string reason) =>
        await ValidationFilter.WriteErrorsAsync(HttpContext, new Dictionary<string, string[]> { ["firebaseIdToken"] = [reason] });
}
