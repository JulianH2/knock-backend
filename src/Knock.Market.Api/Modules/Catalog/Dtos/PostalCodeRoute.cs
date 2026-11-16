using Microsoft.AspNetCore.Mvc;

namespace Knock.Market.Api.Modules.Catalog.Dtos;

internal sealed class PostalCodeRoute
{
    [FromRoute(Name = "cp")]
    public string? Cp { get; set; }
}

internal sealed record ZonaResponse(bool EnCobertura, string CiudadPiloto, IReadOnlyList<ZonaColoniaResponse> Colonias);

internal sealed record ZonaColoniaResponse(string Id, string Nombre, string Cp, string Municipio);
