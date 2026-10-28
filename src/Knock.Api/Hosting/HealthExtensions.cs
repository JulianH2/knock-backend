using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Knock.Api.Hosting;

internal static class HealthExtensions
{
    public const string ReadyTag = "ready";

    /// <summary>Las comprobaciones de readiness las registra cada dependencia: MongoDB (<c>AddKnockMongo</c>) y Blob (Media).</summary>
    public static IServiceCollection AddKnockHealth(this IServiceCollection services)
    {
        services.AddHealthChecks();
        return services;
    }

    public static IEndpointRouteBuilder MapKnockHealth(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains(ReadyTag) });
        return endpoints;
    }
}
