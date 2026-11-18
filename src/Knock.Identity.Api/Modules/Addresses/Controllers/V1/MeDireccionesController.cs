using Asp.Versioning;
using Knock.Identity.Api.Modules.Addresses.Domain;
using Knock.Identity.Api.Modules.Addresses.Dtos;
using Knock.Identity.Api.Modules.Addresses.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    private static AddressResponse ToResponse(Address address) =>
        new(address.Id, address.Street, address.Colonia, address.References, address.Principal, address.CreatedAt);

    private string? Sub() => User.FindFirst("sub")?.Value is { Length: > 0 } sub ? sub : null;
}
