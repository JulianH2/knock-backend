using System.Diagnostics;
using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Knock.SecurityTest.Observability;

public class ExceptionTelemetryPrivacyTests
{
    [Fact]
    public async Task UnhandledException_ExportedWithoutSensitiveValues_TC_TT_OBS_01_10()
    {
        using var factory = new KnockApiFactory("Production");
        using var client = factory.CreateClient();

        using var response = await TestHttp.GetAsync(client, "/api/v1/test/boom-sensitive");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("lookup failed", "fuera de Development la respuesta no trae el detalle (no cambia)");

        var span = (await factory.Telemetry.WaitForServerSpansAsync(1)).Single();
        span.Status.Should().Be(ActivityStatusCode.Error);
        var exception = span.Events.Should().ContainSingle(e => e.Name == "exception").Which;
        var tags = exception.Tags.ToDictionary(t => t.Key, t => t.Value?.ToString() ?? string.Empty);
        tags["exception.type"].Should().Be(typeof(InvalidOperationException).FullName, "el tipo sí se exporta");
        tags["exception.message"].Should().Contain("lookup failed for");
        tags["exception.stacktrace"].Should().Contain(nameof(TestController.BoomSensitive));

        var errors = factory.Telemetry.Logs.Where(l => l.LogLevel == LogLevel.Error).ToList();
        errors.Should().NotBeEmpty("el log del error también se exporta");
        var exported = string.Join('\n', [TelemetryCapture.Dump(span), .. factory.Telemetry.Logs.Select(TelemetryCapture.Dump)]);
        exported.Should().NotContain("5555 0101", "sin el celular");
        exported.Should().NotContain(TestController.SensitiveJwt, "sin el JWT");
        exported.Should().NotContain("eyJ");
        exported.Should().NotContain("19.4326", "sin las coordenadas");
        exported.Should().NotContain("99.1332");
        errors.Should().AllSatisfy(error =>
            TelemetryCapture.Attribute(error, "exception.type").Should().Be(typeof(InvalidOperationException).FullName));
    }
}
