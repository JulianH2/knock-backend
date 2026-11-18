using Asp.Versioning;
using Knock.Identity.Api.Hosting;
using Knock.Identity.Api.Modules.Account.Dtos;
using Knock.Identity.Api.Modules.Account.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Knock.Identity.Api.Modules.Account.Controllers.V1;

/// <summary>Cambio de número de la persona con sesión (spec 039, USR-03 y USR-04).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/me/cambio-numero")]
[Authorize]
internal sealed class MeCambioNumeroController(PhoneChangeService phoneChange) : AccountControllerBase
{
    /// <summary>Límite del cuerpo: 4 KiB (un celular o un token de Firebase).</summary>
    public const int MaxBodyBytes = 4096;

    /// <summary>USR-03. Comprueba que el número nuevo no pertenece a otra cuenta, antes de que la app gaste un SMS de Firebase.</summary>
    [HttpPost]
    [EnableRateLimiting(RateLimitingExtensions.PhoneChangeCheckPolicy)]
    [RequestSizeLimit(MaxBodyBytes)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CheckAvailability([FromBody] PhoneAvailabilityRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCaller(out _, out _))
        {
            return UnauthorizedProblem();
        }

        // El ValidationFilter ya garantizó que el celular viene y es un E.164 de México.
        await phoneChange.EnsureAvailableAsync(request.CelularNuevo ?? string.Empty, cancellationToken);
        return NoContent();
    }
}
