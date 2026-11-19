using Knock.Identity.Api.Modules.Safety.Data;
using Knock.Identity.Api.Modules.Safety.Services;

namespace Knock.Identity.Api.Modules.Safety;

/// <summary>Módulo de seguridad entre personas (spec 043): bloqueos y, después, reportes con apelación.</summary>
public static class SafetyModuleExtensions
{
    public static IServiceCollection AddSafetyModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<BloqueosStore>();
        services.AddSingleton<BlockService>();
        return services;
    }
}
