using Asp.Versioning;
using Knock.Identity.Api.Hosting;
using Knock.Identity.Api.Modules.Trust.Dtos;
using Knock.Identity.Api.Modules.Trust.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Knock.Identity.Api.Modules.Trust.Controllers.V1;

/// <summary>
/// Verificación de identidad propia (spec 025, KNOCK-79 y KNOCK-80): crear con consentimiento biométrico y consultar el estado. La ruta no lleva identificador de
/// persona: es siempre la del <c>sub</c> del token. Nada se cachea (R-18).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/me/verificacion")]
[Authorize]
internal sealed class MeVerificacionController(VerificationService verifications) : ControllerBase
{
    /// <summary>Crea la verificación en <c>en_captura</c> con su consentimiento. Las fotos llegan con la tarea T-04 (enlaces de subida).</summary>
    [HttpPost]
    [RequestSizeLimit(CreateVerificationRequest.MaxBodyBytes)]
    [EnableRateLimiting(RateLimitingExtensions.VerificationCreatePolicy)]
    [ProducesResponseType<VerificationCreatedResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Create([FromBody] CreateVerificationRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        var sub = User.FindFirst("sub")?.Value;
        var sid = User.FindFirst("sid")?.Value;
        if (string.IsNullOrWhiteSpace(sub) || string.IsNullOrWhiteSpace(sid))
        {
            return Unauthorized();
        }

        var created = await verifications.CreateAsync(sub, sid, request, cancellationToken);
        return created is null ? Unauthorized() : StatusCode(StatusCodes.Status201Created, created);
    }

    /// <summary>Estado de la verificación más reciente; sin ninguna, <c>sin_verificar</c> (nunca 404).</summary>
    [HttpGet]
    [ProducesResponseType<VerificationStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        var sub = User.FindFirst("sub")?.Value;
        return string.IsNullOrWhiteSpace(sub) ? Unauthorized() : Ok(await verifications.GetAsync(sub, cancellationToken));
    }
}
