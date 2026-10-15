using Knock.Api.Modules.Requests.Services;

namespace Knock.Api.Modules.Requests;

/// <summary>Registro del módulo Requests. Solo esta clase y <see cref="IRequestsApi"/> son públicas; el resto del módulo es internal.</summary>
public static class RequestsModuleExtensions
{
    public static IServiceCollection AddRequestsModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IRequestsApi, RequestsApi>();
        return services;
    }
}
