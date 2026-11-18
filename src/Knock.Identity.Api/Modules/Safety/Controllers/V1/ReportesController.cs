using Asp.Versioning;
using Knock.Identity.Api.Hosting;
using Knock.Identity.Api.Modules.Safety.Dtos;
using Knock.Identity.Api.Modules.Safety.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Identity.Api.Modules.Safety.Controllers.V1;

/// <summary>
/// Reportes de una persona contra otra y su apelación (spec 043, REP-01 a REP-03). Recibe, valida el formato y delega en <see cref="ReportService"/>; no toca MongoDB. La persona es siempre
/// la del <c>sub</c> del token: ninguna ruta, consulta ni cuerpo acepta el identificador de otra persona para actuar en su nombre (R-24). Nunca se cachea.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize]
internal sealed class ReportesController(ReportService service) : ControllerBase
{
    private const string _problemJson = "application/problem+json";

    /// <summary>
    /// Crea un reporte (<c>abierto</c>). 201 la primera vez y 200 si ya había uno abierto igual; 422 con <c>errors.&lt;campo&gt;</c> si un valor no es válido, si es contra uno mismo o contra alguien
    /// que no existe, o <c>content_rejected</c>; 503 <c>moderation_unavailable</c> si la moderación no responde. Con motivo de peligro la respuesta lleva el aviso de llamar al 911.
    /// </summary>
    [HttpPost("api/v{version:apiVersion}/reportes")]
    [RequestSizeLimit(CreateReportBody.MaxBodyBytes)]
    [ProducesResponseType<ReportCreated>(StatusCodes.Status201Created)]
    [ProducesResponseType<ReportCreated>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, _problemJson)]
    public async Task<IActionResult> Create([FromBody] CreateReportBody body, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        var creation = await service.CreateAsync(Subject(), body, cancellationToken);
        return creation.Outcome switch
        {
            ReportOutcome.Created => Created(new Uri("/api/v1/reportes/me", UriKind.Relative), creation.Report),
            ReportOutcome.AlreadyOpen => Ok(creation.Report),
            _ => await ValidationFilter.WriteErrorsAsync(HttpContext, new Dictionary<string, string[]> { ["reportadoId"] = [ValidationFilter.GenericReason] }),
        };
    }

    /// <summary>Los reportes que hizo quien llama, del más reciente al más antiguo, con paginación por cursor. Nunca lleva datos de la otra parte.</summary>
    [HttpGet("api/v{version:apiVersion}/reportes/me")]
    [ProducesResponseType<ReportPage>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, _problemJson)]
    public async Task<ActionResult<ReportPage>> List([FromQuery] ReportsQuery query, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        return await service.ListAsync(Subject(), query, cancellationToken);
    }

    /// <summary>Apela la decisión de un reporte, una sola vez. 200; 403 si no es una de las dos partes; 409 <c>appeal_not_allowed</c> sin decisión o con una apelación previa.</summary>
    [HttpPost("api/v{version:apiVersion}/reportes/{id}/apelacion")]
    [RequestSizeLimit(AppealBody.MaxBodyBytes)]
    [ProducesResponseType<AppealResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, _problemJson)]
    public async Task<ActionResult<AppealResult>> Appeal([FromRoute] ReportRoute route, [FromBody] AppealBody body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        Response.Headers.CacheControl = "private, no-store";
        return await service.AppealAsync(Subject(), route.Id!, body.Texto!, cancellationToken);
    }

    private string Subject() => User.FindFirst("sub")?.Value ?? string.Empty;
}
