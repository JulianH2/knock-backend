using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Knock.Api.Hosting;

internal static class HealthExtensions
{
    public const string ReadyTag = "ready";

    /// <summary>Raíz de las rutas de salud (<c>/health</c> y <c>/health/ready</c>); no generan spans (TT-OBS-01).</summary>
    public const string LivenessPath = "/health";

    /// <summary>Las comprobaciones de readiness las registra cada dependencia: MongoDB (<c>AddKnockMongo</c>) y Blob (Media).</summary>
    public static IServiceCollection AddKnockHealth(this IServiceCollection services)
    {
        services.AddHealthChecks();
        return services;
    }

    public static IEndpointRouteBuilder MapKnockHealth(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks(LivenessPath, new HealthCheckOptions { Predicate = _ => false });
        endpoints.MapHealthChecks($"{LivenessPath}/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains(ReadyTag) });
        return endpoints;
    }
}
