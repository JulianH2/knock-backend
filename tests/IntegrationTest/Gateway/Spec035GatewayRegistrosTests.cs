using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using AwesomeAssertions;
using Knock.Gateway.Hosting;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Xunit;

namespace Knock.IntegrationTest.Gateway;

/// <summary>
/// Spec 035 (KNOCK-186, CA-03): con todos los niveles en <c>Verbose</c>, el gateway real reenvía una petición con consulta a un servicio y ningún registro
/// deja salir la consulta, ni la de YARP ni la de ASP.NET Core. Los demás servicios los comprueba <c>Spec035RegistrosTests</c> en SecurityTest.
/// Solo datos ficticios.
/// </summary>
public sealed class Spec035GatewayRegistrosTests
{
    [Fact]
    public async Task Error_Http_Y_Traza_Exportada_Sin_Consulta_CA_035_07_CA_035_16()
    {
        var traces = new ConcurrentBag<Dictionary<string, object?>>();
        var exported = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var host = await GatewayHost.StartAsync(
            configureServices: services =>
            {
                services.AddSingleton<IRevokedSessions>(new NoRevokedSessions());
                services.AddSingleton<IRequestCounter>(new NoLimitCounter());
                services.ConfigureOpenTelemetryTracerProvider(tracing =>
                    tracing.AddProcessor(new SimpleActivityExportProcessor(new TraceExporter(traces, exported))));
            });
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/api/v1/me?q=secreto&token=x", UriKind.Relative));
        using var response = await host.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await exported.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var span = traces.Should().ContainSingle().Which;
        span.Should().NotContainKey("url.query");
        foreach (var key in new[] { "url.full", "http.target" })
        {
            if (span.TryGetValue(key, out var value))
            {
                value?.ToString().Should().NotContain("?").And.NotContain("secreto").And.NotContain("token=x");
            }
        }

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("instance").GetString().Should().Be("/api/v1/me");
    }

    private sealed class TraceExporter(ConcurrentBag<Dictionary<string, object?>> traces, TaskCompletionSource exported) : BaseExporter<Activity>
    {
        public override ExportResult Export(in Batch<Activity> batch)
        {
            foreach (var activity in batch)
            {
                if (activity.Kind == ActivityKind.Server)
                {
                    traces.Add(activity.TagObjects.ToDictionary(tag => tag.Key, tag => tag.Value, StringComparer.Ordinal));
                    exported.TrySetResult();
                }
            }

            return ExportResult.Success;
        }
    }

    [Fact]
    public async Task Reenvio_Sin_Consulta_CA_035_03()
    {
        await using var host = await GatewayHost.StartAsync(
            QueryLeakDetector.VerboseSettings,
            services =>
            {
                services.AddSingleton<IRevokedSessions>(new NoRevokedSessions());
                services.AddSingleton<IRequestCounter>(new NoLimitCounter());
            });

        HttpStatusCode? status = null;
        var output = await ConsoleCapture.RunAsync(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/api/v1/colonias/en-punto" + QueryLeakDetector.Query, UriKind.Relative));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GatewayTestSupport.IssueToken());
            using var response = await host.Client.SendAsync(request);
            status = response.StatusCode;
        });

        status.Should().Be(HttpStatusCode.OK, "la petición sí se reenvió al servicio de Mercado");
        host.PathsSeen.Should().ContainSingle().Which.Should().Be("/api/v1/colonias/en-punto");
        output.Should().NotBeNullOrWhiteSpace("el gateway registra algo con todos los niveles en Verbose");
        QueryLeakDetector.Find(output).Should().BeEmpty();
    }
}
