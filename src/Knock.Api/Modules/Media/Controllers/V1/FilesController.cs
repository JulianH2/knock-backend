using System.ComponentModel.DataAnnotations;
using Asp.Versioning;
using Knock.Api.Modules.Media.Dtos;
using Knock.Api.Modules.Media.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Api.Modules.Media.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/archivos")]
[Authorize]
internal sealed class FilesController(MediaService media) : ControllerBase
{
    /// <summary>Límite del cuerpo JSON: 1 KiB. La foto nunca pasa por la API (va directo a Blob con el SAS).</summary>
    public const int MaxBodyBytes = 1024;

    [HttpPost("subidas")]
    [RequestSizeLimit(MaxBodyBytes)]
    [ProducesResponseType<UploadCreatedResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateUpload([FromBody] CreateUploadRequest request, CancellationToken cancellationToken)
    {
        if (OwnerId is not { } ownerId)
        {
            return Unauthorized();
        }

        var outcome = await media.CreateUploadAsync(ownerId, request.Usage, request.ContentType, request.SizeBytes, cancellationToken);
        return ToResult(outcome, ticket => StatusCode(
            StatusCodes.Status201Created, new UploadCreatedResponse(ticket.Id, ticket.Link.Url, ticket.Link.ExpiresAt)));
    }

    [HttpPost("{id:guid}/confirmar")]
    [RequestSizeLimit(MaxBodyBytes)]
    [ProducesResponseType<ConfirmedFileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken cancellationToken)
    {
        if (OwnerId is not { } ownerId)
        {
            return Unauthorized();
        }

        var outcome = await media.ConfirmAsync(ownerId, id, cancellationToken);
        return ToResult(outcome, file => Ok(new ConfirmedFileResponse(file.Id, file.State, file.Width, file.Height)));
    }

    [HttpGet("{id:guid}/lectura")]
    [ProducesResponseType<ReadLinkResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Read(
        Guid id,
        [FromQuery(Name = "version"), RegularExpression("^(original|reducida)$")] string version = "original",
        CancellationToken cancellationToken = default)
    {
        if (OwnerId is not { } ownerId)
        {
            return Unauthorized();
        }

        var outcome = await media.CreateOwnerReadLinkAsync(ownerId, id, version == "reducida", cancellationToken);
        return ToResult(outcome, link => Ok(new ReadLinkResponse(link.Url, link.ExpiresAt)));
    }

    /// <summary>El dueño sale del <c>sub</c> del JWT, nunca de la petición.</summary>
    private string? OwnerId => User.FindFirst("sub")?.Value is { Length: > 0 } sub ? sub : null;

    private IActionResult ToResult<T>(MediaOutcome<T> outcome, Func<T, IActionResult> onSuccess) => outcome.Status switch
    {
        MediaOutcomeStatus.Success => onSuccess(outcome.Value!),
        MediaOutcomeStatus.Conflict => Problem(statusCode: StatusCodes.Status409Conflict, title: "Conflict", detail: outcome.Detail),
        MediaOutcomeStatus.Unprocessable => Problem(statusCode: StatusCodes.Status422UnprocessableEntity, title: "Invalid file", detail: outcome.Detail),
        _ => Problem(statusCode: StatusCodes.Status404NotFound, title: "Not found"),
    };
}
