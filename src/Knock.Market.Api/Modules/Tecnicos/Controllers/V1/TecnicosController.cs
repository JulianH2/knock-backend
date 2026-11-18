using Asp.Versioning;
using Knock.Market.Api.Common.Errors;
using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Tecnicos.Domain;
using Knock.Market.Api.Modules.Tecnicos.Dtos;
using Knock.Market.Api.Modules.Tecnicos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;

namespace Knock.Market.Api.Modules.Tecnicos.Controllers.V1;

/// <summary>
/// Técnicos (spec 046). Recibe, valida el formato y delega en los servicios; no toca MongoDB. Las respuestas con datos propios no se cachean
/// (<c>Cache-Control: private, no-store</c>).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tecnicos")]
internal sealed class TecnicosController(TechnicianRegistrationService registration, TechnicianProfileService profiles) : ControllerBase
{
    private const string _problemJson = "application/problem+json";

    /// <summary>Registra a la persona del token como técnico (TEC-01): nace <c>activo</c>, sin verificación y con «Disponible hoy» apagado.</summary>
    [HttpPost]
    [Authorize]
    [RequestSizeLimit(RegisterTechnicianBody.MaxBodyBytes)]
    [EnableRateLimiting(RateLimitingExtensions.RegisterTechnicianPolicy)]
    [ProducesResponseType<TechnicianResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, _problemJson)]
    public async Task<IActionResult> Register([FromBody] RegisterTechnicianBody body, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        var subject = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(subject))
        {
            return Unauthorized();
        }

        var outcome = await registration.RegisterAsync(subject, body, cancellationToken);
        return outcome.Status switch
        {
            RegisterTechnicianStatus.Created => Created(new Uri($"/api/v1/tecnicos/{outcome.Technician!.Id}", UriKind.Relative), outcome.Technician),
            RegisterTechnicianStatus.AlreadyTechnician => Rule(StatusCodes.Status409Conflict, TechnicianRules.AlreadyTechnicianCode),
            RegisterTechnicianStatus.TradeInvalid => Rule(StatusCodes.Status422UnprocessableEntity, ErrorCodes.TradeInvalidCode, "oficios"),
            _ => Rule(StatusCodes.Status422UnprocessableEntity, ErrorCodes.ColoniaNotFoundCode, "coloniaId"),
        };
    }

    /// <summary>
    /// Perfil de un técnico (TEC-05): sin sesión, solo el resumen; con sesión válida, también el detalle. Un técnico que no existe o está oculto responde
    /// 404 <c>technician_not_found</c>.
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType<TechnicianProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, _problemJson)]
    public async Task<IActionResult> Profile([FromRoute] TechnicianIdRoute route, CancellationToken cancellationToken)
    {
        var signedIn = !string.IsNullOrWhiteSpace(User.FindFirst("sub")?.Value);
        Response.Headers.CacheControl = "private, no-store";
        var profile = await profiles.GetAsync(MongoDB.Bson.ObjectId.Parse(route.Id), signedIn, cancellationToken);
        return profile is null ? Rule(StatusCodes.Status404NotFound, TechnicianRules.TechnicianNotFoundCode) : Ok(profile);
    }

    /// <summary>Regla de negocio en ProblemDetails, con <c>code</c> y, si se nombra un campo, <c>errors</c> con un motivo genérico que nunca repite el valor.</summary>
    private ObjectResult Rule(int status, string code, string? field = null)
    {
        var problem = ProblemDetailsFactory.CreateProblemDetails(HttpContext, status, ReasonPhrases.GetReasonPhrase(status));
        problem.Extensions["code"] = code;
        if (field is not null)
        {
            problem.Extensions["errors"] = new Dictionary<string, string[]> { [field] = [ValidationFilter.GenericReason] };
        }

        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { _problemJson } };
    }
}
