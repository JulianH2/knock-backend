using System.Text.Json.Serialization;

namespace Knock.Contracts.Conversations;

/// <summary>Datos públicos del técnico de una conversación: solo nombre y foto, nunca contacto (spec 030, R-06).</summary>
public sealed record TecnicoPublicoConversacion(string Nombre, [property: JsonPropertyName("fotoUrl"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Foto);

/// <summary>
/// Lo que Mercado dice a Mensajería sobre quién puede conversar en el par (solicitud, técnico) y si la conversación está abierta (spec 030, R-02 y R-03). La ruta que lo
/// devuelve no se publica en el gateway: solo la llama Mensajería por la red interna con la sesión de quien pide. <see cref="Rol"/> es <c>cliente</c> o <c>tecnico</c>.
/// </summary>
public sealed record PermisoConversacion(
    string SolicitudId,
    string TecnicoId,
    string ClienteId,
    string TecnicoUsuarioId,
    string Rol,
    bool Abierta,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? MotivoCierre,
    bool Elegido,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? VisitaId,
    string Servicio,
    string ColoniaNombre,
    TecnicoPublicoConversacion TecnicoPublico);
