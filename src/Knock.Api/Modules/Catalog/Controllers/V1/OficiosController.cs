using Asp.Versioning;
using Knock.Api.Modules.Catalog.Dtos;
using Knock.Api.Modules.Catalog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Api.Modules.Catalog.Controllers.V1;

/// <summary>Catálogo público de oficios (spec TT-PRU-01): la app lo necesita antes o durante el registro.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/oficios")]
internal sealed class OficiosController(OficioQueries oficios) : ControllerBase
{
    /// <summary>
    /// Oficios activos como arreglo sin paginar: excepción documentada a la constitución §11 (catálogo acotado, D9).
    /// <c>[AllowAnonymous]</c> explícito para que siga público aunque se agregue una política de autorización global (D3).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<OficioResponse>>(StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await oficios.ListActiveAsync(cancellationToken));
}
