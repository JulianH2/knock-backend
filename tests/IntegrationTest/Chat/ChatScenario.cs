using System.Text.Json;
using System.Threading.Channels;
using Knock.IntegrationTest.Support;
using Microsoft.AspNetCore.SignalR.Client;

namespace Knock.IntegrationTest.Chat;

/// <summary>
/// Una visita ficticia con Mercado de mentira y una o dos copias del servicio de Mensajería sobre el mismo Redis. Cierra las conexiones y las copias al terminar. El cliente de la visita
/// es <see cref="Client"/> y el técnico elegido, <see cref="Technician"/>; cualquier otro <c>sub</c> es ajeno.
/// </summary>
internal sealed class ChatScenario : IAsyncDisposable
{
    private static readonly TimeSpan _wait = TimeSpan.FromSeconds(10);

    private readonly List<HubConnection> _connections = [];
    private readonly List<ChatFactory> _copies = [];

    public string Client { get; } = Guid.NewGuid().ToString("D");

    public string Technician { get; } = Guid.NewGuid().ToString("D");

    public string State { get; set; } = "confirmada";

    public FakeMarket Market { get; private set; } = null!;

    public static async Task<ChatScenario> StartAsync()
    {
        var scenario = new ChatScenario();
        scenario.Market = await FakeMarket.StartAsync();
        scenario.Market.Answer = (sub, _) => Task.FromResult(scenario.AnswerFor(sub));
        return scenario;
    }

    /// <summary>Levanta una copia del servicio (la primera es «la copia A»; una segunda sobre el mismo Redis es «la copia B»).</summary>
    public async Task<ChatFactory> CopyAsync(IReadOnlyDictionary<string, string?>? overrides = null, string? redisConnectionString = null)
    {
        var copy = await ChatFactory.CreateAsync(Market, overrides, redisConnectionString);
        _copies.Add(copy);
        return copy;
    }

    public static string Token(string sub, string? sid = null, TimeSpan? lifetime = null) => GatewayTestSupport.IssueToken(sub: sub, sid: sid, lifetime: lifetime);

    /// <summary>Abre una conexión de <paramref name="sub"/> a la copia dada y la deja iniciada (sin unirse a ninguna visita).</summary>
    public async Task<HubConnection> ConnectAsync(ChatFactory copy, string sub, string? sid = null, TimeSpan? lifetime = null)
    {
        ArgumentNullException.ThrowIfNull(copy);
        var connection = copy.Connect(Token(sub, sid, lifetime));
        _connections.Add(connection);
        await connection.StartAsync();
        return connection;
    }

    public static async Task<JsonElement> JoinAsync(HubConnection connection, string visit = ChatFactory.Visit) =>
        await InvokeAsync(connection, "UnirseAVisita", visit);

    public static async Task<JsonElement> SendAsync(HubConnection connection, string text, string visit = ChatFactory.Visit) =>
        await InvokeAsync(connection, "EnviarMensaje", visit, text);

    /// <summary>Todos los eventos <c>mensaje</c> que reciba la conexión, en orden.</summary>
    public static ChannelReader<JsonElement> Listen(HubConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        var channel = Channel.CreateUnbounded<JsonElement>();
        connection.On<JsonElement>("mensaje", message => channel.Writer.TryWrite(message));
        return channel.Reader;
    }

    public static async Task<JsonElement> NextAsync(ChannelReader<JsonElement> inbox)
    {
        ArgumentNullException.ThrowIfNull(inbox);
        using var timeout = new CancellationTokenSource(_wait);
        return await inbox.ReadAsync(timeout.Token);
    }

    /// <summary>Verdadero si no llega ningún mensaje en el tiempo dado.</summary>
    public static async Task<bool> NothingArrivesAsync(ChannelReader<JsonElement> inbox, TimeSpan time)
    {
        ArgumentNullException.ThrowIfNull(inbox);
        using var timeout = new CancellationTokenSource(time);
        try
        {
            await inbox.ReadAsync(timeout.Token);
            return false;
        }
        catch (OperationCanceledException)
        {
            return true;
        }
    }

    public static string Text(JsonElement result, string name) => result.TryGetProperty(name, out var value) ? value.GetString() ?? string.Empty : string.Empty;

    public static bool Ok(JsonElement result) => result.GetProperty("ok").GetBoolean();

    public async ValueTask DisposeAsync()
    {
        foreach (var connection in _connections)
        {
            await connection.DisposeAsync();
        }

        foreach (var copy in _copies)
        {
            await copy.DisposeAsync();
        }

        await Market.DisposeAsync();
    }

    private VisitAnswer AnswerFor(string sub)
    {
        if (sub == Client)
        {
            return new VisitAnswer(200, "cliente", State);
        }

        return sub == Technician ? new VisitAnswer(200, "tecnico", State) : new VisitAnswer(404);
    }

    private static Task<JsonElement> InvokeAsync(HubConnection connection, string method, params object[] arguments) =>
        connection.InvokeCoreAsync<JsonElement>(method, arguments);
}
