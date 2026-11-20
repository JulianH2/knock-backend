using Asp.Versioning;

namespace Knock.Messaging.Api.Hosting;

internal static class ApiVersioningExtensions
{
    /// <summary>Versión en la ruta (<c>/api/v1/...</c>), sin versión por omisión: una ruta sin versión no existe.</summary>
    public static IServiceCollection AddKnockApiVersioning(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = false;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddMvc();
        return services;
    }
}
