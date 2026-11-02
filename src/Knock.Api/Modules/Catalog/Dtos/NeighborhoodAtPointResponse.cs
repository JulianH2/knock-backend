namespace Knock.Api.Modules.Catalog.Dtos;

internal sealed record NeighborhoodAtPointResponse(
    bool EnCobertura,
    NeighborhoodAtPointColoniaResponse? Colonia);

internal sealed record NeighborhoodAtPointColoniaResponse(
    string Id,
    string Nombre,
    string Cp,
    string Municipio);
