using System.Net;
using AwesomeAssertions;
using Knock.Api.Hosting;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.IntegrationTest.Observability;

public class CorrelationTelemetryTests
{
    private const string _header = "X-Correlation-Id";

    [Fact]
    public async Task SpanCarriesCorrelationId_ProblemTraceIdMatchesSpan_TC_TT_OBS_01_04()
    {
        using var factory = new KnockApiFactory();
        using var client = factory.CreateClient();

        using var propagated = await TestHttp.GetAsync(client, "/api/v1/test/ping", headers: new Dictionary<string, string> { [_header] = "corr-obs-04" });
        using var failing = await TestHttp.GetAsync(client, "/api/v1/test/boom");

        failing.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var generatedId = failing.Headers.GetValues(_header).Single();
        using var body = await TestHttp.ReadJsonAsync(failing);
        var traceId = body.RootElement.GetProperty("traceId").GetString();
        var correlationId = body.RootElement.GetProperty("correlationId").GetString();

        var spans = await factory.Telemetry.WaitForServerSpansAsync(2);
        var pingSpan = spans.Single(s => Equals(s.GetTagItem("url.path"), "/api/v1/test/ping"));
        var boomSpan = spans.Single(s => Equals(s.GetTagItem("url.path"), "/api/v1/test/boom"));

        pingSpan.GetTagItem(ObservabilityExtensions.CorrelationIdAttribute).Should().Be("corr-obs-04", "el id propagado llega al span");
        boomSpan.GetTagItem(ObservabilityExtensions.CorrelationIdAttribute).Should().Be(generatedId, "el id generado es el del encabezado");

        traceId.Should().MatchRegex("^00-[0-9a-f]{32}-[0-9a-f]{16}-0[01]$", "el formato W3C de TT-API-03 no cambia");
        traceId.Split('-')[1].Should().Be(boomSpan.TraceId.ToHexString(), "el <trace-id> del cuerpo es el operation_Id de la telemetría");
        correlationId.Should().Be(generatedId);
        correlationId.Should().NotBe(traceId, "traceId y correlationId siguen separados");
    }
}
