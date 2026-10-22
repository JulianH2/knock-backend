using System.Text.Json;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.IntegrationTest.Logging;

public class CorrelationLoggingTests
{
    [Fact]
    public async Task EveryLogOfARequest_CarriesCorrelationId_TC_TT_API_03_11()
    {
        var output = await ConsoleCapture.RunAsync(async () =>
        {
            using var factory = new KnockApiFactory();
            using var client = factory.CreateClient();
            using var failing = await TestHttp.GetAsync(client, "/api/v1/test/at/1/2", headers: new Dictionary<string, string> { ["X-Correlation-Id"] = "corr-A" });
            using var ping = await TestHttp.GetAsync(client, "/api/v1/test/ping", headers: new Dictionary<string, string> { ["X-Correlation-Id"] = "corr-B" });
        });

        var events = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => (Line: line, Json: JsonDocument.Parse(line).RootElement))
            .ToList();

        var service = events.Where(e => e.Line.Contains("Test service step executed", StringComparison.Ordinal)).ToList();
        service.Should().ContainSingle("un log del servicio durante la petición A");
        CorrelationOf(service[0].Json).Should().Be("corr-A");

        var errors = events.Where(e => e.Json.TryGetProperty("@l", out var l) && l.GetString() == "Error").ToList();
        errors.Should().NotBeEmpty("la petición A termina en error");
        errors.All(e => CorrelationOf(e.Json) == "corr-A").Should().BeTrue("todo log de error de la petición A trae su id");

        var requestLogs = events.Where(e => e.Json.TryGetProperty("RequestPath", out _) && e.Json.TryGetProperty("StatusCode", out _)).ToList();
        requestLogs.Should().HaveCount(2);
        CorrelationOf(requestLogs.Single(e => e.Line.Contains("ping", StringComparison.Ordinal)).Json).Should().Be("corr-B");
        CorrelationOf(requestLogs.Single(e => !e.Line.Contains("ping", StringComparison.Ordinal)).Json).Should().Be("corr-A");

        events.Where(e => CorrelationOf(e.Json) == "corr-B").All(e => e.Line.Contains("ping", StringComparison.Ordinal))
            .Should().BeTrue("los eventos de otra petición no llevan el id de A ni al revés");
    }

    private static string? CorrelationOf(JsonElement e) => e.TryGetProperty("CorrelationId", out var v) ? v.GetString() : null;
}
