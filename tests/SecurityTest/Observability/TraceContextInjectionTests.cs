using System.Diagnostics;
using System.Net;
using AwesomeAssertions;
using Knock.Api.Hosting;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.SecurityTest.Observability;

public class TraceContextInjectionTests
{
    private const string _clientTraceId = "4bf92f3577b34da6a3ce929d0e0e4736";
    private const string _validTraceparent = "00-" + _clientTraceId + "-00f067aa0ba902b7-01";
    private const string _baggagePhone = "15555550199";
    private static readonly string _baggageToken = SyntheticTokens.Opaque("tok", "SECRET", "baggage", "05");

    [Fact]
    public async Task HostileTraceContextAndBaggage_DoNotReachTelemetry_TC_TT_OBS_01_05()
    {
        using var factory = new KnockApiFactory();
        using var client = factory.CreateClient();
        var hostileId = "corr\r\nX-Injected: 1" + new string('a', 200);

        var statuses = new List<HttpStatusCode>();
        foreach (var headers in new[]
        {
            new Dictionary<string, string> { ["traceparent"] = _validTraceparent },
            new Dictionary<string, string> { ["traceparent"] = "00-not-a-valid-trace-context" },
            new Dictionary<string, string> { ["traceparent"] = _validTraceparent, ["baggage"] = $"phone=%2B{_baggagePhone},token={_baggageToken}" },
            new Dictionary<string, string> { ["X-Correlation-Id"] = hostileId },
        })
        {
            using var response = await TestHttp.GetAsync(client, "/api/v1/test/log", headers: headers);
            statuses.Add(response.StatusCode);
        }

        statuses.Should().AllBeEquivalentTo(HttpStatusCode.OK, "la petición responde normal");
        var spans = await factory.Telemetry.WaitForServerSpansAsync(4);
        spans.Should().AllSatisfy(span =>
        {
            span.TraceId.ToHexString().Should().NotBe(_clientTraceId, "la API inicia siempre una traza nueva (decisión P9)");
            span.ParentSpanId.Should().Be(default(ActivitySpanId), "el span de servidor no tiene padre remoto");
            span.HasRemoteParent.Should().BeFalse();
            span.Baggage.Should().BeEmpty("el baggage del cliente no se toma");
        });
        spans.Select(s => s.TraceId).Distinct().Should().HaveCount(4, "cada petición es su propia traza");

        var exported = string.Join('\n', spans.Select(TelemetryCapture.Dump).Concat(factory.Telemetry.Logs.Select(TelemetryCapture.Dump)));
        exported.Should().NotContain(_baggagePhone).And.NotContain(_baggageToken).And.NotContain(_clientTraceId);

        var hostileSpan = spans[3];
        var correlation = hostileSpan.GetTagItem(ObservabilityExtensions.CorrelationIdAttribute).Should().BeOfType<string>().Subject;
        correlation.Should().MatchRegex("^[0-9a-f]{32}$", "el id hostil se reemplaza por el generado (TC-TT-API-03-10)");
        exported.Should().NotContain("X-Injected");
    }
}
