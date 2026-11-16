namespace Knock.Market.Api.Modules.Requests.Dtos;

/// <summary>Oficio de la solicitud: clave del catálogo y nombre.</summary>
internal sealed record RequestTrade(string Clave, string Nombre);

/// <summary>Colonia de la solicitud: identificador, nombre y código postal.</summary>
internal sealed record RequestNeighborhood(string Id, string Nombre, string Cp);

/// <summary>Foto privada de la solicitud: el enlace de lectura vence a los 5 minutos y la app lo vuelve a pedir con el detalle.</summary>
internal sealed record RequestPhoto(string Id, string Url, DateTimeOffset VenceEn);

/// <summary>
/// Solicitud (spec 020, §5). <c>Direccion</c> y <c>TotalCotizaciones</c> solo salen en la vista del cliente dueño; la vista del técnico no trae
/// nunca la dirección, el nombre ni el teléfono del cliente.
/// </summary>
internal sealed record RequestResponse(
    string Id,
    string Estado,
    string Modalidad,
    RequestTrade Oficio,
    RequestNeighborhood Colonia,
    string Descripcion,
    IReadOnlyList<RequestPhoto> Fotos,
    string Dia,
    string Franja,
    string? Direccion,
    int? TotalCotizaciones,
    DateTimeOffset VenceEn,
    DateTimeOffset CreadoEn);

/// <summary>Fila de <c>GET /solicitudes</c>: sin descripción, dirección ni enlaces de fotos.</summary>
internal sealed record RequestSummary(
    string Id,
    string Estado,
    string OficioNombre,
    string ColoniaNombre,
    string Dia,
    string Franja,
    int TotalFotos,
    int TotalCotizaciones,
    DateTimeOffset VenceEn,
    DateTimeOffset CreadoEn);

/// <summary>Página de solicitudes propias; <c>NextCursor</c> es <see langword="null"/> en la última página.</summary>
internal sealed record RequestPage(IReadOnlyList<RequestSummary> Items, string? NextCursor);
