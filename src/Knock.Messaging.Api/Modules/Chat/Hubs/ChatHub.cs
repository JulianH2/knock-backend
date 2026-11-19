using Knock.Messaging.Api.Modules.Chat.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Knock.Messaging.Api.Modules.Chat;

/// <summary>
/// El canal de chat de visita (<c>/hubs/chat</c>, spec 026). Solo traduce invocaciones a llamadas de servicio y responde con objetos <c>{ ok, codigo }</c>; no valida reglas ni habla con
/// Redis ni con Mercado. Al conectarse la persona no entra a ninguna sala: entra con <see cref="UnirseAVisita"/> y sale con <see cref="SalirDeVisita"/>. El token de la conexión se
/// conserva solo en la memoria de esa conexión y se descarta al cerrarla (R-09).
/// </summary>
[Authorize]
internal sealed class ChatHub(UnionService union, MensajeService messages, ChatMetrics metrics, ILogger<ChatHub> logger) : Hub
{
    private const string _stateKey = "knock.chat.state";

    private static readonly Action<ILogger, Exception?> _opened = LoggerMessage.Define(LogLevel.Information, new EventId(1, "ChatOpened"), "Chat connection opened");
    private static readonly Action<ILogger, Exception?> _closed = LoggerMessage.Define(LogLevel.Information, new EventId(2, "ChatClosed"), "Chat connection closed");

    public override Task OnConnectedAsync()
    {
        var http = Context.GetHttpContext();
        var token = http?.Request.Headers.Authorization.ToString() is { Length: > 7 } header && header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? header[7..]
            : http?.Request.Query["access_token"].ToString() ?? string.Empty;
        Context.Items[_stateKey] = new ChatConnectionState(Context.User?.FindFirst("sub")?.Value ?? string.Empty, token);
        metrics.ConnectionsOpened.Add(1);
        _opened(logger, null);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        Context.Items.Remove(_stateKey);
        metrics.ConnectionsClosed.Add(1);
        _closed(logger, null);
        return base.OnDisconnectedAsync(exception);
    }

    /// <summary>Entra a la sala de la visita si quien pide es el cliente dueño o el técnico elegido y la visita está <c>confirmada</c> o <c>en_curso</c>.</summary>
    public async Task<ChatResult> UnirseAVisita(string visitaId)
    {
        var state = State();
        var result = await union.JoinAsync(state, visitaId, Context.ConnectionAborted);
        if (result.Ok)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, MensajeService.Group(visitaId), Context.ConnectionAborted);
        }
        else if (UnionService.IsValidId(visitaId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, MensajeService.Group(visitaId), Context.ConnectionAborted);
        }

        return result;
    }

    public async Task<ChatResult> SalirDeVisita(string visitaId)
    {
        var result = UnionService.Leave(State(), visitaId);
        if (UnionService.IsValidId(visitaId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, MensajeService.Group(visitaId), Context.ConnectionAborted);
        }

        return result;
    }

    /// <summary>Retransmite el mensaje al grupo de la visita (incluida esta conexión y otros dispositivos de la misma persona). Nada se guarda.</summary>
    public async Task<ChatResult> EnviarMensaje(string visitaId, string texto)
    {
        var state = State();
        var result = await messages.SendAsync(state, visitaId, texto, Context.ConnectionAborted);
        if (result.Codigo == ChatErrorCodes.ConversationUnavailableCode && UnionService.IsValidId(visitaId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, MensajeService.Group(visitaId), Context.ConnectionAborted);
        }

        return result;
    }

    private ChatConnectionState State() => (ChatConnectionState)Context.Items[_stateKey]!;
}
