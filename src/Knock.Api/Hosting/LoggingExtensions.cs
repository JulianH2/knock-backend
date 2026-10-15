using Serilog;
using Serilog.Formatting.Compact;

namespace Knock.Api.Hosting;

internal static class LoggingExtensions
{
    /// <summary>Serilog a consola en JSON (una línea por evento), configurable desde appsettings.</summary>
    public static IServiceCollection AddKnockLogging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSerilog(logger => logger
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console(new RenderedCompactJsonFormatter()));
        return services;
    }
}
