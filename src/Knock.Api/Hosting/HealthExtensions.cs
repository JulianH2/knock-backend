using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Knock.Api.Hosting;

internal static class HealthExtensions
{
    public const string ReadyTag = "ready";

    /// <summary>PostgreSQL forma parte de readiness; las demás dependencias se agregan en sus tareas.</summary>
    public static IServiceCollection AddKnockHealth(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<PostgresReadinessHealthCheck>("postgresql", tags: [ReadyTag]);
        return services;
    }

    public static IEndpointRouteBuilder MapKnockHealth(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains(ReadyTag) });
        return endpoints;
    }
}

internal sealed class PostgresReadinessHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString("KnockDatabase");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return HealthCheckResult.Unhealthy("PostgreSQL connection is not configured.");
        }

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return HealthCheckResult.Healthy();
    }
}
