using Knock.Contracts.Colonias;
using Knock.Identity.Api.Modules.Usuarios.Data;
using Knock.Identity.Api.Modules.Usuarios.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Knock.Identity.Api.Modules.Usuarios;

/// <summary>Registro del módulo Usuarios (spec 017: perfil de la persona). Solo esta clase es pública; el resto del módulo es internal.</summary>
public static class UsuariosModuleExtensions
{
    public static IServiceCollection AddUsuariosModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton(TimeProvider.System);
        services.AddOptions<ProfileOptions>().BindConfiguration(ProfileOptions.SectionName);
        services.AddOptions<ProfileCacheOptions>().BindConfiguration(ProfileCacheOptions.SectionName);
        services.AddSingleton<IValidateOptions<ProfileCacheOptions>, ProfileCacheOptionsValidator>();
        services.AddSingleton<UsuariosStore>();
        services.AddSingleton<IUsuariosStore>(provider => provider.GetRequiredService<UsuariosStore>());
        services.AddSingleton<DemoClientSeeder>();
        services.AddHostedService<DemoSeedStartup>();
        services.AddSingleton<IProfileCache, RedisProfileCache>();
        services.AddHttpContextAccessor();
        services.AddOptions<MarketCatalogOptions>()
            .BindConfiguration(MarketCatalogOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(options => options.HasValidBaseUrl(), "Services:Market:BaseUrl must be an absolute http or https address without a path.")
            .ValidateOnStart();
        // Sin Services:Market:BaseUrl el cliente no tiene dirección y falla como «Mercado no responde» (503), igual que antes de existir el endpoint.
        services.AddHttpClient<IColoniaCatalog, MarketColoniaCatalog>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<MarketCatalogOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                client.BaseAddress = new Uri(options.BaseUrl);
            }

            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        services.AddSingleton<ProfileRules>();
        services.AddScoped<ProfileService>();
        return services;
    }
}
