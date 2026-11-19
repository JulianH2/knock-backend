extern alias MessagingApi;

using System.Net;
using Knock.IntegrationTest.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Knock.IntegrationTest.Chat;

/// <summary>Lo que el Mercado de mentira contesta sobre una visita: el estado HTTP y, con 200, el rol de quien pregunta y el estado de la visita.</summary>
internal sealed record VisitAnswer(int Status, string? Role = null, string? State = null);

/// <summary>
/// Mercado de mentira para las pruebas del chat (spec 026): contesta <c>GET /api/v1/visitas/{id}</c> con lo que dice <see cref="Answer"/> para el <c>sub</c> del token recibido.
/// Solo datos ficticios; guarda cuántas veces lo llamaron.
/// </summary>
internal sealed class FakeMarket : IAsyncDisposable
{
    private readonly WebApplication _app;
    private int _calls;

    private FakeMarket(WebApplication app, string baseUrl)
    {
        _app = app;
        BaseUrl = baseUrl;
    }

    public string BaseUrl { get; }

    public int Calls => Volatile.Read(ref _calls);

    /// <summary>Respuesta por (sub, visitId). Por omisión, la visita no existe (404).</summary>
    public Func<string, string, Task<VisitAnswer>> Answer { get; set; } = (_, _) => Task.FromResult(new VisitAnswer(StatusCodes.Status404NotFound));

    /// <summary>Todos los que pidan la visita son participantes de ella con ese rol y estado.</summary>
    public void EveryoneIs(string role, string state = "confirmada") => Answer = (_, _) => Task.FromResult(new VisitAnswer(StatusCodes.Status200OK, role, state));

    public static async Task<FakeMarket> StartAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        FakeMarket? market = null;
        app.MapGet("/api/v1/visitas/{id}", async (string id, HttpContext context) =>
        {
            Interlocked.Increment(ref market!._calls);
            var token = context.Request.Headers.Authorization.ToString()["Bearer ".Length..];
            var sub = new JsonWebTokenHandler().ReadJsonWebToken(token).GetClaim("sub").Value;
            var answer = await market.Answer(sub, id);
            return answer.Status == StatusCodes.Status200OK
                ? Results.Json(new { rol = answer.Role, estado = answer.State })
                : Results.StatusCode(answer.Status);
        });
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        market = new FakeMarket(app, address.TrimEnd('/') + "/");
        return market;
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}

/// <summary>
/// Una copia de <c>Knock.Messaging.Api</c> para las pruebas del chat: Redis real con contraseña, Mercado de mentira y los límites del chat por configuración. MongoDB no se usa
/// (cadena sintética). Dos de estas sobre el mismo Redis son «dos copias del servicio» (spec 026, R-15). Solo datos ficticios.
/// </summary>
internal sealed class ChatFactory(IReadOnlyDictionary<string, string?> settings) : WebApplicationFactory<MessagingApi::Program>
{
    public const string Visit = "507f1f77bcf86cd799439011";

    public static async Task<ChatFactory> CreateAsync(FakeMarket market, IReadOnlyDictionary<string, string?>? overrides = null, string? redisConnectionString = null)
    {
        ArgumentNullException.ThrowIfNull(market);
        var redis = await RedisTestFixture.SharedAsync();
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:KnockRedis"] = redisConnectionString ?? redis.ConnectionString,
            ["ConnectionStrings:KnockMongo"] = "mongodb://127.0.0.1:27099/?serverSelectionTimeoutMS=500",
            ["Jwt:Issuer"] = GatewayTestSupport.Issuer,
            ["Jwt:Audience"] = GatewayTestSupport.Audience,
            ["Jwt:SigningKey"] = GatewayTestSupport.SigningKeyBase64,
            ["RateLimiting:Global:PermitLimit"] = "1000000",
            ["RateLimiting:Ready:PermitLimit"] = "1000000",
            ["Chat:Habilitado"] = "true",
            ["Services:Market:BaseUrl"] = market.BaseUrl,
        };
        foreach (var (key, value) in overrides ?? new Dictionary<string, string?>())
        {
            values[key] = value;
        }

        return new ChatFactory(values);
    }

    /// <summary>Un cliente de SignalR (eventos del servidor sobre el servidor de pruebas en memoria) contra esta copia, con el token en la consulta (<c>access_token</c>), como lo hace la app.</summary>
    public HubConnection Connect(string token)
    {
        var server = Server;
        return new HubConnectionBuilder()
            .WithUrl(new Uri(server.BaseAddress, "hubs/chat"), options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                options.Transports = HttpTransportType.ServerSentEvents;
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
            })
            .Build();
    }

    /// <summary>El saludo del hub (<c>POST /hubs/chat/negotiate</c>) con el token en la consulta, para ver el código HTTP.</summary>
    public async Task<HttpStatusCode> NegotiateAsync(string? token)
    {
        using var client = CreateClient();
        var path = token is null ? "/hubs/chat/negotiate?negotiateVersion=1" : $"/hubs/chat/negotiate?negotiateVersion=1&access_token={Uri.EscapeDataString(token)}";
        using var response = await client.PostAsync(new Uri(path, UriKind.Relative), content: null);
        return response.StatusCode;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseEnvironment("Production");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));
    }
}
