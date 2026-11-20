extern alias gateway;
extern alias messaging;

using System.Diagnostics;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Xunit;
using GatewayHosting = gateway::Knock.Gateway.Hosting;
using MessagingHosting = messaging::Knock.Messaging.Api.Hosting;

namespace Knock.UnitTest.Hosting;

/// <summary>
/// Spec 035 (KNOCK-186, T-03, R-04 · CA-06): la última barrera de los registros reemplaza la consulta de una dirección o ruta por
/// <c>?[redacted]</c> y conserva lo anterior al <c>?</c>. Las cuatro copias de <c>TelemetryRedactor</c> deben comportarse igual.
/// </summary>
public sealed class Spec035RedaccionTests
{
    private const string _placeholder = "[redacted]";

    private static readonly (string Project, Func<string, string> Redact)[] _redactors =
    [
        ("gateway", GatewayHosting.TelemetryRedactor.Redact),
        ("identity", Knock.Identity.Api.Hosting.TelemetryRedactor.Redact),
        ("messaging", MessagingHosting.TelemetryRedactor.Redact),
        ("market", Knock.Market.Api.Hosting.TelemetryRedactor.Redact),
    ];

    public static TheoryData<string> Projects
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var (project, _) in _redactors)
            {
                data.Add(project);
            }

            return data;
        }
    }

    private static Func<string, string> Redactor(string project) => _redactors.Single(r => r.Project == project).Redact;

    [Theory]
    [MemberData(nameof(Projects))]
    public async Task ProblemDetails_Sin_Consulta_CA_035_07(string project)
        => await CheckProblemInstanceAsync(project, null, "/error");

    [Theory]
    [MemberData(nameof(Projects))]
    public async Task ProblemDetails_Usa_Plantilla_Sin_Valores_CA_035_07(string project)
    {
        await CheckProblemInstanceAsync(project, "api/v1/tecnicos/{id}", "/api/v1/tecnicos/{id}");
        await CheckProblemInstanceAsync(project, "/api/v1/tecnicos/{id}", "/api/v1/tecnicos/{id}");
        await CheckProblemInstanceAsync(project, "api/v1/zonas/cp/{cp}", "/api/v1/zonas/cp/{cp}");
    }

    private static async Task CheckProblemInstanceAsync(string project, string? template, string expected)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var environment = Host.CreateEmptyApplicationBuilder(null).Environment;
        environment.EnvironmentName = "Production";
        services.AddSingleton<IHostEnvironment>(environment);
        switch (project)
        {
            case "gateway": GatewayHosting.ProblemResponses.AddKnockGatewayProblemDetails(services); break;
            case "identity": Knock.Identity.Api.Hosting.ProblemDetailsExtensions.AddKnockProblemDetails(services); break;
            case "messaging": MessagingHosting.ProblemResponses.AddKnockProblemDetails(services); break;
            case "market": Knock.Market.Api.Hosting.ProblemDetailsExtensions.AddKnockProblemDetails(services); break;
        }

        using var provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Path = template is null ? "/error" : "/api/v1/tecnicos/no-es-un-id";
        context.Request.QueryString = new QueryString("?q=synthetic-secret");
        if (template is not null)
        {
            context.SetEndpoint(new RouteEndpointBuilder(
                _ => Task.CompletedTask, RoutePatternFactory.Parse(template), 0).Build());
        }
        context.Response.Body = new MemoryStream();
        await provider.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = { Status = 500, Instance = "/error?q=synthetic-secret" },
        });
        context.Response.Body.Position = 0;
        using var json = await JsonDocument.ParseAsync(context.Response.Body);
        json.RootElement.GetProperty("instance").GetString().Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(Projects))]
    public void Trazas_Sin_Consulta_CA_035_16(string project)
    {
        using var source = new ActivitySource("Spec035." + project);
        using var capture = new TraceCapture();
        using BaseProcessor<Activity> redactor = project switch
        {
            "gateway" => new GatewayHosting.TelemetryRedactionProcessor(),
            "identity" => new Knock.Identity.Api.Hosting.TelemetryRedactionProcessor(),
            "messaging" => new MessagingHosting.TelemetryRedactionProcessor(),
            _ => new Knock.Market.Api.Hosting.TelemetryRedactionProcessor(),
        };
        using var provider = Sdk.CreateTracerProviderBuilder()
            .AddSource(source.Name)
            .AddProcessor(redactor)
            .AddProcessor(capture)
            .Build();
        using (var activity = source.StartActivity("request"))
        {
            activity.Should().NotBeNull();
            activity.SetTag("url.query", "q=synthetic-secret");
            activity.SetTag("url.full", "https://example.test/path?q=synthetic-secret");
        }

        capture.Tags.Should().NotBeNull();
        capture.Tags.Should().NotContainKey("url.query");
        capture.Tags!["url.full"]!.ToString().Should().NotContain("synthetic-secret");
    }

    private sealed class TraceCapture : BaseProcessor<Activity>
    {
        public Dictionary<string, object?>? Tags { get; private set; }

        public override void OnEnd(Activity data) => Tags = data.TagObjects.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
    }

    [Theory]
    [MemberData(nameof(Projects))]
    public void Redacta_Consultas_En_Texto_CA_035_06(string project)
    {
        var redact = Redactor(project);

        redact("GET https://host/ruta?a=1 respondió 200").Should().Be("GET https://host/ruta?[redacted] respondió 200");
        redact("/api/v1/x?y=2#z").Should().Be("/api/v1/x?[redacted]#z");
        redact("https://host?colonia=centro&oficio=plomeria").Should().Be("https://host?[redacted]");

        var exception = new InvalidOperationException("Falló la llamada a https://destino.interno/v1/buscar?q=fuga%20de%20agua&cp=06600");
        var redactedException = redact(exception.ToString());
        redactedException.Should().Contain("https://destino.interno/v1/buscar?[redacted]");
        redactedException.Should().NotContain("fuga");
        redactedException.Should().NotContain("06600");

        redact("La petición terminó sin consulta en /api/v1/oficios").Should().Be("La petición terminó sin consulta en /api/v1/oficios");
    }

    [Theory]
    [MemberData(nameof(Projects))]
    public void No_Redacta_El_Signo_De_Una_Frase_CA_035_06(string project)
    {
        var redact = Redactor(project);

        redact("¿Llegó el técnico? Sí, a tiempo").Should().Be("¿Llegó el técnico? Sí, a tiempo");
        redact("estado?ok en la cola").Should().Be("estado?ok en la cola");
        redact("ruta a/b?c=1 sin barra inicial").Should().Be("ruta a/b?c=1 sin barra inicial");
    }

    [Theory]
    [MemberData(nameof(Projects))]
    public void Una_Consulta_Larga_Se_Redacta_En_Tiempo_Lineal_CA_035_06(string project)
    {
        var redact = Redactor(project);
        var text = string.Concat(Enumerable.Repeat("/a/b?c=d&", 20_000)) + " fin";

        var stopwatch = Stopwatch.StartNew();
        var result = redact(text);
        stopwatch.Stop();

        result.Should().NotBe(_placeholder, "la regla no debe agotar su tiempo máximo");
        result.Should().NotContain("c=d");
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1));
    }
}
