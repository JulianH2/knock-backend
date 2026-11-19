using System.Diagnostics.Metrics;
using System.Text;
using System.Text.Json.Serialization;
using Knock.Contracts.Visits;
using Knock.Messaging.Api.Modules.Chat.Data;
using Knock.Messaging.Api.Modules.Chat.Options;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using MongoDB.Bson;

namespace Knock.Messaging.Api.Modules.Chat.Services;

/// <summary>
/// <c>codigo</c> de las invocaciones del hub (spec 026, §5): en inglés y en <c>snake_case</c>, sin datos de la persona ni de la visita. Se declaran como constantes <c>...Code</c> para que
/// la prueba de convención de errores (spec 002, CA-07) los vea.
/// </summary>
internal static class ChatErrorCodes
{
    public const string UnauthorizedCode = "unauthorized";
    public const string ConversationUnavailableCode = "conversation_unavailable";
    public const string UnavailableCode = "unavailable";
    public const string NotJoinedCode = "not_joined";
    public const string InvalidMessageCode = "invalid_message";
    public const string MessageLimitCode = "message_limit";
}

/// <summary>Respuesta de una invocación del hub: <c>{ ok: true, … }</c> o <c>{ ok: false, codigo }</c>; nunca una excepción con detalle interno (R-20).</summary>
internal sealed record ChatResult(
    bool Ok,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Codigo = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? MensajeId = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTimeOffset? EnviadoEn = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? ReintentarEnSegundos = null)
{
    public static ChatResult Success { get; } = new(true);

    public static ChatResult Fail(string code) => new(false, code);
}

/// <summary>Cuerpo del evento <c>mensaje</c> (R-12): sin el <c>sub</c>, el nombre ni el rol de quien escribe.</summary>
internal sealed record ChatMessage(string VisitaId, string MensajeId, string Texto, DateTimeOffset EnviadoEn);

/// <summary>Estado de una conexión mientras está abierta: las salas a las que se unió y su token de sesión, solo en memoria (R-09). Se pierde al cerrarla.</summary>
internal sealed class ChatConnectionState(string subject, string bearerToken)
{
    public string Subject { get; } = subject;

    /// <summary>Token de quien se conectó, para reenviarlo a Mercado mientras dura la conexión. Nunca se guarda ni se registra.</summary>
    public string BearerToken { get; } = bearerToken;

    public HashSet<string> Rooms { get; } = new(StringComparer.Ordinal);
}

/// <summary>Medidores de los conteos de R-19 (conexiones, mensajes, uniones rechazadas, límites y errores del puente). Nunca llevan el texto, el token, el <c>sub</c>, el <c>sid</c> ni la visita.</summary>
internal sealed class ChatMetrics : IDisposable
{
    public const string MeterName = "Knock.Chat";

    private readonly Meter _meter = new(MeterName);

    public ChatMetrics()
    {
        ConnectionsOpened = _meter.CreateCounter<long>("knock.chat.connections.opened");
        ConnectionsClosed = _meter.CreateCounter<long>("knock.chat.connections.closed");
        MessagesDelivered = _meter.CreateCounter<long>("knock.chat.messages.delivered");
        JoinsRejected = _meter.CreateCounter<long>("knock.chat.joins.rejected");
        LimitsReached = _meter.CreateCounter<long>("knock.chat.limits.reached");
        BridgeErrors = _meter.CreateCounter<long>("knock.chat.bridge.errors");
    }

    public Counter<long> ConnectionsOpened { get; }

    public Counter<long> ConnectionsClosed { get; }

    public Counter<long> MessagesDelivered { get; }

    public Counter<long> JoinsRejected { get; }

    public Counter<long> LimitsReached { get; }

    public Counter<long> BridgeErrors { get; }

    public void Dispose() => _meter.Dispose();
}

/// <summary>
/// Unirse y salir de la sala de una visita (spec 026, R-05 a R-07 y R-10): comprueba el formato del identificador, la marca de visita cerrada y la pertenencia contra Mercado
/// (<see cref="IVisitaCatalog"/>, con la sesión reenviada). Nunca asume pertenencia cuando no se pudo comprobar y no distingue «no existe» de «es ajena».
/// </summary>
internal sealed class UnionService(IVisitaCatalog catalog, ChatRedisStore redis, ChatMetrics metrics)
{
    private static readonly string[] _openStates = ["confirmada", "en_curso"];

