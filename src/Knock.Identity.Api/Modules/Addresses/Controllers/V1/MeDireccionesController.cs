using Asp.Versioning;
using Knock.Identity.Api.Hosting;
using Knock.Identity.Api.Modules.Addresses.Domain;
using Knock.Identity.Api.Modules.Addresses.Dtos;
using Knock.Identity.Api.Modules.Addresses.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace Knock.Identity.Api.Modules.Addresses.Controllers.V1;

/// <summary>
/// Direcciones propias (spec 040, DIR-01 a DIR-04). Ninguna ruta, consulta ni cuerpo acepta el identificador de otra persona: la persona es siempre la del <c>sub</c> del
/// token (R-15) y una dirección ajena no se distingue de una que no existe (R-14).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/me/direcciones")]
[Authorize]
[PrivateNoStore]
internal sealed class MeDireccionesController(AddressService addresses) : ControllerBase
{
    /// <summary>Límite del cuerpo de <c>POST</c> y <c>PUT</c>: 4 KiB (calle, colonia y referencias de hasta 200 caracteres); más responde 413 (spec 009).</summary>
    public const int MaxBodyBytes = 4096;

    /// <summary>Lista las direcciones de quien llama, la más reciente primero. Sin paginar: una persona tiene pocas.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AddressResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        if (Sub() is not { } sub)
        {
            return Unauthorized();
        }

        var list = await addresses.ListAsync(sub, cancellationToken);
        return list is null ? Unauthorized() : Ok(list.Select(ToResponse));
    }

    /// <summary>
    /// Guarda una dirección nueva. Un valor inválido responde 422 con <c>errors.&lt;campo&gt;</c> sin repetirlo; una colonia que no está en el catálogo, 422 en <c>errors.colonia</c>.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(MaxBodyBytes)]
    [ProducesResponseType<AddressResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Create([FromBody] CreateAddressRequest request, CancellationToken cancellationToken)
    {
        if (Sub() is not { } sub)
        {
            return Unauthorized();
        }

        var result = await addresses.CreateAsync(sub, AddressInput.From(request), cancellationToken);
        return result.Outcome switch
        {
            AddressOutcome.NoAccount => Unauthorized(),
            AddressOutcome.ColoniaNotFound => await ColoniaNotFoundAsync(),
            _ => StatusCode(StatusCodes.Status201Created, ToResponse(result.Address!)),
        };
    }

    /// <summary>Edita los campos enviados de una dirección propia. Una dirección ajena responde 404 igual que una que no existe (R-14).</summary>
    [HttpPut("{id}")]
    [RequestSizeLimit(MaxBodyBytes)]
    [ProducesResponseType<AddressResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Update([FromRoute] AddressRoute route, [FromBody] UpdateAddressRequest request, CancellationToken cancellationToken)
    {
        if (Sub() is not { } sub)
        {
            return Unauthorized();
        }

        var result = await addresses.UpdateAsync(sub, ObjectId.Parse(route.Id), AddressEdit.From(request), cancellationToken);
        return result.Outcome switch
        {
            AddressOutcome.NoAccount => Unauthorized(),
            AddressOutcome.ColoniaNotFound => await ColoniaNotFoundAsync(),
            _ => Ok(ToResponse(result.Address!)),
        };
    }

    private Task<IActionResult> ColoniaNotFoundAsync() =>
        ValidationFilter.WriteErrorsAsync(HttpContext, new Dictionary<string, string[]> { ["colonia"] = ["The colonia does not exist in the catalog."] });

    private static AddressResponse ToResponse(Address address) =>
        new(address.Id, address.Street, address.Colonia, address.References, address.Principal, address.CreatedAt);

    private string? Sub() => User.FindFirst("sub")?.Value is { Length: > 0 } sub ? sub : null;
}
