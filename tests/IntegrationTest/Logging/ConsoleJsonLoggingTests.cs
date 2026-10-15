using System.Text.Json;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.IntegrationTest.Logging;

public class ConsoleJsonLoggingTests
{
    [Fact]
    public async Task Request_WritesJsonLineToConsole_TC_TT_API_01_13()
    {
        var original = Console.Out;
        using var captured = new StringWriter();
        string output;
        try
        {
            Console.SetOut(TextWriter.Synchronized(captured));
            using var factory = new KnockApiFactory();
            using var client = factory.CreateClient();
            using var response = await client.GetAsync(new Uri("/health", UriKind.Relative));
            response.EnsureSuccessStatusCode();
            output = captured.ToString();
        }
        finally
        {
            Console.SetOut(original);
        }

        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        lines.Should().NotBeEmpty();
        foreach (var line in lines)
        {
            using var json = JsonDocument.Parse(line);
            json.RootElement.TryGetProperty("@t", out _).Should().BeTrue($"cada línea debe ser un evento JSON de Serilog: {line}");
        }

        lines.Should().Contain(line => line.Contains("\"RequestPath\":\"/health\"", StringComparison.Ordinal));
    }
}
