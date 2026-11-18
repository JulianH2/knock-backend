using Asp.Versioning;
using Knock.Contracts.Pagination;
using Knock.Identity.Api.Hosting;
using Knock.Identity.Api.Modules.HomeAndCar.Dtos;
using Knock.Identity.Api.Modules.HomeAndCar.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Identity.Api.Modules.HomeAndCar.Controllers.V1;

/// <summary>
/// Mi casa y mi auto (spec 041). La ruta nunca lleva el identificador de la persona: es siempre la del <c>sub</c> del token (R-20), así nadie puede pedir ni cambiar lo de otra.
/// Un elemento ajeno responde igual que uno inexistente (404, R-16); por eso ninguna ruta responde 403.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/me/casa-y-auto")]
[Authorize]
internal sealed class CasaYAutoController(HomeAndCarService service) : ControllerBase
{
    /// <summary>Límite del cuerpo de <c>POST</c> y <c>PUT</c>: 4 KiB (tres campos cortos); un cuerpo mayor responde 413 antes de leerse (spec 009).</summary>
    public const int MaxBodyBytes = 4096;

    /// <summary>Lista los autos y las cosas de la casa de la persona (CAS-01).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        return Sub() is { } sub ? Ok(await service.ListAsync(sub, cancellationToken)) : Unauthorized();
    }

    /// <summary>Registra un auto o una cosa de la casa (CAS-02). Con 20 elementos ya registrados responde 409 <c>item_limit_reached</c>.</summary>
    [HttpPost]
    [RequestSizeLimit(MaxBodyBytes)]
    [ProducesResponseType<ItemResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Create([FromBody] CreateItemRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        if (Sub() is not { } sub)
        {
            return Unauthorized();
        }

        var item = await service.CreateAsync(sub, request, cancellationToken);
        return Created(new Uri($"{Request.Path.Value!.TrimEnd('/')}/{item.Id}", UriKind.Relative), item);
    }

    /// <summary>Edita un elemento propio (CAS-03): cambian solo los campos enviados. Uno ajeno o inexistente responde 404.</summary>
    [HttpPut("{id}")]
    [RequestSizeLimit(MaxBodyBytes)]
    [ProducesResponseType<ItemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Update([FromRoute] ItemRoute route, [FromBody] UpdateItemRequest request, CancellationToken cancellationToken)
    {
        return await AsOwnerAsync(async sub =>
        {
            var outcome = await service.UpdateAsync(sub, route.Id!, request, cancellationToken);
            return outcome.Item is { } item ? Ok(item) : await ValidationFilter.WriteErrorsAsync(HttpContext, outcome.Errors!);
        });
    }

    /// <summary>Quita un elemento propio (CAS-04). Uno ajeno o inexistente responde 404. Los trabajos terminados de Mercado no se tocan.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Delete([FromRoute] ItemRoute route, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        if (Sub() is not { } sub)
        {
            return Unauthorized();
        }

        await service.DeleteAsync(sub, route.Id!, cancellationToken);
        return NoContent();
    }

    /// <summary>Trabajos terminados ligados a un elemento propio (CAS-05), del más reciente al más antiguo y por cursor. Uno ajeno o inexistente responde 404; si Mercado no responde, 503 <c>market_unavailable</c>.</summary>
    [HttpGet("{id}/historial")]
    [ProducesResponseType<CursorPage<HistoryItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> History([FromRoute] ItemRoute route, [FromQuery] HistoryQuery query, CancellationToken cancellationToken)
    {
        return await AsOwnerAsync(async sub =>
        {
            var outcome = await service.HistoryAsync(sub, route.Id!, query, cancellationToken);
            return outcome.Page is { } page ? Ok(page) : await ValidationFilter.WriteErrorsAsync(HttpContext, outcome.Errors!);
        });
    }

    /// <summary>Sin cuerpo en caché y con el <c>sub</c> del token: lo único que toda ruta de esta spec comparte (R-20).</summary>
    private Task<IActionResult> AsOwnerAsync(Func<string, Task<IActionResult>> action)
    {
        Response.Headers.CacheControl = "private, no-store";
        return Sub() is { } sub ? action(sub) : Task.FromResult<IActionResult>(Unauthorized());
    }

    private string? Sub() => User.FindFirst("sub")?.Value is { Length: > 0 } sub ? sub : null;
}
