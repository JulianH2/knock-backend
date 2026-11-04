using System.Text.RegularExpressions;
using AwesomeAssertions;
using Knock.Api.Hosting;
using Xunit;

namespace Knock.SecurityTest.Secrets;

/// <summary>
/// Sin secretos en la configuración (TT-API-01, caso 14) y sin cadena de conexión de Application Insights ni DSN de Sentry en el
/// repositorio: solo llegan por variable de entorno o Key Vault (TT-OBS-01 §4, caso 13).
/// </summary>
public static partial class AppSettingsSecretsTests
{
    private static readonly string[] _textExtensions = [".cs", ".json", ".yml", ".yaml", ".sh", ".js", ".tf", ".env", ".md", ".csproj"];

    [GeneratedRegex(@"(password|pwd|secret|apikey|api_key|accountkey|sharedaccesssignature|privatekey|signingkey)\s*=\s*[^\s;,""]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex CredentialPattern();

    [GeneratedRegex(@"""[^""]*(password|pwd|secret|apikey|accountkey|privatekey|signingkey|token)[^""]*""\s*:\s*""[^""]+""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex SensitiveKeyPattern();

    [GeneratedRegex(@"-----BEGIN [A-Z ]*PRIVATE KEY-----|eyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{5,}|AKIA[0-9A-Z]{16}", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex KnownSecretFormat();

    private static List<string> FindSecrets(string json)
    {
        var findings = new List<string>();
        findings.AddRange(CredentialPattern().Matches(json).Select(m => m.Value));
        findings.AddRange(SensitiveKeyPattern().Matches(json).Select(m => m.Value));
        findings.AddRange(KnownSecretFormat().Matches(json).Select(m => m.Value));
        return findings;
    }

    internal static string ApiProjectDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Knock.slnx")))
        {
            dir = dir.Parent;
        }

        return dir is null
            ? throw new InvalidOperationException("No se encontró Knock.slnx subiendo desde el directorio de pruebas.")
            : Path.Combine(dir.FullName, "src", "Knock.Api");
    }

    [Fact]
    public static void AppSettings_ContainNoSecrets_TC_TT_API_01_14()
    {
        var files = Directory.GetFiles(ApiProjectDirectory(), "appsettings*.json", SearchOption.TopDirectoryOnly);

        files.Should().NotBeEmpty();
        foreach (var file in files)
        {
            FindSecrets(File.ReadAllText(file)).Should().BeEmpty($"{Path.GetFileName(file)} no debe contener secretos");
        }
    }

    [Theory]
    [InlineData("{ \"ConnectionStrings\": { \"Db\": \"Host=h;Username=u;Pass" + "word=notreal123\" } }")]
    [InlineData("{ \"Jwt\": { \"SigningKey\": \"synthetic-value\" } }")]
    [InlineData("{ \"Storage\": \"AccountKey=ZmFrZQ==\" }")]
    [InlineData("{ \"x\": \"-----BEGIN PRIVATE KEY-----\" }")]
    public static void SecretDetector_WithSyntheticSecret_FindsIt_TC_TT_API_01_14(string json)
    {
        FindSecrets(json).Should().NotBeEmpty();
    }

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
        var root = Directory.GetParent(ApiProjectDirectory())!.Parent!.FullName;
        var appSettings = Directory.GetFiles(Path.Combine(root, "src"), "appsettings*.json", SearchOption.AllDirectories)
            .Where(f => !IsBuildOutput(f))
            .ToList();
        var observabilityGuide = Path.Combine(root, "docs", "OBSERVABILIDAD.md");
        // .env.example es opcional: si no existe se omite sin fallar; si existe se revisa como los demás.
        var envExample = Path.Combine(root, ".env.example");
        var files = appSettings
            .Concat(TextFiles(Path.Combine(root, "deploy")))
            .Concat(TextFiles(Path.Combine(root, "tests")))
            .Append(observabilityGuide)
            .Concat(File.Exists(envExample) ? [envExample] : [])
            .ToList();

        appSettings.Should().NotBeEmpty();
        File.Exists(observabilityGuide).Should().BeTrue("la guía de observabilidad es donde más fácil se pega una cadena por descuido");
        files.Should().HaveCountGreaterThan(appSettings.Count + 1, "también se revisan deploy/ y tests/");
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
    public static void ConnectionStringDetector_FindsRealLookingValues_IgnoresTheSyntheticOne_TC_TT_OBS_01_13(string template, bool expected)
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
