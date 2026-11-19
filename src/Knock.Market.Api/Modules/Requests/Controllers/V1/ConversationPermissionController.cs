using Asp.Versioning;
using Knock.Contracts.Conversations;
using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Requests.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Market.Api.Modules.Requests.Controllers.V1;

/// <summary>
/// Permiso de conversación (spec 030, R-02): lo consulta Mensajería por la red interna con la sesión de quien pide. <b>No se publica en el gateway</b>, como <c>GET /colonias/existe</c>
/// (spec 017). Recibe, valida el formato y delega en <see cref="ConversationPermissionService"/>; no toca MongoDB. Nunca se cachea (<c>Cache-Control: private, no-store</c>).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/solicitudes/{id}/conversacion")]
[Authorize]
[PrivateNoStore]
internal sealed class ConversationPermissionController(ConversationPermissionService service) : ControllerBase
{
    private const string _problemJson = "application/problem+json";

    /// <summary>
    /// El permiso del par (solicitud, técnico) para quien pide: el cliente dueño debe mandar <c>tecnicoId</c>; el técnico no (se ignora si lo manda). Cualquier otro caso es 404 uniforme.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PermisoConversacion>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, _problemJson)]
    public async Task<ActionResult<PermisoConversacion>> Get(string id, [FromQuery] string? tecnicoId, CancellationToken cancellationToken) =>
        await service.GetAsync(User.FindFirst("sub")?.Value ?? string.Empty, id, tecnicoId, cancellationToken) is { } permission ? permission : NotFound();
}
