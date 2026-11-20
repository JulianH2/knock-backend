using Knock.Identity.Api.Modules.Sesiones.Data;
using Knock.Identity.Api.Modules.Sesiones.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Knock.Identity.Api.Modules.Sesiones;

/// <summary>Registro del módulo Sesiones (specs 015 a 018). Solo esta clase es pública; el resto del módulo es internal.</summary>
public static class SesionesModuleExtensions
{
    public static IServiceCollection AddSesionesModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton(TimeProvider.System);
        services.AddSesionesData();
        services.AddOptions<PhoneProtectionOptions>().BindConfiguration(PhoneProtectionOptions.SectionName);
        services.AddSingleton<IValidateOptions<PhoneProtectionOptions>, PhoneProtectionOptionsValidator>();
        services.AddSingleton(CreatePhoneProtectionKeys);
        services.AddSingleton<PhoneProtector>();
        services.AddOptions<ConsentVersionsOptions>()
            .BindConfiguration(ConsentVersionsOptions.SectionName)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.AvisoPrivacidad) && !string.IsNullOrWhiteSpace(options.Terminos),
                "Consentimientos:AvisoPrivacidad and Consentimientos:Terminos are required.")
            .ValidateOnStart();
        services.AddSingleton<SessionTokenIssuer>();
        services.AddSingleton<IFirebaseIdTokenVerifier, FirebaseIdTokenVerifier>();
        services.AddSingleton<LoginService>();
        services.AddSingleton<ISesionesApi, SesionesApi>();
        services.AddSingleton<ISesionCancelada, RedisSesionCancelada>();
        services.AddSingleton<SessionRefreshService>();
        services.AddSingleton<LogoutService>();
        services.AddSingleton<SessionManagementService>();
        return services;
    }

    // Se validan al crear el servicio del canje y no al arrancar (ValidateOnStart pide llaves sintéticas en scripts/export_openapi.py). Llaves
    // inválidas nunca se aceptan: el mensaje nombra la clave de configuración y jamás su valor.
    private static PhoneProtectionKeys CreatePhoneProtectionKeys(IServiceProvider provider)
    {
        var options = provider.GetRequiredService<IOptions<PhoneProtectionOptions>>().Value;
        return PhoneProtectionKeys.TryCreate(options, out var errors)
            ?? throw new OptionsValidationException(Options.DefaultName, typeof(PhoneProtectionOptions), errors);
    }
}
