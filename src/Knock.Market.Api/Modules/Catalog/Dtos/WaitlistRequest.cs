namespace Knock.Market.Api.Modules.Catalog.Dtos;

internal sealed record WaitlistRequest(string? Cp, string? Municipio);

internal sealed record WaitlistResponse(string Id, string Cp, string? Municipio, DateTime CreadoEn, bool YaRegistrado);
