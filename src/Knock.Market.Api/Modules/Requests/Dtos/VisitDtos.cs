using System.Text.Json.Serialization;

namespace Knock.Market.Api.Modules.Requests.Dtos;

/// <summary>Técnico de la visita para el cliente: contacto público del técnico elegido (spec 021, R-15).</summary>
internal sealed record VisitTechnician(string Nombre, string? FotoUrl, bool Verificado, string NivelVerificacion, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Telefono, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Whatsapp);

/// <summary>Lo único del cliente que ve el técnico: su colonia. Nunca el nombre ni el teléfono.</summary>
internal sealed record VisitCustomer(string Colonia);

/// <summary>Lugar de la visita: <c>domicilio</c> o <c>taller</c>, con su dirección.</summary>
internal sealed record VisitPlace(string Tipo, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Direccion);

/// <summary>
/// Vista de la visita (spec 022, R-02 y R-05). <c>Rol</c> es <c>cliente</c> o <c>tecnico</c> y decide qué campos vienen: <c>Tecnico</c>, <c>Codigo</c>,
/// <c>CodigoVenceEn</c>, <c>MotivoCodigo</c>, <c>ReglaSeguridad</c> y <c>PuedeAvisar</c> solo en la del cliente; <c>Cliente</c> y <c>LlegadaEn</c> solo en la del técnico.
/// El código nunca llega al técnico: <c>Codigo</c> (cuatro dígitos) solo sale en domicilio, en <c>confirmada</c> o <c>en_curso</c>, sin usar, sin vencer y sin
/// bloqueo; cuando falta, <c>MotivoCodigo</c> dice por qué (<c>usado</c>, <c>vencido</c>, <c>bloqueada</c> o <c>no_aplica</c>).
/// </summary>
internal sealed record VisitResponse(
    string Id,
    string Rol,
    string Estado,
    string Modalidad,
    string Dia,
    string Hora,
    int? Precio,
    int Tope,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] VisitTechnician? Tecnico,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] VisitCustomer? Cliente,
    VisitPlace Lugar,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Codigo,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTimeOffset? CodigoVenceEn,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? MotivoCodigo,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ReglaSeguridad,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? PuedeAvisar,
    bool PuedeLlegar,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTimeOffset? LlegadaEn);

/// <summary>Cuerpo de <c>POST /visitas/{id}/llegada</c>: <c>Codigo</c> son exactamente cuatro dígitos ASCII, con los ceros a la izquierda.</summary>
internal sealed record ArrivalBody(string? Codigo)
{
    /// <summary>Tamaño máximo del cuerpo (spec 022, §5): 8 KB.</summary>
    public const int MaxBodyBytes = 8192;
}

/// <summary>
/// Texto de confianza que prepara <c>POST /visitas/{id}/aviso</c> (no envía nada ni guarda contactos). <c>IncluyeCodigo</c> e <c>IncluyeDireccion</c> son
/// siempre falsos: ni el código ni la dirección viajan en el aviso.
/// </summary>
internal sealed record TrustNotice(string Texto, bool IncluyeCodigo, bool IncluyeDireccion);
