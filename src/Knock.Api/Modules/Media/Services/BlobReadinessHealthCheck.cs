using Azure.Storage.Blobs;
using Knock.Api.Modules.Media.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Knock.Api.Modules.Media.Services;

/// <summary>Readiness de Blob: la conexión está configurada y el servicio responde (sin exponer la cadena ni el host).</summary>
internal sealed class BlobReadinessHealthCheck(IConfiguration configuration, IServiceProvider services, IOptions<MediaOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString(MediaOptions.ConnectionStringName)))
        {
            return HealthCheckResult.Unhealthy("Blob storage connection is not configured.");
        }

        var container = services.GetRequiredService<BlobServiceClient>().GetBlobContainerClient(options.Value.FilesContainer);
        await container.ExistsAsync(cancellationToken);
        return HealthCheckResult.Healthy();
    }
}
