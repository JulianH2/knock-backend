extern alias gateway;
extern alias messaging;

using System.Diagnostics;
using AwesomeAssertions;
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
