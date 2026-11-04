using System.Text.RegularExpressions;
using AwesomeAssertions;
using Xunit;

namespace Knock.IntegrationTest.Hosting;

public class SecurityPipelineOrderTests
{
    private static readonly TimeSpan _regexTimeout = TimeSpan.FromSeconds(2);

    /// <summary>El orden de la constitución §4 con la llamada que lo implementa en <c>UseKnockApi</c>.</summary>
    private static readonly string[] _constitutionOrder =
    [
        "UseForwardedHeaders",
        "CorrelationIdMiddleware",
        "SecurityHeadersMiddleware",
        "UseExceptionHandler",
        "UseSerilogRequestLogging",
        "UseHsts",
        "UseHttpsRedirection",
        "CorsMiddleware",
        "UseRateLimiter",
        "RequestBodyLimitMiddleware",
        "UseAuthentication",
        "UseAuthorization",
    ];

    [Fact]
    public void SecurityLayers_FollowTheConstitutionOrder_TC_TT_API_06_17()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root, "src", "Knock.Api", "Hosting", "KnockApplicationExtensions.cs"));
        var useKnockApi = source[source.IndexOf("UseKnockApi", StringComparison.Ordinal)..];
        var actual = Regex.Matches(useKnockApi, @"app\.(?:UseMiddleware<(\w+)>|(\w+))\(", RegexOptions.None, _regexTimeout)
            .Select(m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value)
            .ToList();

        // AuthNoStoreMiddleware (HU-001) no está en la constitución §4: solo fija `Cache-Control` de /auth/* y se escribe al iniciar la respuesta,
        // así que se comprueba aparte y no altera el orden de las capas de la constitución.
        var layers = actual.Where(name => name != "AuthNoStoreMiddleware").ToList();
        layers.Take(_constitutionOrder.Length).Should().Equal(_constitutionOrder, "el pipeline sigue el orden fijo de la constitución §4");
        layers.Skip(_constitutionOrder.Length).Should().StartWith("MapKnockHealth", "los endpoints van después de la autorización");
        actual.IndexOf("AuthNoStoreMiddleware").Should().BeInRange(actual.IndexOf("CorrelationIdMiddleware"), actual.IndexOf("UseRateLimiter") - 1, "cubre también el 429 y el 413");

        var documented = File.ReadAllLines(Path.Combine(root, "docs", "MIDDLEWARES.md"))
            .Select(line => Regex.Match(line, @"^\d+\.\s+`(\w+)`", RegexOptions.None, _regexTimeout))
            .Where(m => m.Success)
            .Select(m => m.Groups[1].Value)
            .ToList();
        documented.Should().Equal(actual, "docs/MIDDLEWARES.md es el mismo orden que el código");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Knock.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Knock.slnx no se encontró subiendo desde el directorio de pruebas.");
    }
}
