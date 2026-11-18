namespace Knock.Identity.Api.Modules.Addresses.Dtos;

/// <summary>
/// Cuerpo del <c>POST /me/direcciones</c> (spec 040, R-03). Solo estos cuatro campos: cualquier otro del JSON (<c>usuarioId</c>, <c>id</c>, <c>creadoEn</c>…) no existe aquí, así
/// que se ignora (R-15). Los campos son anulables a propósito: la validación propia responde 422 por campo en lugar del 400 automático de MVC.
/// </summary>
internal sealed record CreateAddressRequest(string? CalleNumero = null, string? Colonia = null, string? Referencias = null, bool? Principal = null);

/// <summary>
/// Dirección de la persona (spec 040, §5). Solo la ve su dueña; nunca lleva el identificador de la persona.
/// </summary>
internal sealed record AddressResponse(string Id, string CalleNumero, string Colonia, string? Referencias, bool Principal, DateTime CreadoEn);

/// <summary>
/// Cuerpo del <c>PUT /me/direcciones/{id}</c> (spec 040, R-05 y R-10): solo cambian los campos enviados. Omitir un campo (o enviarlo <c>null</c>) lo deja igual; en las
/// referencias, un texto vacío las borra. Cualquier otro campo del JSON (<c>usuarioId</c>, <c>id</c>…) se ignora (R-15).
/// </summary>
internal sealed record UpdateAddressRequest(string? CalleNumero = null, string? Colonia = null, string? Referencias = null, bool? Principal = null);

/// <summary>Identificador de la dirección en la ruta. Es un objeto con validador (no un <c>string</c> suelto): un valor que no es un identificador responde 422 en <c>id</c> (spec 040, §7).</summary>
internal sealed class AddressRoute
{
    [Microsoft.AspNetCore.Mvc.FromRoute(Name = "id")]
    public string? Id { get; set; }
}
