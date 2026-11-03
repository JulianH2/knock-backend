using System.Diagnostics;
using System.Net;
using AwesomeAssertions;
using Knock.Api.Hosting;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Knock.IntegrationTest.Observability;

public class ExporterRegistrationTests
{
    private const string _ping = "/api/v1/test/ping";
    private const string _statsbeatVariable = "APPLICATIONINSIGHTS_STATSBEAT_DISABLED";

    /// <summary>Cadena sintética: clave de ceros (<see cref="Guid.Empty"/>) y un destino local que no responde (puerto 9). Nunca es Azure.</summary>
    private static string SyntheticConnectionString() =>
        string.Join(';', $"InstrumentationKey={Guid.Empty}", "IngestionEndpoint=" + new UriBuilder(Uri.UriSchemeHttp, "127.0.0.1", 9).Uri);

    [Theory]
    [InlineData("Development", null)]
    [InlineData("Testing", null)]
    [InlineData("Development", "")]
    [InlineData("Production", "   ")]
    public async Task NoConnectionString_RegistersNoAzureMonitorExporter_TC_TT_OBS_01_01(string environment, string? connectionString)
    {
        using var factory = new KnockApiFactory(
            environment,
            settings: new Dictionary<string, string?> { [ObservabilityExtensions.ConnectionStringKey] = connectionString });
        using var client = factory.CreateClient();

        using var response = await TestHttp.GetAsync(client, _ping);

        response.StatusCode.Should().Be(HttpStatusCode.OK, "la API responde igual que sin observabilidad");
        var status = factory.Services.GetRequiredService<AzureMonitorExportStatus>();
        status.Enabled.Should().BeFalse();
        status.Signals.Should().BeEmpty("no se construyó ningún exportador de Azure Monitor (trazas, métricas ni logs)");
        (await factory.Telemetry.WaitForServerSpansAsync(1)).Should().ContainSingle("la tubería es la misma: solo falta el exportador");
    }

    [Fact]
    public async Task SyntheticConnectionString_ExportsWithoutBlockingRequests_TC_TT_OBS_01_02()
    {
        var statsbeat = Environment.GetEnvironmentVariable(_statsbeatVariable);
        // Sin Statsbeat el exportador no intenta salir a los puntos de Microsoft: la prueba no toca la red.
        Environment.SetEnvironmentVariable(_statsbeatVariable, "true");
        try
        {
            await using var factory = new KnockApiFactory(
                "Production",
                settings: new Dictionary<string, string?> { [ObservabilityExtensions.ConnectionStringKey] = SyntheticConnectionString() });
            var statuses = new List<HttpStatusCode>();
            using (var client = factory.CreateClient())
            {
                for (var i = 0; i < 20; i++)
                {
                    using var response = await TestHttp.GetAsync(client, _ping);
                    statuses.Add(response.StatusCode);
                }
            }

            var status = factory.Services.GetRequiredService<AzureMonitorExportStatus>();
            var watch = Stopwatch.StartNew();
            await factory.DisposeAsync();
            watch.Stop();

            statuses.Should().HaveCount(20).And.AllBeEquivalentTo(HttpStatusCode.OK, "la exportación no bloquea ni lanza");
            status.Enabled.Should().BeTrue();
            status.Signals.Keys.Should().BeEquivalentTo(
                [ObservabilityExtensions.TracesSignal, ObservabilityExtensions.MetricsSignal, ObservabilityExtensions.LogsSignal],
                "se construyen los tres exportadores");
            status.Signals.Values.Should().AllSatisfy(signal =>
            {
                signal.DisableOfflineStorage.Should().BeTrue("no se escribe telemetría en el disco del contenedor");
                signal.EnableLiveMetrics.Should().BeFalse("Live Metrics apagado (decisión P7)");
                signal.SamplingRatio.Should().Be(1.0f, "100 % de las trazas (decisión P7)");
                signal.TracesPerSecond.Should().BeNull("sin el límite de 5 trazas por segundo que trae el exportador");
            });
            watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5), "el cierre no espera a Application Insights");
        }
        finally
        {
            Environment.SetEnvironmentVariable(_statsbeatVariable, statsbeat);
        }
    }
}
