extern alias gateway;
extern alias messaging;

using AwesomeAssertions;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;
using GatewayHosting = gateway::Knock.Gateway.Hosting;
using MessagingHosting = messaging::Knock.Messaging.Api.Hosting;

namespace Knock.UnitTest.Hosting;

/// <summary>
/// Spec 035 (KNOCK-186, R-01): el filtro de fuentes de cada proyecto descarta, por debajo de <c>Warning</c>, los eventos que escriben la dirección completa con su
/// consulta, y deja pasar los demás. Las cuatro copias de <c>LoggingExtensions</c> deben comportarse igual.
/// </summary>
public sealed class Spec035QueryBearingSourcesTests
{
    private static readonly (string Project, Func<LogEvent, bool> Filter)[] _filters =
    [
        ("gateway", GatewayHosting.LoggingExtensions.IsQueryBearingBelowWarning),
        ("identity", Knock.Identity.Api.Hosting.LoggingExtensions.IsQueryBearingBelowWarning),
        ("messaging", MessagingHosting.LoggingExtensions.IsQueryBearingBelowWarning),
        ("market", Knock.Market.Api.Hosting.LoggingExtensions.IsQueryBearingBelowWarning),
    ];

    public static TheoryData<string> Projects
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var (project, _) in _filters)
            {
                data.Add(project);
            }

            return data;
        }
    }

    private static LogEvent Event(LogEventLevel level, string? source)
    {
        var properties = new List<LogEventProperty>();
        if (source is not null)
        {
            properties.Add(new LogEventProperty("SourceContext", new ScalarValue(source)));
        }

        return new LogEvent(DateTimeOffset.UtcNow, level, null, new MessageTemplate("m", []), properties);
    }

    private static Func<LogEvent, bool> FilterOf(string project) => _filters.Single(f => f.Project == project).Filter;

    [Theory]
    [MemberData(nameof(Projects))]
    public void Excludes_The_Four_Sources_Below_Warning_CA_035_01(string project)
    {
        var filter = FilterOf(project);
        string[] sources =
        [
            "Microsoft.AspNetCore.Hosting.Diagnostics",
            "Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware",
            "System.Net.Http.HttpClient",
            "System.Net.Http.HttpClient.firebase.LogicalHandler",
            "Yarp.ReverseProxy.Forwarder.HttpForwarder",
        ];

        foreach (var source in sources)
        {
            foreach (var level in new[] { LogEventLevel.Verbose, LogEventLevel.Debug, LogEventLevel.Information })
            {
                filter(Event(level, source)).Should().BeTrue($"{source} en {level} escribe la consulta");
            }
        }
    }

    [Theory]
    [MemberData(nameof(Projects))]
    public void Keeps_Warnings_And_Errors_Of_Those_Sources_For_The_Formatter_To_Redact_CA_035_01(string project)
    {
        var filter = FilterOf(project);

        filter(Event(LogEventLevel.Warning, "Microsoft.AspNetCore.Hosting.Diagnostics")).Should().BeFalse();
        filter(Event(LogEventLevel.Error, "Yarp.ReverseProxy.Forwarder.HttpForwarder")).Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(Projects))]
    public void Keeps_Other_Sources_And_Events_Without_A_Source_CA_035_01(string project)
    {
        var filter = FilterOf(project);

        filter(Event(LogEventLevel.Information, "Serilog.AspNetCore.RequestLoggingMiddleware")).Should().BeFalse("el registro propio de la petición no se toca");
        filter(Event(LogEventLevel.Verbose, "Microsoft.AspNetCore.Routing.EndpointMiddleware")).Should().BeFalse();
        filter(Event(LogEventLevel.Information, "Microsoft.AspNetCore.HostingNotThis")).Should().BeFalse("otra fuente con un nombre parecido no se descarta");
        filter(Event(LogEventLevel.Information, null)).Should().BeFalse();
    }
}
