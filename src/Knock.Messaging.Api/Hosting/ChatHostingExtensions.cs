using Knock.Messaging.Api.Modules.Chat;
using Knock.Messaging.Api.Modules.Chat.Options;
using Microsoft.AspNetCore.SignalR.StackExchangeRedis;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Knock.Messaging.Api.Hosting;

/// <summary>
/// Lo del chat que toca Redis o el pipeline HTTP del servicio (spec 026): el puente de SignalR entre copias sobre la misma conexión de Redis del servicio, el límite de 8 KB del hub y la puerta
/// del saludo que falla cerrado si el chat está apagado o Redis no responde. Vive en <c>Hosting</c> porque el módulo no debe depender de <c>StackExchange.Redis</c>.
/// </summary>
internal static class ChatHostingExtensions
{
    /// <summary>Prefijo de canal del puente (constante, no configurable por entorno; R-15).</summary>
    public const string BridgeChannelPrefix = "knock:chat";

    /// <summary>
    /// SignalR con el puente de Redis (<c>knock:chat</c>) sobre el <see cref="IConnectionMultiplexer"/> del servicio, mensajes entrantes de hasta 8 KB y sin guardar nada. Requiere que
    /// <c>AddKnockRedis()</c> ya se haya llamado (lo hace <c>AddKnockAuthentication</c>).
    /// </summary>
    public static IServiceCollection AddKnockChatBridge(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSignalR(options => options.MaximumReceiveMessageSize = ChatOptions.MaxReceiveBytes).AddStackExchangeRedis();
        services.AddOptions<RedisOptions>().Configure<IServiceProvider>((options, provider) =>
        {
            options.Configuration.ChannelPrefix = RedisChannel.Literal(BridgeChannelPrefix);
            options.ConnectionFactory = _ => Task.FromResult(provider.GetRequiredService<IConnectionMultiplexer>());
        });
        return services;
    }

    /// <summary>
    /// Puerta del saludo y de la conexión del hub (R-16 y R-17): con el chat apagado responde 503 <c>chat_disabled</c> y con Redis caído, 503, antes de aceptar una conexión sin puente ni límite.
    /// El resto de las rutas no pasan por aquí.
    /// </summary>
    public static IApplicationBuilder UseKnockChatGate(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseWhen(
            context => context.Request.Path.StartsWithSegments(ChatModuleExtensions.HubPath, StringComparison.OrdinalIgnoreCase),
            branch => branch.Use(async (context, next) =>
            {
                if (context.RequestServices.GetRequiredService<IOptions<ChatOptions>>().Value.Habilitado is not true)
                {
                    await ProblemResponses.WriteAsync(context, StatusCodes.Status503ServiceUnavailable, "Service Unavailable", "chat_disabled");
                    return;
                }

                if (!await RedisAnswersAsync(context.RequestServices.GetRequiredService<IConnectionMultiplexer>()))
                {
                    context.Response.Headers.RetryAfter = "5";
                    await ProblemResponses.WriteAsync(context, StatusCodes.Status503ServiceUnavailable, "Service Unavailable", "service_unavailable");
                    return;
                }

                await next(context);
            }));
    }

    private static async Task<bool> RedisAnswersAsync(IConnectionMultiplexer redis)
    {
        if (!redis.IsConnected)
        {
            return false;
        }

        try
        {
            await redis.GetDatabase().PingAsync();
            return true;
        }
        catch (Exception exception) when (exception is RedisException or TimeoutException)
        {
            return false;
        }
    }
}
