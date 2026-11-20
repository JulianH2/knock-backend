namespace Knock.Market.Api.Modules.Tecnicos.Dtos;

internal sealed record TrabajoResponse(
    bool DisponibleHoy,
    TrabajoVerificationResponse Verificacion,
    object? Nivel,
    IReadOnlyList<TrabajoVisitResponse> ProximasVisitas,
    IReadOnlyList<TrabajoQuoteResponse> CotizacionesEnviadas,
    TrabajoFinishedPage Terminadas,
    TrabajoSummary Resumen);

internal sealed record TrabajoVerificationResponse(string Estado);
internal sealed record TrabajoClient(string Colonia);
internal sealed record TrabajoPlace(string Tipo, string? Direccion);
internal sealed record TrabajoVisitResponse(
    string VisitaId, string SolicitudId, string Dia, string Hora, string Modalidad,
    TrabajoClient Cliente, TrabajoPlace Lugar, int? Precio, int Tope, string Estado, bool PuedeLlegar);
internal sealed record TrabajoQuoteResponse(
    string CotizacionId, string SolicitudId, string OficioNombre, string TipoPrecio,
    int? Precio, int? PrecioMin, int? PrecioMax, int Tope, string Estado, string? MotivoRechazo, DateTime EnviadaEn);
internal sealed record TrabajoFinishedItem(string SolicitudId, string OficioNombre, DateTime TerminadaEn, int? Precio, object? Calificacion);
internal sealed record TrabajoFinishedPage(IReadOnlyList<TrabajoFinishedItem> Items, string? NextCursor);
internal sealed record TrabajoSummary(long TrabajosTerminados, object? Calificacion);
