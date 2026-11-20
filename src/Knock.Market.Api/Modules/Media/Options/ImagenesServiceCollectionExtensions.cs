using Knock.Market.Api.Modules.Media.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Knock.Market.Api.Modules.Media.Options;

internal static class ImagenesServiceCollectionExtensions
{
    /// <summary>Registra <see cref="ImagenesOptions"/> validadas al arrancar y <see cref="ImagenPublicaUrl"/> (spec 032).</summary>
    public static IServiceCollection AddImagenesPublicas(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddOptions<ImagenesOptions>()
            .BindConfiguration(ImagenesOptions.SectionName)
            .ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<ImagenesOptions>, ImagenesOptionsValidator>());
        services.TryAddSingleton<ImagenPublicaUrl>();
        return services;
    }
}
