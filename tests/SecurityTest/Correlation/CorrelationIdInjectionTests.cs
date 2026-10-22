using System.Text.Json;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.SecurityTest.Correlation;

public class CorrelationIdInjectionTests
{
    private const string _header = "X-Correlation-Id";
    private const string _marker = "HOSTILE-MARKER-9f3a";

    public static TheoryData<string> HostileValues => new()
    {
        _marker + new string('x', 60),
        _marker + "-with-space bad",
        _marker + "\r\nX-Injected: yes",
        _marker + "\nX-Injected: yes",
        "{\"" + _marker + "\":\"x\"}",
        _marker + "<script>alert(1)</script>",
        _marker + "/../..",
        _marker + "%0d%0aX-Injected:yes",
    };

    [Theory]
    [MemberData(nameof(HostileValues))]
    public async Task HostileHeader_IsReplaced_NoLogOrHeaderInjection_TC_TT_API_03_10(string hostile)
    {
        string? responseId = null;
        var injected = false;
        var output = await ConsoleCapture.RunAsync(async () =>
        {
            using var factory = new KnockApiFactory();
            using var client = factory.CreateClient();
            using var response = await TestHttp.GetAsync(client, "/api/v1/test/ping", headers: new Dictionary<string, string> { [_header] = hostile });
            responseId = response.Headers.GetValues(_header).Single();
            injected = response.Headers.Contains("X-Injected");
            var raw = await response.Content.ReadAsStringAsync();
            raw.Should().NotContain(_marker);
        });

        responseId.Should().MatchRegex("^[0-9a-f]{32}$", "el valor hostil se descarta y se genera un GUID sin guiones");
        injected.Should().BeFalse("no se inyectan encabezados de respuesta");
        output.Should().NotContain(_marker, "el valor enviado no llega a los logs");

        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var requestLine = lines.Single(l => l.Contains("\"RequestPath\"", StringComparison.Ordinal) && l.Contains("ping", StringComparison.Ordinal));
        foreach (var line in lines)
        {
            using var json = JsonDocument.Parse(line);
            json.RootElement.TryGetProperty("@t", out _).Should().BeTrue("ninguna línea de log se parte: todas son eventos JSON completos");
        }

        using var request = JsonDocument.Parse(requestLine);
        request.RootElement.GetProperty("CorrelationId").GetString().Should().Be(responseId);
    }
}
