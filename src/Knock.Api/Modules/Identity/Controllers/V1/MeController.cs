using Asp.Versioning;
using Knock.Api.Modules.Identity.Domain;
using Knock.Api.Modules.Identity.Dtos;
using Knock.Api.Modules.Identity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Api.Modules.Identity.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/me")]
[Authorize]
internal sealed class MeController(ProfileService profiles) : ControllerBase
{
    /// <summary>Límite del cuerpo de <c>PUT /me</c>: 2 KiB (spec HU-002 §4).</summary>
    public const int MaxBodyBytes = 2048;

    /// <summary>El id sale del <c>sub</c> del JWT, nunca de un parámetro de la petición.</summary>
    [HttpGet]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        if (User.FindFirst("sub")?.Value is not { Length: > 0 } sub)
        {
            return Unauthorized();
        }

        return await profiles.GetAsync(sub, cancellationToken) is { } profile ? Ok(ToResponse(profile)) : Unauthorized();
    }

    /// <summary>
    /// Completa el nombre de la cuenta del token. La ruta y el cuerpo no llevan ningún id: no hay forma de apuntar a otra cuenta. El
    /// <c>ValidationFilter</c> ya garantizó un nombre válido antes de llegar aquí.
    /// </summary>
    [HttpPut]
    [RequestSizeLimit(MaxBodyBytes)]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Put([FromBody] ProfileRequest request, CancellationToken cancellationToken)
    {
        if (User.FindFirst("sub")?.Value is not { Length: > 0 } sub)
        {
            return Unauthorized();
        }

        PersonName.TryNormalize(request.Nombre, out var name);
        return await profiles.SaveNameAsync(sub, name, cancellationToken) is { } profile ? Ok(ToResponse(profile)) : Unauthorized();
    }

    private static MeResponse ToResponse(UserProfile profile) => new(profile.Sub, profile.Name, profile.ProfileComplete);
}
