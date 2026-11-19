using Knock.Contracts.Moderation;
using Knock.Identity.Api.Modules.Safety.Data;
using Knock.Identity.Api.Modules.Safety.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        services.AddSingleton<ReportesStore>();
        // Hasta que exista la moderación de la spec 031 en Identidad, un reporte no se guarda sin revisión (falla cerrado). Las pruebas la sustituyen.
        services.TryAddSingleton<IContentModerator, UnavailableContentModerator>();
        services.AddSingleton<ReportService>();
        return services;
    }
}
