namespace Knock.Market.Api.Modules.Tecnicos.Dtos;

/// <summary>Cuerpo de <c>POST /tecnicos</c> (spec 046, TEC-01, R-02 y R-39). El 422 de forma lo da <c>RegisterTechnicianBodyValidator</c>; nunca repite el valor recibido.</summary>
/// <param name="Nombre">Nombre y apellido que ven los clientes: de 2 a 80 letras, espacios, punto, apóstrofo o guion.</param>
/// <param name="Oficios">Una o más claves de oficios activos del catálogo, sin repetir y sin máximo propio (D-07).</param>
/// <param name="OtroOficio">Texto libre opcional («Otro oficio»), de hasta 60 caracteres; no recibe solicitudes.</param>
/// <param name="Atencion">Una de <c>taller</c>, <c>domicilio</c> o <c>ambos</c>.</param>
/// <param name="DireccionTaller">De 8 a 200 caracteres; obligatoria con taller o ambos y no admitida a domicilio. Se guarda cifrada.</param>
/// <param name="RadioKm">3, 5, 8 o 12; obligatorio a domicilio o ambos y no admitido solo en taller.</param>
/// <param name="ColoniaId">Colonia base del técnico (24 hexadecimales); de ella sale la ubicación aproximada.</param>
/// <param name="Perfil">Datos opcionales del perfil público.</param>
/// <param name="AceptaTerminos">Debe ser <see langword="true"/>.</param>
internal sealed record RegisterTechnicianBody(
    string? Nombre,
    IReadOnlyList<string>? Oficios,
    string? OtroOficio,
    string? Atencion,
    string? DireccionTaller,
    int? RadioKm,
    string? ColoniaId,
    TechnicianProfileBody? Perfil,
    bool? AceptaTerminos)
{
    /// <summary>Tope del cuerpo JSON: 8 KB (spec 009).</summary>
    public const int MaxBodyBytes = 8 * 1024;
}

/// <param name="NombreNegocio">Hasta 60 caracteres.</param>
/// <param name="Descripcion">Hasta 300 caracteres.</param>
/// <param name="Anios">Años de experiencia, de 0 a 60.</param>
internal sealed record TechnicianProfileBody(string? NombreNegocio, string? Descripcion, int? Anios);

/// <summary>Perfil propio recién creado (201). Nunca trae la dirección del taller, el teléfono ni la ubicación.</summary>
internal sealed record TechnicianResponse(
    string Id,
    string Nombre,
    IReadOnlyList<string> Oficios,
    string? OtroOficio,
    string Atencion,
    int? RadioKm,
    string ColoniaId,
    string? NombreNegocio,
    string? Descripcion,
    int? Anios,
    string Estado,
    string Verificacion,
    bool DisponibleHoy,
    DateTime CreadoEn);
