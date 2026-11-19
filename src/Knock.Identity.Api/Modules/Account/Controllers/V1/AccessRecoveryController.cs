using System.Globalization;
using Asp.Versioning;
using Knock.Identity.Api.Hosting;
using Knock.Identity.Api.Modules.Account.Dtos;
using Knock.Identity.Api.Modules.Account.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Knock.Identity.Api.Modules.Account.Controllers.V1;

/// <summary>Recuperación de acceso sin sesión (spec 039, USR-05).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/recuperacion-acceso")]
[AllowAnonymous]
internal sealed class AccessRecoveryController(AccessRecoveryService recovery) : ControllerBase
{
    /// <summary>Límite del cuerpo: 4 KiB (un token de Firebase, un celular y un tipo).</summary>
    public const int MaxBodyBytes = 4096;

    /// <summary>
    /// USR-05. Inicia la recuperación con el número nuevo verificado por Firebase; si existe la cuenta, crea un caso para la cola del panel.
    /// Siempre responde 202 sin revelar si la cuenta existe.
    /// </summary>
    [HttpPost]
    [EnableRateLimiting(RateLimitingExtensions.AccessRecoveryPolicy)]
    [RequestSizeLimit(MaxBodyBytes)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> StartRecovery([FromBody] AccessRecoveryRequest request, CancellationToken cancellationToken)
    {
        // El ValidationFilter ya garantizó que los tres campos vienen y tienen la forma correcta.
        var outcome = await recovery.RequestAsync(
            new AccessRecoveryInput(request.FirebaseIdToken ?? string.Empty, request.CelularAnterior ?? string.Empty, request.Tipo ?? string.Empty),
            cancellationToken);
        switch (outcome)
        {
            case AccessRecoveryOutcome.InvalidToken:
                return await ValidationFilter.WriteErrorsAsync(
                    HttpContext,
                    new Dictionary<string, string[]> { ["firebaseIdToken"] = ["The token is not valid."] });
            case AccessRecoveryOutcome.RateLimited limited:
                Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(limited.RetryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                await ProblemResponses.WriteAsync(HttpContext, StatusCodes.Status429TooManyRequests, "Too Many Requests");
                return new EmptyResult();
            default:
                return Accepted();
        }
    }
}
