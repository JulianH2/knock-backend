using Knock.Identity.Api.Modules.HomeAndCar.Data;
using Knock.Identity.Api.Modules.HomeAndCar.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Knock.Identity.Api.Modules.HomeAndCar;

/// <summary>Registro del módulo HomeAndCar (spec 041: Mi casa y mi auto). Solo esta clase es pública; el resto del módulo es internal.</summary>
public static class HomeAndCarModuleExtensions
{
    public static IServiceCollection AddHomeAndCarModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<HomeAndCarStore>();
        services.AddSingleton<IHomeAndCarStore>(provider => provider.GetRequiredService<HomeAndCarStore>());
        services.AddOptions<MarketHistoryOptions>().BindConfiguration(MarketHistoryOptions.SectionName).ValidateDataAnnotations();
        services.AddHttpContextAccessor();
        // Sin Services:Market:BaseUrl o sin la clave el cliente no puede llamar y el historial responde 503, igual que el catálogo de colonias.
        services.AddHttpClient<IMarketHistory, MarketHistoryClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<MarketHistoryOptions>>().Value;
            if (Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseAddress) && baseAddress.Scheme is "http" or "https")
            {
                client.BaseAddress = baseAddress;
            }

            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        services.AddScoped<HomeAndCarService>();
        return services;
    }
}