    public async Task<ChatResult> JoinAsync(ChatConnectionState connection, string? visitId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (!IsValidId(visitId))
        {
            return Reject(ChatErrorCodes.UnauthorizedCode);
        }

        var closed = await redis.IsClosedAsync(visitId!);
        if (closed is null)
        {
            metrics.BridgeErrors.Add(1);
            return Reject(ChatErrorCodes.UnavailableCode);
        }

        if (closed.Value)
        {
            connection.Rooms.Remove(visitId!);
            return Reject(ChatErrorCodes.ConversationUnavailableCode);
        }

        var membership = await catalog.CheckAsync(visitId!, connection.BearerToken, cancellationToken);
        if (membership.Status == VisitMembershipStatus.Unavailable)
        {
            return Reject(ChatErrorCodes.UnavailableCode);
        }

        if (membership.Status == VisitMembershipStatus.NotMember)
        {
            return Reject(ChatErrorCodes.UnauthorizedCode);
        }

        if (!_openStates.Contains(membership.State, StringComparer.Ordinal))
        {
            return Reject(ChatErrorCodes.ConversationUnavailableCode);
        }

        connection.Rooms.Add(visitId!);
        return ChatResult.Success;
    }

    public static ChatResult Leave(ChatConnectionState connection, string? visitId)
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (visitId is not null)
        {
            connection.Rooms.Remove(visitId);
        }

        return ChatResult.Success;
    }

    /// <summary>El identificador de la sala es el <c>ObjectId</c> de la visita (24 hexadecimales); cualquier otra cosa no puede ser una visita.</summary>
    public static bool IsValidId(string? visitId) => visitId is { Length: 24 } && ObjectId.TryParse(visitId, out _);

    private ChatResult Reject(string code)
    {
        metrics.JoinsRejected.Add(1);
        return ChatResult.Fail(code);
    }
}

/// <summary>
/// Enviar un mensaje a la sala de la visita (spec 026, R-10 a R-14): exige haberse unido, consulta la marca de visita cerrada sin llamar a Mercado, valida el texto, cobra el límite
/// compartido en Redis y lo retransmite al grupo con un <c>mensajeId</c> UUID v7. Nada se guarda (R-13): si el destinatario no está conectado, el mensaje no se entrega.
/// </summary>
internal sealed class MensajeService(
    IHubContext<ChatHub> hub,
    ChatRedisStore redis,
    IOptions<ChatOptions> options,
    TimeProvider clock,
    ChatMetrics metrics)
{
    public static string Group(string visitId) => $"visita:{visitId}";

    public async Task<ChatResult> SendAsync(ChatConnectionState connection, string? visitId, string? text, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (visitId is null || !connection.Rooms.Contains(visitId))
        {
            return ChatResult.Fail(ChatErrorCodes.NotJoinedCode);
        }

        var closed = await redis.IsClosedAsync(visitId);
        if (closed is null)
        {
            metrics.BridgeErrors.Add(1);
            return ChatResult.Fail(ChatErrorCodes.UnavailableCode);
        }

        if (closed.Value)
        {
            connection.Rooms.Remove(visitId);
            return ChatResult.Fail(ChatErrorCodes.ConversationUnavailableCode);
        }

        var clean = Clean(text, options.Value.MaxCaracteres);
        if (clean is null)
        {
            return ChatResult.Fail(ChatErrorCodes.InvalidMessageCode);
        }

        var charge = await redis.ChargeAsync(connection.Subject, options.Value.MensajesPorMinuto, ChatOptions.WindowSeconds);
        if (charge.Unavailable)
        {
            metrics.BridgeErrors.Add(1);
            return ChatResult.Fail(ChatErrorCodes.UnavailableCode);
        }

        if (!charge.Allowed)
        {
            metrics.LimitsReached.Add(1);
            return new ChatResult(false, ChatErrorCodes.MessageLimitCode, ReintentarEnSegundos: charge.RetryAfterSeconds);
        }

        var id = Guid.CreateVersion7().ToString("D");
        var sentAt = clock.GetUtcNow();
        await hub.Clients.Group(Group(visitId)).SendAsync("mensaje", new ChatMessage(visitId, id, clean, sentAt), cancellationToken);
        metrics.MessagesDelivered.Add(1);
        return new ChatResult(true, MensajeId: id, EnviadoEn: sentAt);
    }

    /// <summary>
    /// El texto recortado, o <see langword="null"/> si no vale (R-11): de 1 a <paramref name="maxCharacters"/> caracteres, sin caracteres de control salvo el salto de línea, y
    /// sin pasar de 4 KB en la invocación.
    /// </summary>
    public static string? Clean(string? text, int maxCharacters)
    {
        if (text is null || Encoding.UTF8.GetByteCount(text) > ChatOptions.MaxInvocationBytes)
        {
            return null;
        }

        var trimmed = text.Trim();
        return trimmed.Length is >= 1 && trimmed.Length <= maxCharacters && !trimmed.Any(c => char.IsControl(c) && c != '\n') ? trimmed : null;
    }
}
