using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.SecurityTest.Observability;

public class LogExportPrivacyTests
{
    [Fact]
    public async Task ExportedLogs_DoNotContainPhoneCurpTokensOrCoordinates_TC_TT_OBS_01_09()
    {
        using var factory = SensitiveTraffic.CreateFactory();
        var bearer = await SensitiveTraffic.SendAsync(factory);
        await factory.Telemetry.WaitForServerSpansAsync(SensitiveTraffic.RequestCount);

        var logs = factory.Telemetry.Logs;
        var lookup = logs.Single(l => l.Body == "Lookup for {Phone} with {Curp} at {lat}/{lon}");
        var exported = string.Join('\n', logs.Select(TelemetryCapture.Dump));

        SensitiveTraffic.AssertNoSensitiveValues(exported, bearer);
        exported.Should().NotContain("phone=").And.NotContain("token=tok", "sin la query");
        lookup.FormattedMessage.Should().Contain("[redacted]", "el par de coordenadas, el celular y la CURP del texto se redactan");
        TelemetryCapture.Attribute(lookup, "lat").Should().Be("[redacted]", "las claves conocidas se redactan siempre");
        TelemetryCapture.Attribute(lookup, "lon").Should().Be("[redacted]");
        TelemetryCapture.Attribute(lookup, "Phone").Should().Be("[redacted]");
        logs.Should().Contain(l => l.LogLevel == Microsoft.Extensions.Logging.LogLevel.Error, "el error de la ruta con coordenadas también se exporta");
    }
}
