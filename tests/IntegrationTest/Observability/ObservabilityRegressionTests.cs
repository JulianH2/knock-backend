using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.IntegrationTest.Observability;

/// <summary>Con la observabilidad activa y el exportador en memoria, los contratos de TT-API-03 siguen iguales (sin editar sus pruebas).</summary>
public class ObservabilityRegressionTests
{
    private const string _header = "X-Correlation-Id";

    [Fact]
    public async Task ExistingCorrelationAndPipelineContractsHold_TC_TT_OBS_01_14()
    {
        using var factory = new KnockApiFactory(settings: TestHttp.Limits(globalPermits: 2, authPermits: 1000));
        using var client = factory.CreateClient();
        var headers = new Dictionary<string, string> { [_header] = "corr-obs-14" };

        using var failing = await TestHttp.GetAsync(client, "/api/v1/test/boom", "203.0.113.140", headers);
        using var unauthorized = await TestHttp.GetAsync(client, "/api/v1/me", "203.0.113.140");
        using var limited = await TestHttp.GetAsync(client, "/api/v1/me", "203.0.113.140");

        failing.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        failing.Headers.GetValues(_header).Should().ContainSingle().Which.Should().Be("corr-obs-14", "la correlación corre antes del manejador de excepciones");
        using var body = await TestHttp.ReadJsonAsync(failing);
        body.RootElement.EnumerateObject().Select(p => p.Name).Should().Contain(["traceId", "correlationId"]);
        body.RootElement.GetProperty("correlationId").GetString().Should().Be("corr-obs-14");
        unauthorized.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests, "el limitador sigue antes de la autenticación");
        limited.Headers.Contains(_header).Should().BeTrue();

        (await factory.Telemetry.WaitForServerSpansAsync(3)).Should().HaveCount(3, "la observabilidad estaba activa durante estas comprobaciones");

        var source = await File.ReadAllTextAsync(Path.Combine(RepositoryRoot(), "src", "Knock.Api", "Hosting", "KnockApplicationExtensions.cs"));
        var useKnockApi = source[source.IndexOf("UseKnockApi", StringComparison.Ordinal)..];
        useKnockApi.Should().NotContainAny(["Observability", "Telemetry", "OpenTelemetry"], "no hay middleware nuevo: docs/MIDDLEWARES.md no cambia");
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Knock.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Knock.slnx no se encontró.");
    }
}
