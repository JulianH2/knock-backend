using Knock.Api.Modules.Messaging.Services;

namespace Knock.Api.Modules.Messaging;

/// <summary>Registro del módulo Messaging. Solo esta clase y <see cref="IMessagingApi"/> son públicas; el resto del módulo es internal.</summary>
public static class MessagingModuleExtensions
{
    public static IServiceCollection AddMessagingModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IMessagingApi, MessagingApi>();
        return services;
    }
}
