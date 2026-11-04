using Asp.Versioning;
using Knock.Api.Modules.Identity.Dtos;
using Knock.Api.Modules.Identity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Api.Modules.Identity.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
internal sealed class RefreshController(RefreshSessionService refresh) : ControllerBase
{
    /// <summary>Renueva la sesión (HU-003): cambia el refresh token por un par nuevo; el anterior deja de servir.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [RequestSizeLimit(AuthController.MaxBodyBytes)]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> RefreshSession([FromBody] RefreshSessionRequest request, CancellationToken cancellationToken)
    {
        // El ValidationFilter ya garantizó que los dos campos vienen y son válidos.
        var result = await refresh.RenewAsync(new RefreshCommand(request.RefreshToken ?? string.Empty, request.DeviceId ?? string.Empty), cancellationToken);
        if (result is null)
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: "Invalid credentials.");
        }

        return Ok(new SessionResponse(result.Session.AccessToken, result.Session.RefreshToken, false, result.ProfileComplete));
    }
}
