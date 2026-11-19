using Knock.Messaging.Api.Modules.Chat.Data;
using Knock.Messaging.Api.Modules.Chat.Options;
using Knock.Messaging.Api.Modules.Chat.Services;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.Options;

namespace Knock.Messaging.Api.Modules.Chat;

/// <summary>Registro del módulo Chat. Solo esta clase es pública; el hub, los servicios y el almacén son internal.</summary>
public static class ChatModuleExtensions
{
    /// <summary>Ruta del hub (spec 026, R-01). El gateway la enruta con la política de sesión.</summary>
    public const string HubPath = "/hubs/chat";

    public static IServiceCollection AddChatModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddOptions<ChatOptions>().BindConfiguration(ChatOptions.SectionName).ValidateOnStart();
        services.AddSingleton<IValidateOptions<ChatOptions>, ChatOptionsValidator>();
        services.AddOptions<MarketServiceOptions>().BindConfiguration(MarketServiceOptions.SectionName).ValidateOnStart();
        services.AddSingleton<IValidateOptions<MarketServiceOptions>, MarketServiceOptionsValidator>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ChatMetrics>();
        services.AddSingleton<ChatRedisStore>();
        services.AddMarketVisitaCatalog();
        services.AddScoped<UnionService>();
        services.AddScoped<MensajeService>();
        return services;
    }

    /// <summary>
    /// Mapea el hub: WebSocket primero y eventos del servidor como respaldo (el sondeo largo queda deshabilitado: no hay estado de sesión en el proceso y multiplica las peticiones, R-18) y cierra
    /// la conexión cuando vence el token (R-04).
    /// </summary>
    public static IEndpointRouteBuilder MapChatHub(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapHub<ChatHub>(HubPath, options =>
        {
            options.Transports = HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents;
            options.CloseOnAuthenticationExpiration = true;
        });
        return endpoints;
    }
}
