using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.IntegrationTest.Observability;

public class OutgoingHttpTelemetryTests
{
    private static readonly string _secretToken = SyntheticTokens.Opaque("tok", "SECRET", "11", "outgoing");
    private const string _secretSignature = "sig-SECRET-11";

    [Fact]
    public async Task ClientSpan_RedactsQueryAndHeaders_PropagatesTraceparent_TC_TT_OBS_01_11()
    {
        using var factory = new KnockApiFactory();
        _ = factory.Services;
        using var server = OneShotServer.Start();

        using (var client = new HttpClient())
        using (var request = new HttpRequestMessage(HttpMethod.Get, server.Url($"sig={_secretSignature}&se=2026-11-02&token={_secretToken}")))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _secretToken);
            using var response = await client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var traceparent = await server.ReceivedTraceparent;
        var clientSpan = await WaitForClientSpanAsync(factory.Telemetry);
        var dump = TelemetryCapture.Dump(clientSpan);

        var fullUrl = clientSpan.GetTagItem("url.full").Should().BeOfType<string>().Subject;
        fullUrl.Should().StartWith(server.Url(string.Empty).GetLeftPart(UriPartial.Path) + "?");
        fullUrl[(fullUrl.IndexOf('?', StringComparison.Ordinal) + 1)..].Split('&').Should().AllSatisfy(
            parameter => parameter.Should().EndWith("=Redacted", "url.full sin valores de la query (como una URL SAS de Blob)"));
        dump.Should().NotContain(_secretToken).And.NotContain(_secretSignature).And.NotContain("2026-11-02").And.NotContain("Bearer");
        clientSpan.TagObjects.Should().NotContain(t => t.Key.StartsWith("http.request.header.", StringComparison.Ordinal));
        traceparent.Should().NotBeNull("el traceparent se propaga en la llamada saliente (ADR-001 regla 5)");
        var parts = traceparent.Split('-');
        parts[1].Should().Be(clientSpan.TraceId.ToHexString());
        parts[2].Should().Be(clientSpan.SpanId.ToHexString());
    }

    private static async Task<Activity> WaitForClientSpanAsync(TelemetryCapture telemetry)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            if (telemetry.Spans.FirstOrDefault(s => s.Kind == ActivityKind.Client) is { } span)
            {
                return span;
            }

            await Task.Delay(20);
        }

        throw new TimeoutException("No se exportó el span de cliente.");
    }

    /// <summary>Servidor HTTP local que responde 200 a una sola petición y guarda su <c>traceparent</c>.</summary>
    private sealed class OneShotServer : IDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly int _port;

        private OneShotServer(int port)
        {
            _port = port;
            _listener.Prefixes.Add(new UriBuilder(Uri.UriSchemeHttp, "127.0.0.1", port).Uri.ToString());
            _listener.Start();
            ReceivedTraceparent = AnswerOnceAsync();
        }

        public Task<string?> ReceivedTraceparent { get; }

        public static OneShotServer Start()
        {
            using var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            return new OneShotServer(port);
        }

        public Uri Url(string query) => new UriBuilder(Uri.UriSchemeHttp, "127.0.0.1", _port, "/blob/a.jpg") { Query = query }.Uri;

        public void Dispose() => _listener.Close();

        private async Task<string?> AnswerOnceAsync()
        {
            var context = await _listener.GetContextAsync();
            var traceparent = context.Request.Headers["traceparent"];
            context.Response.StatusCode = 200;
            context.Response.Close();
            return traceparent;
        }
    }
}
