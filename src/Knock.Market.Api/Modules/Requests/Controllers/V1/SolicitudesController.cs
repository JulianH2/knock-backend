using Asp.Versioning;
using Knock.Market.Api.Common.Errors;
using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Requests.Domain;
using Knock.Market.Api.Modules.Requests.Dtos;
using Knock.Market.Api.Modules.Requests.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;

namespace Knock.Market.Api.Modules.Requests.Controllers.V1;

/// <summary>
/// Solicitudes de cotización (spec 020, HU-024): crear, listar las propias y ver el detalle según el rol. Reciben, validan el formato y delegan en
/// <see cref="SolicitudService"/>; no tocan MongoDB. Ninguna respuesta se cachea (<c>Cache-Control: private, no-store</c>).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/solicitudes")]
[Authorize]
[PrivateNoStore]
internal sealed class SolicitudesController(SolicitudService service) : ControllerBase
{
    private const string _problemJson = "application/problem+json";

    /// <summary>Crea la solicitud en estado <c>enviada</c> y con <c>venceEn</c> a 48 h.</summary>
    [HttpPost]
    [RequestSizeLimit(CreateRequestBody.MaxBodyBytes)]
    [EnableRateLimiting(RateLimitingExtensions.CreateRequestPolicy)]
    [ProducesResponseType<RequestResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, _problemJson)]
    public async Task<IActionResult> Create([FromBody] CreateRequestBody body, CancellationToken cancellationToken)
    {
        var outcome = await service.CreateAsync(Subject(), body, cancellationToken);
        return outcome.Status switch
        {
            CreateRequestStatus.Created => Created(new Uri($"/api/v1/solicitudes/{outcome.Request!.Id}", UriKind.Relative), outcome.Request),
            CreateRequestStatus.TradeInvalid => Rule(StatusCodes.Status422UnprocessableEntity, ErrorCodes.TradeInvalidCode, "oficioClave"),
            CreateRequestStatus.ColoniaNotFound => Rule(StatusCodes.Status422UnprocessableEntity, ErrorCodes.ColoniaNotFoundCode, "coloniaId"),
            CreateRequestStatus.AddressRequired => Rule(StatusCodes.Status422UnprocessableEntity, code: null, "direccion"),
            CreateRequestStatus.AddressNotApplicable => Rule(StatusCodes.Status422UnprocessableEntity, RequestErrorCodes.AddressNotApplicableCode, "direccion"),
            CreateRequestStatus.PhotoInvalid => Rule(StatusCodes.Status422UnprocessableEntity, RequestErrorCodes.PhotoInvalidCode),
            CreateRequestStatus.ContentRejected => Rule(StatusCodes.Status422UnprocessableEntity, RequestErrorCodes.ContentRejectedCode),
            _ => Rule(StatusCodes.Status503ServiceUnavailable, RequestErrorCodes.ModerationUnavailableCode),
        };
    }

    /// <summary>Solicitudes del <c>sub</c> del token, de la más reciente a la más antigua, con paginación por cursor.</summary>
    [HttpGet]
    [ProducesResponseType<RequestPage>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, _problemJson)]
    public async Task<ActionResult<RequestPage>> List([FromQuery] ListRequestsQuery query, CancellationToken cancellationToken) =>
        await service.ListAsync(Subject(), query, cancellationToken);

    /// <summary>Detalle: vista del cliente dueño, o vista acotada del técnico elegible o que ya cotizó. Cualquier otra persona recibe 404.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType<RequestResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, _problemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, _problemJson)]
    public async Task<ActionResult<RequestResponse>> Get(string id, CancellationToken cancellationToken) =>
        await service.GetAsync(Subject(), id, cancellationToken) is { } request ? request : NotFound();

    private string Subject() => User.FindFirst("sub")?.Value ?? string.Empty;

    /// <summary>
    /// Regla de negocio con ProblemDetails (mismo formato que el resto de errores: <c>traceId</c> y <c>correlationId</c>), con <c>code</c> cuando la spec lo define y, si se
    /// nombra un campo, <c>errors</c> con un motivo genérico que nunca repite el valor recibido.
    /// </summary>
    private ObjectResult Rule(int status, string? code, string? field = null)
    {
        var problem = ProblemDetailsFactory.CreateProblemDetails(HttpContext, status, ReasonPhrases.GetReasonPhrase(status));
        if (code is not null)
        {
            problem.Extensions["code"] = code;
        }

        if (field is not null)
        {
            problem.Extensions["errors"] = new Dictionary<string, string[]> { [field] = [ValidationFilter.GenericReason] };
        }

        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { _problemJson } };
    }
}
