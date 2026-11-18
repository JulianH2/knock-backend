using Asp.Versioning;
using Knock.Contracts.Pagination;
using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Requests.Dtos;
using Knock.Market.Api.Modules.Requests.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Market.Api.Modules.Requests.Controllers.V1;

/// <summary>
/// Ruta interna del historial de «Mi casa y mi auto» (spec 041, R-17): solo la llama Identidad con su token de servicio (<see cref="InternalServiceAuthentication"/>) y el gateway no la
/// enruta, así que no se alcanza desde fuera. Además de la firma, el token lleva el cliente y el elemento que se piden y deben coincidir con la consulta: un token capturado no sirve
/// para leer el historial de otra persona. Nunca se cachea.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/interno/historial")]
[Authorize(Policy = InternalServiceAuthentication.HistoryPolicy)]
[PrivateNoStore]
internal sealed class InternalHistoryController(HistoryService service) : BusinessRuleController
{
    public const string ClientClaim = "usuarioId";
    public const string ElementClaim = "elementoId";

    /// <summary>Visitas terminadas del cliente ligadas al elemento, de la más reciente a la más antigua, por cursor. Sin precios ni direcciones.</summary>
    [HttpGet]
    [ProducesResponseType<CursorPage<HistoryEntryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, ProblemJson)]
    public async Task<IActionResult> List([FromQuery] InternalHistoryQuery query, CancellationToken cancellationToken)
    {
        if (!string.Equals(User.FindFirst(ClientClaim)?.Value, query.UsuarioId, StringComparison.Ordinal)
            || !string.Equals(User.FindFirst(ElementClaim)?.Value, query.ElementoId, StringComparison.OrdinalIgnoreCase))
        {
            return Rule(StatusCodes.Status403Forbidden, InternalServiceAuthentication.ServiceNotAllowedCode);
        }

        return Ok(await service.ListAsync(query.UsuarioId!, query.ElementoId!, query.Cursor, query.Limite, cancellationToken));
    }
}
