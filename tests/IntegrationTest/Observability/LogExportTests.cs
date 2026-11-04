using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Knock.IntegrationTest.Observability;

public class LogExportTests
{
    [Fact]
    public async Task ServiceLogs_ExportedWithTraceAndCorrelation_SameLevelsAsSerilog_TC_TT_OBS_01_06()
    {
        using var factory = new KnockApiFactory();
        using var client = factory.CreateClient();

        using var response = await TestHttp.GetAsync(client, "/api/v1/test/log-levels", headers: new Dictionary<string, string> { ["X-Correlation-Id"] = "corr-obs-06" });

        var span = (await factory.Telemetry.WaitForServerSpansAsync(1)).Single();
        var logs = factory.Telemetry.Logs;
        var information = logs.Single(l => l.FormattedMessage == "Test service step executed");
        var warning = logs.Single(l => l.FormattedMessage == "Test service warning raised");

        information.LogLevel.Should().Be(LogLevel.Information);
        warning.LogLevel.Should().Be(LogLevel.Warning);
        foreach (var log in new[] { information, warning })
        {
            log.TraceId.Should().Be(span.TraceId, "el log lleva la traza de la petición");
            log.SpanId.Should().Be(span.SpanId);
            TelemetryCapture.Attribute(log, "CorrelationId").Should().Be("corr-obs-06");
            log.CategoryName.Should().Be(typeof(TestController).FullName);
        }

        logs.Should().Contain(l => l.Body != null && l.Body.StartsWith("HTTP {RequestMethod}", StringComparison.Ordinal), "el log de petición de Serilog también se exporta");
        logs.Where(l => l.CategoryName != null && l.CategoryName.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal))
            .Should().AllSatisfy(l => ((int)l.LogLevel).Should().BeGreaterThanOrEqualTo((int)LogLevel.Warning, "mismos niveles que Serilog"));
        logs.Should().NotContain(l => l.FormattedMessage != null && l.FormattedMessage.StartsWith("Request starting", StringComparison.Ordinal));
    }
}
