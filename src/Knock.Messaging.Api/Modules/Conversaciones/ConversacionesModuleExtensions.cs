using Knock.Contracts.Conversations;
using Knock.Messaging.Api.Modules.Chat.Options;
using Knock.Messaging.Api.Modules.Conversaciones.Data;
using Microsoft.Extensions.Options;

namespace Knock.Messaging.Api.Modules.Conversaciones;

/// <summary>Registro del módulo Conversaciones (spec 030). Solo esta clase es pública; el resto del módulo es internal.</summary>
public static class ConversacionesModuleExtensions
{
    /// <summary>
    /// Registra el manejador que traduce <c>DomainException</c> a ProblemDetails con el <c>code</c> de su catálogo y el cliente del permiso de conversación de Mercado con su caché de
    /// 60 segundos. El cliente usa la dirección y el tiempo límite de <c>Services:Market</c>, que valida el módulo Chat (<c>AddChatModule</c>) al arrancar: este registro va después de ese.
    /// </summary>
    public static IServiceCollection AddConversacionesModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddExceptionHandler<DomainExceptionHandler>();
        services.AddSingleton<IPermisoCache, PermisoRedisCache>();
        services
            .AddHttpClient<IPermisoDeConversacion, MarketPermisoDeConversacion>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<MarketServiceOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl!, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(options.TiempoLimiteSegundos);
            })
            .RemoveAllLoggers();
        return services;
    }
}
