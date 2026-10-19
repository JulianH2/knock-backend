using Asp.Versioning;
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
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> ExchangeFirebaseToken([FromBody] FirebaseExchangeRequest request, CancellationToken cancellationToken)
    {
        var session = await sessions.ExchangeAsync(request.FirebaseIdToken, cancellationToken);
        return session is null
            ? Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: "Invalid credentials.")
            : Ok(new SessionResponse(session.AccessToken, session.RefreshToken));
    }
}
