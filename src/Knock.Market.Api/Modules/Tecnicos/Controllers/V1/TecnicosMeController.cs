using Asp.Versioning;
using Knock.Market.Api.Common.Errors;
using Knock.Market.Api.Modules.Tecnicos.Dtos;
using Knock.Market.Api.Modules.Tecnicos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Market.Api.Modules.Tecnicos.Controllers.V1;

/// <summary>
/// Perfil propio del técnico (spec 046, TEC-08 a TEC-11). La persona es la del <c>sub</c> del token; una cuenta sin perfil de técnico recibe 404 (403 en la
/// disponibilidad, CA-38) y no cambia nada. Ninguna respuesta se cachea.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tecnicos/me")]
[Authorize]
internal sealed class TecnicosMeController(MyTechnicianService service) : ControllerBase
{
    private const string _problemJson = TechnicianProblems.ProblemJson;

    /// <summary>Edita el perfil público (TEC-08): cambian solo los campos enviados.</summary>
    [HttpPut]
    [RequestSizeLimit(UpdateTechnicianBody.MaxBodyBytes)]
    [ProducesResponseType<TechnicianResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, _problemJson)]
    public async Task<IActionResult> Update([FromBody] UpdateTechnicianBody body, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        var outcome = await service.UpdateAsync(Subject(), body, cancellationToken);
        return outcome.Status switch
        {
            UpdateTechnicianStatus.Updated => Ok(outcome.Technician),
            UpdateTechnicianStatus.NotATechnician => Rule(StatusCodes.Status404NotFound, code: null),
            UpdateTechnicianStatus.TradeInvalid => Rule(StatusCodes.Status422UnprocessableEntity, ErrorCodes.TradeInvalidCode, outcome.Field),
            _ => Rule(StatusCodes.Status422UnprocessableEntity, ErrorCodes.ValidationFailedCode, outcome.Field),
        };
    }

    /// <summary>Enciende o apaga «Disponible hoy» (TEC-09).</summary>
    [HttpPut("disponibilidad")]
    [RequestSizeLimit(AvailabilityBody.MaxBodyBytes)]
    [ProducesResponseType<AvailabilityResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, _problemJson)]
    public async Task<IActionResult> Availability([FromBody] AvailabilityBody body, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        var result = await service.SetAvailabilityAsync(Subject(), body.DisponibleHoy!.Value, cancellationToken);
        return result is null ? Rule(StatusCodes.Status403Forbidden, ErrorCodes.NotATechnicianCode) : Ok(result);
    }

    /// <summary>Horarios de los siete días (TEC-10); sin horario guardado, todos cerrados.</summary>
    [HttpGet("horarios")]
    [ProducesResponseType<ScheduleBody>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, _problemJson)]
    public async Task<IActionResult> Schedule(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        var schedule = await service.GetScheduleAsync(Subject(), cancellationToken);
        return schedule is null ? Rule(StatusCodes.Status404NotFound, code: null) : Ok(new ScheduleBody(schedule));
    }

    /// <summary>Reemplaza los horarios de los siete días (TEC-11).</summary>
    [HttpPut("horarios")]
    [RequestSizeLimit(ScheduleBody.MaxBodyBytes)]
    [ProducesResponseType<ScheduleBody>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, _problemJson)]
    public async Task<IActionResult> ReplaceSchedule([FromBody] ScheduleBody body, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        var schedule = await service.SetScheduleAsync(Subject(), body.Horarios!, cancellationToken);
        return schedule is null ? Rule(StatusCodes.Status404NotFound, code: null) : Ok(new ScheduleBody(schedule));
    }

    private string Subject() => User.FindFirst("sub")?.Value ?? string.Empty;

    private ObjectResult Rule(int status, string? code, string? field = null) => TechnicianProblems.Rule(this, status, code, field);
}
