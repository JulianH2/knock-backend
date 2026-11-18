using Knock.Identity.Api.Modules.HomeAndCar.Data;
using Knock.Identity.Api.Modules.HomeAndCar.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        services.AddScoped<HomeAndCarService>();
        return services;
    }
}
