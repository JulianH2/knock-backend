using System.Text.RegularExpressions;
using AwesomeAssertions;
using Knock.Api.Hosting;
using Xunit;

namespace Knock.SecurityTest.Observability;

/// <summary>La cadena de conexión de Application Insights y el DSN de Sentry solo llegan por variable de entorno o Key Vault (TT-OBS-01 §4).</summary>
public static partial class ConnectionStringNotInRepoTests
{
    private static readonly string[] _textExtensions = [".cs", ".json", ".yml", ".yaml", ".sh", ".js", ".tf", ".env", ".md", ".csproj"];

    [GeneratedRegex(@"InstrumentationKey\s*=\s*(?<key>[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12})", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex InstrumentationKey();

    [GeneratedRegex(@"IngestionEndpoint\s*=\s*https://", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex AzureIngestionEndpoint();

    [GeneratedRegex(@"https://[0-9a-f]{32}@[A-Za-z0-9.-]*sentry[A-Za-z0-9.-]*/[0-9]+", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex SentryDsn();

    private static List<string> FindConnectionStrings(string text)
    {
        var findings = InstrumentationKey().Matches(text)
            .Where(m => !Guid.TryParse(m.Groups["key"].Value, out var key) || key != Guid.Empty)
            .Select(m => m.Value)
            .ToList();
        findings.AddRange(AzureIngestionEndpoint().Matches(text).Select(m => m.Value));
        findings.AddRange(SentryDsn().Matches(text).Select(m => m.Value));
        return findings;
    }

    [Fact]
    public static void RepositoryHasNoRealConnectionStringOrDsn_TC_TT_OBS_01_13()
    {
        var root = Directory.GetParent(Secrets.AppSettingsSecretsTests.ApiProjectDirectory())!.Parent!.FullName;
        var appSettings = Directory.GetFiles(Path.Combine(root, "src"), "appsettings*.json", SearchOption.AllDirectories)
            .Where(f => !IsBuildOutput(f))
            .ToList();
        var files = appSettings
            .Concat(TextFiles(Path.Combine(root, "deploy")))
            .Concat(TextFiles(Path.Combine(root, "tests")))
            .ToList();

        appSettings.Should().NotBeEmpty();
        files.Should().HaveCountGreaterThan(appSettings.Count, "también se revisan deploy/ y tests/");
        foreach (var file in files)
        {
            FindConnectionStrings(File.ReadAllText(file)).Should().BeEmpty($"{Path.GetRelativePath(root, file)} no debe traer una cadena de conexión ni un DSN");
        }

        foreach (var file in appSettings)
        {
            File.ReadAllText(file).Should().NotContain(ObservabilityExtensions.ConnectionStringKey, "la cadena solo se lee de la variable de entorno o de Key Vault");
        }
    }

    [Theory]
    [InlineData("InstrumentationKey={0};IngestionEndpoint=http://127.0.0.1:9/", false)]
    [InlineData("InstrumentationKey={1}", true)]
    [InlineData("IngestionEndpoint={4}://{2}.in.applicationinsights.azure.com/", true)]
    [InlineData("https://{3}@o1.ingest.sentry.io/42", true)]
    public static void Detector_FindsRealLookingValues_IgnoresTheSyntheticOne_TC_TT_OBS_01_13(string template, bool expected)
    {
        // Los valores se arman al correr la prueba para que este archivo no contenga ninguno.
        var text = string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            template,
            Guid.Empty,
            Guid.NewGuid(),
            "westus2-1",
            Guid.NewGuid().ToString("N"),
            Uri.UriSchemeHttps);

        FindConnectionStrings(text).Should().HaveCount(expected ? 1 : 0);
    }

    private static IEnumerable<string> TextFiles(string directory) =>
        Directory.Exists(directory)
            ? Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
                .Where(f => !IsBuildOutput(f) && _textExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            : [];

    private static bool IsBuildOutput(string path) =>
        path.Split(Path.DirectorySeparatorChar).Any(segment => segment is "bin" or "obj" or "TestResults");
}
