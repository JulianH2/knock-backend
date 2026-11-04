using Asp.Versioning;
using Knock.Api.Modules.Identity.Dtos;
using Knock.Api.Modules.Identity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Api.Modules.Identity.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
internal sealed class LogoutController(LogoutService logout) : ControllerBase
{
    /// <summary>Cierra la sesión de este dispositivo (HU-004): 204 exista o no el refresh token.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [RequestSizeLimit(AuthController.MaxBodyBytes)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
    {
        await logout.LogoutAsync(request.RefreshToken ?? string.Empty, AccessTokenClaims.From(User), cancellationToken);
        return NoContent();
    }
}
