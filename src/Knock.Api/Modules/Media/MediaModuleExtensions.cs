using Azure.Storage.Blobs;
using Knock.Api.Hosting;
using Knock.Api.Modules.Media.Data;
using Knock.Api.Modules.Media.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Knock.Api.Modules.Media;

/// <summary>Registro del módulo Media (servicio Mercado). Solo esta clase y <see cref="IMediaApi"/> son públicas; el resto del módulo es internal.</summary>
public static class MediaModuleExtensions
{
    public static IServiceCollection AddMediaModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton(TimeProvider.System);
        services.AddOptions<MediaOptions>()
            .BindConfiguration(MediaOptions.SectionName)
            .Validate(media => media.MaxConcurrentProcessing >= 1, "Media:MaxConcurrentProcessing must be 1 or more.")
            .ValidateOnStart();
        services.AddSingleton(provider =>
        {
            var connectionString = provider.GetRequiredService<IConfiguration>().GetConnectionString(MediaOptions.ConnectionStringName);
            return string.IsNullOrWhiteSpace(connectionString)
                ? throw new InvalidOperationException($"ConnectionStrings:{MediaOptions.ConnectionStringName} is not configured.")
                : new BlobServiceClient(connectionString);
        });
        services.AddSingleton<IMediaFileStore, BlobMediaFileStore>();
        services.AddSingleton<MediaBlobStorage>();
        services.AddSingleton<ImageProcessor>();
        services.AddSingleton<IModerationHook, NoModerationHook>();
        services.AddSingleton<MediaService>();
        services.AddSingleton<IMediaApi, MediaApi>();
        services.AddHealthChecks().AddCheck<BlobReadinessHealthCheck>("blob", tags: [HealthExtensions.ReadyTag]);
        return services;
    }
}
