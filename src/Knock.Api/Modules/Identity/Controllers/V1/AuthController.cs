using System.Diagnostics;
using Asp.Versioning;
using Knock.Api.Hosting;
using Knock.Api.Modules.Identity.Dtos;
using Knock.Api.Modules.Identity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Api.Modules.Identity.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
internal sealed class AuthController(FirebaseSessionService sessions) : ControllerBase
{
    /// <summary>Límite del cuerpo del canje: 8 KiB.</summary>
    public const int MaxBodyBytes = 8192;

    [HttpPost("firebase")]
    [AllowAnonymous]
    [RequestSizeLimit(MaxBodyBytes)]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ExchangeFirebaseToken([FromBody] FirebaseExchangeRequest request, CancellationToken cancellationToken)
    {
        // El ValidationFilter ya garantizó que los tres campos vienen y son válidos.
        var exchange = new ExchangeRequest(
            request.FirebaseIdToken ?? string.Empty,
            request.DeviceId ?? string.Empty,
            CorrelationId.Current(HttpContext) ?? string.Empty,
            Activity.Current?.TraceId.ToString() ?? HttpContext.TraceIdentifier);
        var result = await sessions.ExchangeAsync(exchange, cancellationToken);
        if (result is null)
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: "Invalid credentials.");
        }

        var body = new SessionResponse(result.Session.AccessToken, result.Session.RefreshToken, result.IsNew, result.ProfileComplete);
        return result.IsNew ? StatusCode(StatusCodes.Status201Created, body) : Ok(body);
    }
}
