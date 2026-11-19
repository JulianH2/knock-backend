namespace Knock.Identity.Api.Modules.Panel;

/// <summary>Módulo Panel (spec 033): sesión del panel interno, permisos y auditoría. Solo esta clase es pública; el resto del módulo es internal.</summary>
public static class PanelModuleExtensions
{
    public static IServiceCollection AddPanelModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
