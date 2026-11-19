using Asp.Versioning;
using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Trust.Domain;
using Knock.Market.Api.Modules.Trust.Dtos;
using Knock.Market.Api.Modules.Trust.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;

namespace Knock.Market.Api.Modules.Trust.Controllers.V1;

/// <summary>
/// Técnicos de confianza de la persona (spec 042, TCO-01 a TCO-03): listar, guardar y quitar. Recibe, valida el formato y delega en <see cref="TrustService"/>; no toca MongoDB. La persona
/// es siempre la del <c>sub</c> del token: ninguna ruta, consulta ni cuerpo acepta el identificador de otra (R-12). Nunca se cachea (<c>Cache-Control: private, no-store</c>).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/me/tecnicos-confianza")]
[Authorize]
[PrivateNoStore]
[EnableRateLimiting(RateLimitingExtensions.TrustPolicy)]
internal sealed class TrustedTechniciansController(TrustService service) : ControllerBase
{
    /// <summary>Estas rutas no llevan cuerpo: un cuerpo de más de 1 KiB responde 413 (spec 009).</summary>
    public const int MaxBodyBytes = 1024;

    private const string _problemJson = "application/problem+json";

    /// <summary>Los técnicos guardados, del más reciente al más antiguo, con paginación por cursor. Un técnico oculto no aparece.</summary>
    [HttpGet]
    [ProducesResponseType<TrustedTechnicianPage>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, _problemJson)]
    public async Task<ActionResult<TrustedTechnicianPage>> List([FromQuery] TrustedListQuery query, CancellationToken cancellationToken) =>
        await service.ListAsync(Subject(), query, cancellationToken);

    /// <summary>Guarda al técnico: 201 la primera vez y 200 al repetir, sin duplicar. Un técnico que no existe o está oculto responde 404 <c>technician_not_found</c>.</summary>
    [HttpPut("{tecnicoId}")]
    [RequestSizeLimit(MaxBodyBytes)]
    [ProducesResponseType<TrustedTechnician>(StatusCodes.Status200OK)]
    [ProducesResponseType<TrustedTechnician>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, _problemJson)]
    public async Task<IActionResult> Save([FromRoute] TrustedTechnicianRoute route, CancellationToken cancellationToken)
    {
        var outcome = await service.SaveAsync(Subject(), route.TecnicoId!, cancellationToken);
        return outcome.Status switch
        {
            TrustSaveStatus.Created => Created(new Uri($"/api/v1/me/tecnicos-confianza/{route.TecnicoId}", UriKind.Relative), outcome.Technician),
            TrustSaveStatus.AlreadySaved => Ok(outcome.Technician),
            _ => Rule(StatusCodes.Status404NotFound, TrustErrorCodes.TechnicianNotFound),
        };
    }

    /// <summary>Quita al técnico de la lista: 204; si no estaba, 404 <c>trusted_technician_not_found</c>.</summary>
    [HttpDelete("{tecnicoId}")]
    [RequestSizeLimit(MaxBodyBytes)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, _problemJson)]
    public async Task<IActionResult> Remove([FromRoute] TrustedTechnicianRoute route, CancellationToken cancellationToken) =>
        await service.RemoveAsync(Subject(), route.TecnicoId!, cancellationToken) ? NoContent() : Rule(StatusCodes.Status404NotFound, TrustErrorCodes.TrustedTechnicianNotFoundCode);

    private string Subject() => User.FindFirst("sub")?.Value ?? string.Empty;

    private ObjectResult Rule(int status, string code)
    {
        var problem = ProblemDetailsFactory.CreateProblemDetails(HttpContext, status, ReasonPhrases.GetReasonPhrase(status));
        problem.Extensions["code"] = code;
        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { _problemJson } };
    }
}
