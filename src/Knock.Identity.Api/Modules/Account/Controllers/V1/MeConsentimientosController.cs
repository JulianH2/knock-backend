using Asp.Versioning;
using Knock.Identity.Api.Modules.Account.Domain;
using Knock.Identity.Api.Modules.Account.Dtos;
using Knock.Identity.Api.Modules.Account.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Identity.Api.Modules.Account.Controllers.V1;

/// <summary>Consentimientos de la persona con sesión (spec 039, USR-07 y USR-08).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/me/consentimientos")]
[Authorize]
internal sealed class MeConsentimientosController(ConsentService consents) : AccountControllerBase
{
    /// <summary>Límite del cuerpo del <c>PUT</c>: 1 KiB (un tipo y un booleano).</summary>
    public const int MaxBodyBytes = 1024;

    /// <summary>USR-07. Lista los consentimientos con su versión y fecha.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ConsentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        if (!TryGetCaller(out var sub, out _))
        {
            return UnauthorizedProblem();
        }

        var entries = await consents.ListAsync(sub, cancellationToken);
        return Ok(entries.Select(ToResponse).ToArray());
    }

    /// <summary>USR-08. Activa o retira un consentimiento opcional (promociones, encuestas, recomendaciones); surte efecto de inmediato.</summary>
    [HttpPut]
    [RequestSizeLimit(MaxBodyBytes)]
    [ProducesResponseType<ConsentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Set([FromBody] UpdateConsentRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        if (!TryGetCaller(out var sub, out _))
        {
            return UnauthorizedProblem();
        }

        // El ValidationFilter ya garantizó que el tipo es uno de los opcionales y que `vigente` viene.
        var entry = await consents.SetAsync(sub, request.Tipo ?? string.Empty, request.Vigente == true, cancellationToken);
        return Ok(ToResponse(entry));
    }

    private static ConsentResponse ToResponse(ConsentEntry entry) => new(entry.Type, entry.Active, entry.Mandatory, entry.Version, entry.ChangedAt);
}
