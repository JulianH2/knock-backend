namespace Knock.Market.Api.Modules.Tecnicos.Dtos;

/// <summary>Cuerpo de <c>PUT /tecnicos/me/disponibilidad</c> (spec 046, TEC-09): el interruptor «Disponible hoy».</summary>
internal sealed record AvailabilityBody(bool? DisponibleHoy)
{
    public const int MaxBodyBytes = 1024;
}

/// <summary>Estado del interruptor: <c>disponibleHoy</c> es derivado de <c>disponibleHasta</c> (UTC), que con el interruptor encendido es hoy a las 23:59:59 de la ciudad.</summary>
internal sealed record AvailabilityResponse(bool DisponibleHoy, DateTime DisponibleHasta);

/// <summary>
/// Cuerpo de <c>PUT /tecnicos/me</c> (spec 046, TEC-08, R-45): solo cambian los campos enviados; un campo ausente o en <see langword="null"/> no cambia.
/// Rigen las reglas del registro más <c>nombreNegocio</c> (hasta 60), <c>descripcion</c> (hasta 300) y <c>anios</c> (0 a 60). <c>fotoUrl</c> solo se acepta
/// de un host de subida del servidor (<c>Tecnicos:HostsFoto</c>).
/// </summary>
internal sealed record UpdateTechnicianBody(
    string? Nombre,
    string? NombreNegocio,
    int? Anios,
    IReadOnlyList<string>? Oficios,
    string? OtroOficio,
    string? Atencion,
    string? DireccionTaller,
    int? RadioKm,
    string? Descripcion,
    string? FotoUrl)
{
    public const int MaxBodyBytes = 8 * 1024;
}

/// <summary>Horario de un día (spec 046, R-47): <c>dia</c> de <c>lunes</c> a <c>domingo</c>; abierto con una o dos franjas <c>HH:mm</c>, o cerrado sin franjas.</summary>
internal sealed record DayScheduleDto(string? Dia, bool? Abierto, IReadOnlyList<TimeRangeDto>? Franjas);

/// <summary>Franja de 24 horas en la hora de la ciudad, con inicio anterior al fin.</summary>
internal sealed record TimeRangeDto(string? Inicio, string? Fin);

/// <summary>Cuerpo de <c>PUT /tecnicos/me/horarios</c> y respuesta de <c>GET</c> (TEC-10 y TEC-11): los siete días, cada uno una vez.</summary>
internal sealed record ScheduleBody(IReadOnlyList<DayScheduleDto>? Horarios)
{
    public const int MaxBodyBytes = 4 * 1024;
}
