using Knock.Identity.Api.Modules.Panel.Authorization;
using Knock.Identity.Api.Modules.Panel.Data;
using Knock.Identity.Api.Modules.Panel.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Knock.Identity.Api.Modules.Panel;

/// <summary>Módulo Panel (spec 033): sesión del panel interno, permisos y auditoría. Solo esta clase es pública; el resto del módulo es internal.</summary>
public static class PanelModuleExtensions
{
    public static IServiceCollection AddPanelModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<PanelCuentasStore>();
        services.AddSingleton<IPanelCuentasStore>(provider => provider.GetRequiredService<PanelCuentasStore>());
        services.AddSingleton<PanelSesionesStore>();
        services.AddSingleton<IPanelSesionesStore>(provider => provider.GetRequiredService<PanelSesionesStore>());
        services.AddSingleton<PanelActivacionesStore>();
        services.AddSingleton<IPanelActivacionesStore>(provider => provider.GetRequiredService<PanelActivacionesStore>());
        services.AddSingleton<PanelAuditoriaStore>();
        services.AddSingleton<IPanelAuditoriaStore>(provider => provider.GetRequiredService<PanelAuditoriaStore>());
        services.AddSingleton<IPanelDesafios, RedisPanelDesafios>();
        services.AddSingleton<IPanelCuentaCache, RedisPanelCuentaCache>();
        services.AddHttpContextAccessor();
        services.AddSingleton<PanelIpHasher>();
        services.AddSingleton<AuditoriaPanel>();
        services.AddSingleton(provider => PanelSettings.Create(provider.GetRequiredService<IConfiguration>(), provider.GetRequiredService<IHostEnvironment>()));
        services.AddOptions<PanelTotpOptions>().BindConfiguration(PanelTotpOptions.SectionName);
        services.AddSingleton(CreateTotpKeys);
        services.AddSingleton<TotpService>();
        services.AddSingleton<PanelAdminService>();
        services.AddSingleton<PanelTokenIssuer>();
        services.AddSingleton<SesionPanelService>();
        services.AddSingleton<AccesoPanelService>();
        services.AddSingleton<ActivacionPanelService>();
        services.AddSingleton<SesionPanelCicloService>();
        services.AddAuthentication().AddJwtBearer(PanelBearer.Scheme, _ => { });
        services.AddOptions<JwtBearerOptions>(PanelBearer.Scheme).Configure<PanelSettings>(PanelBearer.Configure);
        return services;
    }

    // Se validan al crear el servicio y no al arrancar: una API desplegada sin la llave nueva sigue arrancando y solo falla el panel. El mensaje nombra
    // la clave de configuración y jamás su valor.
    private static PanelTotpKeys CreateTotpKeys(IServiceProvider provider)
    {
        var options = provider.GetRequiredService<IOptions<PanelTotpOptions>>().Value;
        return PanelTotpKeys.TryCreate(options, out var errors)
            ?? throw new OptionsValidationException(Options.DefaultName, typeof(PanelTotpOptions), errors);
    }
}
