using Knock.Api.Modules.Assistant.Services;

namespace Knock.Api.Modules.Assistant;

/// <summary>Registro del módulo Assistant. Solo esta clase y <see cref="IAssistantApi"/> son públicas; el resto del módulo es internal.</summary>
public static class AssistantModuleExtensions
{
    public static IServiceCollection AddAssistantModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IAssistantApi, AssistantApi>();
        return services;
    }
}
