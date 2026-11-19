using Knock.Identity.Api.Common.Crypto;
using Knock.Identity.Api.Modules.Addresses.Data;
using Knock.Identity.Api.Modules.Addresses.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Knock.Identity.Api.Modules.Addresses;

/// <summary>Registro del módulo Addresses (spec 040: direcciones del cliente). Solo esta clase es pública; el resto del módulo es internal.</summary>
public static class AddressesModuleExtensions
{
    public static IServiceCollection AddAddressesModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton(TimeProvider.System);
        // Sin llave configurada la API arranca, pero guardar o leer una dirección falla: nunca se guarda en claro (spec 040, §8).
        services.AddOptions<FieldEncryptionOptions>().BindConfiguration(FieldEncryptionOptions.SectionName);
        services.TryAddSingleton<FieldCipher>();
        services.AddSingleton<AddressCipher>();
        services.AddSingleton<AddressesStore>();
        services.AddSingleton<IAddressesStore>(provider => provider.GetRequiredService<AddressesStore>());
        services.AddScoped<AddressService>();
        return services;
    }
}
