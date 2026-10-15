using Knock.Api.Modules.Notifications.Services;

namespace Knock.Api.Modules.Notifications;

/// <summary>Registro del módulo Notifications. Solo esta clase y <see cref="INotificationsApi"/> son públicas; el resto del módulo es internal.</summary>
public static class NotificationsModuleExtensions
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<INotificationsApi, NotificationsApi>();
        return services;
    }
}
