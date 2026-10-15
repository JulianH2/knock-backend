using Knock.Api.Modules.Trust.Services;

namespace Knock.Api.Modules.Trust;

/// <summary>Registro del módulo Trust. Solo esta clase y <see cref="ITrustApi"/> son públicas; el resto del módulo es internal.</summary>
public static class TrustModuleExtensions
{
    public static IServiceCollection AddTrustModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<ITrustApi, TrustApi>();
        return services;
    }
}
