using System.Text.RegularExpressions;
using AwesomeAssertions;
using Xunit;

namespace Knock.SecurityTest.Secrets;

public static partial class AppSettingsSecretsTests
{
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
}
