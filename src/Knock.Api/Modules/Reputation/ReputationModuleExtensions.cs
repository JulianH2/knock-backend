using Knock.Api.Modules.Reputation.Services;

namespace Knock.Api.Modules.Reputation;

/// <summary>Registro del módulo Reputation. Solo esta clase y <see cref="IReputationApi"/> son públicas; el resto del módulo es internal.</summary>
public static class ReputationModuleExtensions
{
    public static IServiceCollection AddReputationModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IReputationApi, ReputationApi>();
        return services;
    }
}
