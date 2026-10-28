using AwesomeAssertions;
using Knock.Api.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using Xunit;

namespace Knock.UnitTest.Hosting;

public sealed class MongoReadinessHealthCheckTests
{
    private const string _secretHost = "knock-secret-host.invalid";

    [Fact]
    public async Task UnexpectedErrorReportsOnlyItsTypeAndCallerCancellationPropagates_TC_TT_BD_06_14()
    {
        using var failing = Client(failOutsideTheDriver: true);
        using var idle = Client(failOutsideTheDriver: false);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        var result = await new MongoReadinessHealthCheck(failing).CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
        var cancel = () => new MongoReadinessHealthCheck(idle).CheckHealthAsync(new HealthCheckContext(), cancelled.Token);

        result.Status.Should().Be(HealthStatus.Unhealthy, "un error que no es del driver también deja la API fuera de readiness");
        result.Description.Should().Contain(nameof(InvalidOperationException));
        result.Description.Should().NotContain(_secretHost, "el mensaje puede traer el host de la cadena");
        result.Exception.Should().BeNull("HealthCheckService registraría su mensaje");
        await cancel.Should().ThrowAsync<OperationCanceledException>("la cancelación que pide quien llama se propaga");
    }

    /// <summary>
    /// Cliente hacia un host que no existe. Con <paramref name="failOutsideTheDriver"/>, al seleccionar servidor lanza una excepción
    /// que no es del driver y lleva el host en el mensaje.
    /// </summary>
    private static MongoClient Client(bool failOutsideTheDriver) => new(new MongoClientSettings
    {
        Server = new MongoServerAddress(_secretHost),
        ServerSelectionTimeout = TimeSpan.FromSeconds(2),
        ClusterConfigurator = cluster =>
        {
            if (failOutsideTheDriver)
            {
                cluster.Subscribe<ClusterSelectingServerEvent>(
                    _ => throw new InvalidOperationException($"Unexpected failure talking to {_secretHost}:10260."));
            }
        },
    });
}
