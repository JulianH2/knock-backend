using System.Globalization;
using System.Text.Json;
using AwesomeAssertions;
using Knock.Market.Api.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Xunit;

namespace Knock.UnitTest.Hosting;

[Collection("Spec011ConsoleOutput")]
public sealed class MarketConsoleRedactionTests
{
    private static readonly Action<ILogger, string, Exception?> _write =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(255, "MarketRedaction"), "Synthetic value {Value}");

    [Fact]
    public async Task Phone_IsRedacted_TC_BUG_005_01() => await AssertRedactedAsync("+52 55 5555 0101", "5555 0101");

    [Fact]
    public async Task Email_IsRedacted_TC_BUG_005_02() => await AssertRedactedAsync("persona@ejemplo.test", "persona@ejemplo.test");

    [Fact]
    public async Task Token_IsRedacted_TC_BUG_005_03()
    {
        var jwt = string.Join('.', "eyJ" + "hbGciOiJub25lIn0", "eyJ" + "zdWIiOiJmaWN0aWNpbyJ9", "firma-ficticia");
        await AssertRedactedAsync("Bearer " + jwt, "firma-ficticia");
    }

    [Fact]
    public async Task Coordinates_AreRedacted_TC_BUG_005_04() => await AssertRedactedAsync("19.4326, -99.1332", "19.4326", "99.1332");

    [Fact]
    public async Task Output_RemainsValidJson_TC_BUG_005_05()
    {
        var (line, _) = await CaptureAsync("+52 55 5555 0101");
        using var json = JsonDocument.Parse(line);
        line.Should().NotContain("5555 0101");
        json.RootElement.GetProperty("SafeCount").GetInt32().Should().Be(42);
        json.RootElement.GetProperty("SafeText").GetString().Should().Be("ordinary");
        json.RootElement.GetProperty("@m").GetString().Should().Contain("[redacted]");
    }

    [Fact]
    public async Task ServiceName_IsKnockMarket_TC_BUG_005_07()
    {
        var (_, serviceName) = await CaptureAsync("ordinary");
        serviceName.Should().Be("knock-market");
        new ObservabilityOptions().ServiceName.Should().Be("knock-market");
    }

    [Fact]
    public void NumericSensitiveProperties_AreRedacted_AndOutputRemainsJson()
    {
        using var buffer = new StringWriter(CultureInfo.InvariantCulture);
        var properties = new[]
        {
            new LogEventProperty("lat", new ScalarValue(19.4326)),
            new LogEventProperty("lon", new ScalarValue(-99.1332)),
            new LogEventProperty("SafeCount", new ScalarValue(42)),
            new LogEventProperty("Origin", new StructureValue([new LogEventProperty("lat", new ScalarValue(19.4326))])),
        };
        var logEvent = new LogEvent(DateTimeOffset.UnixEpoch, LogEventLevel.Information, null, new MessageTemplate("numeric", []), properties);

        new RedactingJsonFormatter(new RenderedCompactJsonFormatter()).Format(logEvent, buffer);

        var line = buffer.ToString();
        using var json = JsonDocument.Parse(line);
        line.Should().NotContain("19.4326").And.NotContain("99.1332");
        json.RootElement.GetProperty("lat").GetString().Should().Be("[redacted]");
        json.RootElement.GetProperty("lon").GetString().Should().Be("[redacted]");
        json.RootElement.GetProperty("Origin").GetProperty("lat").GetString().Should().Be("[redacted]");
        json.RootElement.GetProperty("SafeCount").GetInt32().Should().Be(42);
    }

    private static async Task AssertRedactedAsync(string value, params string[] secrets)
    {
        var (line, _) = await CaptureAsync(value);
        using var json = JsonDocument.Parse(line);
        var message = json.RootElement.GetProperty("@m").GetString();
        var property = json.RootElement.GetProperty("Value").GetString();
        foreach (var secret in secrets)
        {
            message.Should().NotContain(secret);
            property.Should().NotContain(secret);
            line.Should().NotContain(secret);
        }

        message.Should().Contain("[redacted]");
        property.Should().Contain("[redacted]");
    }

    private static async Task<(string Line, string? ServiceName)> CaptureAsync(string value)
    {
        var original = Console.Out;
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        Console.SetOut(output);
        using var capture = new ResourceCapture();
        try
        {
            var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = "Testing", ApplicationName = "Knock.Market.Api" });
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Knock.slnx")))
            {
                directory = directory.Parent;
            }

            var root = directory?.FullName ?? throw new InvalidOperationException("Knock.slnx not found");
            builder.Configuration.AddJsonFile(Path.Combine(root, "src", "Knock.Market.Api", "appsettings.json"));
            builder.Services.AddKnockLogging(builder.Configuration);
            builder.Services.AddKnockObservability(builder.Configuration, builder.Environment);
            builder.Services.ConfigureOpenTelemetryLoggerProvider(logging => logging.AddProcessor(capture));
            using var host = builder.Build();
            await host.StartAsync();
            var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger<MarketConsoleRedactionTests>();
            using (logger.BeginScope(new Dictionary<string, object> { ["SafeCount"] = 42, ["SafeText"] = "ordinary" }))
            {
                _write(logger, value, null);
            }

            await host.StopAsync();
        }
        finally
        {
            Console.SetOut(original);
        }

        var line = output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Single(item => item.Contains(typeof(MarketConsoleRedactionTests).FullName!, StringComparison.Ordinal));
        return (line, capture.ServiceName);
    }

    private sealed class ResourceCapture : BaseProcessor<LogRecord>
    {
        public string? ServiceName { get; private set; }

        public override void OnEnd(LogRecord data)
        {
            ServiceName = ParentProvider?.GetResource().Attributes
                .FirstOrDefault(attribute => attribute.Key == "service.name").Value?.ToString();
        }
    }
}
