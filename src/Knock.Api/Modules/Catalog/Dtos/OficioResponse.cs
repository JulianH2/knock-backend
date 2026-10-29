namespace Knock.Api.Modules.Catalog.Dtos;

/// <summary>Oficio activo del catálogo tal como lo ve la app: solo clave, nombre y modalidad (nunca id ni activo).</summary>
/// <param name="Clave">Clave estable del catálogo (≤ 16 caracteres).</param>
/// <param name="Nombre">Nombre para mostrar, en español (≤ 100 caracteres).</param>
/// <param name="Modalidad">Valores conocidos: <c>domicilio</c> o <c>taller</c>. Es texto, no enum cerrado, para admitir modalidades futuras.</param>
internal sealed record OficioResponse(string Clave, string Nombre, string Modalidad);
