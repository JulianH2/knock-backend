using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Knock.Market.Api.Modules.Requests.Dtos;

/// <summary>Técnico que cotizó, tal como lo ve el cliente al comparar (spec 021). Sin teléfono: el contacto solo llega con la visita.</summary>
internal sealed record QuoteTechnician(string Id, string Nombre, string? FotoUrl, double? Calificacion, bool Verificado, string NivelVerificacion);

/// <summary>
/// Cotización dentro de la comparación (spec 021, §5). <c>Cubre</c> es el texto corto de lo que cubre según su tipo de precio y <c>Etiquetas</c> son marcas
/// calculadas por el servidor, por ejemplo la más barata o la más pronto. <c>Precio</c> es nulo en un rango (solo hay mínimo y máximo) y <c>MotivoRechazo</c> solo aparece en las rechazadas.
/// </summary>
internal sealed record QuoteComparisonItem(
    string Id,
    string Estado,
    string TipoPrecio,
    int? Precio,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? PrecioMin,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? PrecioMax,
    int Tope,
    bool RequiereAprobacion,
    string Dia,
    string Hora,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Nota,
    string Cubre,
    IReadOnlyList<string> Etiquetas,
    QuoteTechnician Tecnico,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? MotivoRechazo);

/// <summary>
/// Respuesta de <c>GET /solicitudes/{id}/cotizaciones</c>: hasta 5 cotizaciones vivas y si el cliente ya puede elegir. <c>TiposMixtos</c> es verdadero cuando
/// hay precios fijos y rangos o visitas juntos (no son comparables uno a uno); <c>Motivo</c> explica por qué <c>PuedeElegir</c> es falso; <c>VisitaId</c>
/// aparece si la solicitud ya tiene cita confirmada.
/// </summary>
internal sealed record QuoteComparison(
    string SolicitudId,
    IReadOnlyList<QuoteComparisonItem> Cotizaciones,
    int TotalCotizaciones,
    bool TiposMixtos,
    bool PuedeElegir,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Motivo,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? VisitaId);

/// <summary>Consulta de la comparación (spec 021): <c>orden</c> es <c>recomendadas</c>, <c>precio</c>, <c>calificacion</c> o <c>pronto</c>.</summary>
internal sealed class QuoteComparisonQuery
{
    [Description("Sort order: recomendadas (default), precio, calificacion or pronto.")]
    public string? Orden { get; set; }
}

/// <summary>Cuerpo de <c>POST /solicitudes/{id}/elegir</c>.</summary>
/// <param name="CotizacionId">Identificador (24 hexadecimales) de la cotización que se elige.</param>
internal sealed record ChooseQuoteBody(string? CotizacionId)
{
    /// <summary>Tamaño máximo del cuerpo (spec 021, §5): 8 KB.</summary>
    public const int MaxBodyBytes = 8192;
}

/// <summary>Técnico elegido, con el contacto público que se revela solo después de elegir.</summary>
internal sealed record ChosenTechnician(string Id, string Nombre, string? FotoUrl, bool Verificado, string NivelVerificacion, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Telefono, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Whatsapp);

/// <summary>Lugar de la cita: tipo (<c>domicilio</c> o <c>taller</c>) y dirección.</summary>
internal sealed record AppointmentPlace(string Tipo, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Direccion);

/// <summary>Cita confirmada al elegir una cotización (spec 021, §5).</summary>
internal sealed record Appointment(
    string VisitaId,
    string SolicitudId,
    string CotizacionId,
    string Estado,
    string Modalidad,
    string Dia,
    string Hora,
    int? Precio,
    int Tope,
    ChosenTechnician Tecnico,
    AppointmentPlace Lugar);
