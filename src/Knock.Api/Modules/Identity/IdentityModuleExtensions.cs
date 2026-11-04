using Knock.Api.Modules.Identity.Data;
using Knock.Api.Modules.Identity.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Knock.Api.Modules.Identity;

/// <summary>Registro del módulo Identity. Solo esta clase y <see cref="IIdentityApi"/> son públicas; el resto del módulo es internal.</summary>
public static class IdentityModuleExtensions
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton(TimeProvider.System);
        services.AddIdentityData();
        services.AddSingleton<IIdentityApi, IdentityApi>();
        services.AddOptions<PhoneProtectionOptions>().BindConfiguration(PhoneProtectionOptions.SectionName);
        services.AddSingleton<IValidateOptions<PhoneProtectionOptions>, PhoneProtectionOptionsValidator>();
        services.AddSingleton(CreatePhoneProtectionKeys);
        services.AddSingleton<PhoneProtector>();
        services.AddSingleton<IIdentityStore>(provider => provider.GetRequiredService<IdentityStore>());
        services.AddSingleton<IProfileStore>(provider => provider.GetRequiredService<IdentityStore>());
        services.AddSingleton<ProfileService>();
        services.AddSingleton<SessionTokenIssuer>();
        services.AddSingleton<IFirebaseIdTokenVerifier, FirebaseIdTokenVerifier>();
        services.AddSingleton<FirebaseSessionService>();
        return services;
    }

    // Se validan al crear el servicio del canje y no al arrancar (ValidateOnStart pide llaves sintéticas en scripts/export_openapi.py, pendiente
    // del líder). Llaves inválidas nunca se aceptan: el mensaje nombra la clave de configuración y jamás su valor.
    private static PhoneProtectionKeys CreatePhoneProtectionKeys(IServiceProvider provider)
    {
        var options = provider.GetRequiredService<IOptions<PhoneProtectionOptions>>().Value;
        return PhoneProtectionKeys.TryCreate(options, out var errors)
            ?? throw new OptionsValidationException(Options.DefaultName, typeof(PhoneProtectionOptions), errors);
    }
}
