namespace Knock.Contracts.Conversations;

/// <summary>Qué respondió Mercado sobre el permiso de conversación (spec 030, R-02 y R-09). <see cref="NoDisponible"/> es 0 para que un valor sin asignar falle cerrado.</summary>
public enum EstadoPermisoConversacion
{
    /// <summary>Mercado no respondió a tiempo, contestó algo que no se entiende y no hay permiso vigente en caché: nunca se asume que la conversación está abierta.</summary>
    NoDisponible = 0,

    /// <summary>Quien pide es parte del par y se conoce el permiso.</summary>
    Concedido = 1,

    /// <summary>La solicitud o el par no existen o no son de quien pide; no se distingue una cosa de la otra (404 uniforme).</summary>
    NoEncontrado = 2,
}

/// <summary>Resultado de consultar el permiso. <see cref="Permiso"/> solo viene con <see cref="EstadoPermisoConversacion.Concedido"/>.</summary>
public sealed record ResultadoPermisoConversacion(EstadoPermisoConversacion Estado, PermisoConversacion? Permiso = null);

/// <summary>
/// Pregunta a Mercado quién puede conversar en el par (solicitud, técnico) y si la conversación está abierta, reenviando la sesión de quien pide (spec 030, R-09). Se registra como
/// <c>scoped</c> y nunca se captura en un singleton.
/// </summary>
public interface IPermisoDeConversacion
{
    /// <summary>
    /// Consulta el permiso de <paramref name="subject"/> (el <c>sub</c> de la sesión) para la solicitud <paramref name="solicitudId"/>. <paramref name="tecnicoId"/> es obligatorio para el
    /// cliente y se omite para el técnico. Con <paramref name="conversacionId"/> el permiso concedido se guarda 60 segundos y, mientras dure, también responde cuando Mercado no
    /// contesta; sin él (la conversación aún no existe) siempre se pregunta a Mercado.
    /// </summary>
    Task<ResultadoPermisoConversacion> ConsultarAsync(
        string solicitudId,
        string? tecnicoId,
        string? conversacionId,
        string subject,
        string bearerToken,
        CancellationToken cancellationToken);
}
