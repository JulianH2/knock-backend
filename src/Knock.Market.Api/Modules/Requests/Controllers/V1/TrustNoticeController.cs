using Asp.Versioning;
using Knock.Market.Api.Modules.Requests.Domain;
using Knock.Market.Api.Modules.Requests.Dtos;
using Knock.Market.Api.Modules.Requests.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Market.Api.Modules.Requests.Controllers.V1;

/// <summary>
/// Aviso de confianza de la visita (spec 022, R-12 y R-13): prepara un texto; no guarda contactos ni envía mensajes. Recibe y delega en <see cref="TrustNoticeService"/>; no
/// toca MongoDB. Nunca se cachea (<c>Cache-Control: private, no-store</c>).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/visitas/{id}/aviso")]
[Authorize]
[PrivateNoStore]
internal sealed class TrustNoticeController(TrustNoticeService service) : BusinessRuleController
{
    /// <summary>El cliente dueño obtiene el texto para compartir con alguien de confianza. Solo con la visita <c>confirmada</c> o <c>en_curso</c>.</summary>
    [HttpPost]
    [ProducesResponseType<TrustNotice>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, ProblemJson)]
    public async Task<IActionResult> Prepare(string id, CancellationToken cancellationToken)
    {
        var outcome = await service.PrepareAsync(Subject(), id, cancellationToken);
        return outcome.Status switch
        {
            TrustNoticeStatus.Prepared => Ok(outcome.Notice),
            TrustNoticeStatus.NotFound => NotFound(),
            _ => Rule(StatusCodes.Status409Conflict, VisitErrorCodes.VisitNotNotifiableCode),
        };
    }
}
