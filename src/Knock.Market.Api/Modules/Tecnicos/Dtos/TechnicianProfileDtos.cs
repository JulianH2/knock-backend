using Microsoft.AspNetCore.Mvc;

namespace Knock.Market.Api.Modules.Tecnicos.Dtos;

/// <summary>Identificador de técnico en la ruta (spec 046, R-15): 24 hexadecimales; si no, 422 <c>errors.id</c>.</summary>
internal sealed class TechnicianIdRoute
{
    [FromRoute(Name = "id")]
    public string? Id { get; set; }
}

/// <summary>
/// Perfil público de un técnico (spec 046, TEC-05, R-42). Sin sesión trae solo el resumen y <see cref="Detalle"/> va en <see langword="null"/>; con sesión
/// suma el detalle. Nunca trae teléfono, dirección del taller ni ubicación. <c>verificacion</c> es <c>ninguno</c>, <c>basica</c> o <c>completa</c> (spec 025);
/// <c>calificacion</c> es el promedio con un decimal o <see langword="null"/>; <c>descripcionCorta</c> son hasta 140 caracteres de la descripción.
/// </summary>
internal sealed record TechnicianProfileResponse(
    string Id,
    string Nombre,
    string? NombreNegocio,
    string? FotoUrl,
    string Verificacion,
    double? Calificacion,
    int? Anios,
    string? DescripcionCorta,
    IReadOnlyList<string> Oficios,
    TechnicianProfileDetail? Detalle);

/// <summary>
/// Parte del perfil que solo ve una persona con sesión: descripción completa, forma de atención, radio a domicilio (<see langword="null"/> si solo atiende en
/// taller), colonia base y «Disponible hoy». Los servicios con precio (spec 047), las fotos de trabajos (TEC-07) y los horarios (TEC-10) tienen sus propias rutas.
/// </summary>
internal sealed record TechnicianProfileDetail(string? Descripcion, string? Atencion, int? RadioKm, string? ColoniaId, bool DisponibleHoy);
