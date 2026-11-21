using Knock.Identity.Api.Modules.Trust.Data;
using Knock.Identity.Api.Modules.Trust.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Knock.Identity.Api.Modules.Trust;

/// <summary>Registro del módulo Trust (spec 025: verificación de identidad). Solo esta clase es pública; el resto del módulo es internal.</summary>
public static class TrustModuleExtensions
{
    public static IServiceCollection AddTrustModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton(TimeProvider.System);
        services.AddOptions<VerificationOptions>().BindConfiguration(VerificationOptions.SectionName)
            .Validate(o => !string.IsNullOrWhiteSpace(o.AvisoVersion) && !string.IsNullOrWhiteSpace(o.AvisoTexto), "Verificacion:AvisoVersion and AvisoTexto are required.")
            .ValidateOnStart();
        services.AddSingleton<VerificationStore>();
        services.AddSingleton<IVerificationStore>(provider => provider.GetRequiredService<VerificationStore>());
        services.AddScoped<VerificationService>();
        return services;
    }
}
