namespace Knock.Identity.Api.Modules.Addresses.Dtos;

/// <summary>
/// Dirección de la persona (spec 040, §5). Solo la ve su dueña; nunca lleva el identificador de la persona.
/// </summary>
internal sealed record AddressResponse(string Id, string CalleNumero, string Colonia, string? Referencias, bool Principal, DateTime CreadoEn);
