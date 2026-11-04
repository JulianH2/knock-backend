namespace Knock.Api.Modules.Identity.Dtos;

/// <summary>
/// Cuerpo de <c>PUT /me</c>. El campo es anulable a propósito y no se declara requerido en el esquema: si falta, el validador responde 422 con
/// <c>errors.nombre</c> en lugar del 400 automático de MVC. Cualquier otro campo del JSON (<c>id</c>, <c>sub</c>, <c>estado</c>, <c>perfilCompleto</c>) no
/// existe aquí y se ignora: la identidad sale solo del JWT.
/// </summary>
internal sealed record ProfileRequest(string? Nombre = null);
