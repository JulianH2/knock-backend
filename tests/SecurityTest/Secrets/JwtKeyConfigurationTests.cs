using System.Security.Cryptography;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Knock.Api.Common.Security;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Knock.SecurityTest.Secrets;

public static partial class JwtKeyConfigurationTests
{
    [GeneratedRegex(@"signingkey|serviceaccount|private_key|client_secret|BEGIN [A-Z ]*PRIVATE KEY", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex KeyMaterial();

    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Knock.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("No se encontró Knock.slnx subiendo desde el directorio de pruebas.");
    }

    private static string Base64Key(int bytes) => Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes));

    private static Dictionary<string, string?> Key(string? value) => new() { ["Jwt:SigningKey"] = value };

    [Fact]
    public static void KeyNotInRepo_AppSettingsAndSourceHaveNoKeyMaterial_TC_TT_API_02_12()
    {
        var apiDir = Path.Combine(RepositoryRoot(), "src", "Knock.Api");
        var settings = Directory.GetFiles(apiDir, "appsettings*.json", SearchOption.TopDirectoryOnly);
        settings.Should().NotBeEmpty();
        foreach (var file in settings)
        {
            KeyMaterial().IsMatch(File.ReadAllText(file)).Should().BeFalse($"{Path.GetFileName(file)} no debe llevar la clave del JWT ni cuenta de servicio");
        }

        // Ninguna clave literal en el código fuente de la API (la clave solo llega por configuración externa).
        var sources = Directory.GetFiles(apiDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
        foreach (var file in sources)
        {
            File.ReadAllText(file).Contains("BEGIN PRIVATE KEY", StringComparison.Ordinal).Should().BeFalse(file);
        }
    }

    [Fact]
    public static void Development_WithoutKey_UsesRandomEphemeralKeyPerStart_TC_TT_API_02_12()
    {
        using var first = new KnockApiFactory("Development", withSigningKey: false);
        using var second = new KnockApiFactory("Development", withSigningKey: false);

        var firstKey = first.Services.GetRequiredService<JwtSettings>().SigningKey.Key;
        var secondKey = second.Services.GetRequiredService<JwtSettings>().SigningKey.Key;

        firstKey.Length.Should().BeGreaterThanOrEqualTo(32);
        secondKey.Length.Should().BeGreaterThanOrEqualTo(32);
        firstKey.Should().NotEqual(secondKey, "la clave efímera es distinta en cada arranque");
    }

    [Fact]
    public static void Development_WithConfiguredKey_UsesIt_TC_TT_API_02_12()
    {
        var configured = RandomNumberGenerator.GetBytes(48);
        using var factory = new KnockApiFactory("Development", withSigningKey: false, settings: Key(Convert.ToBase64String(configured)));

        factory.Services.GetRequiredService<JwtSettings>().SigningKey.Key.Should().Equal(configured);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public static void OutsideDevelopment_WithoutKey_DoesNotStart_TC_TT_API_02_12(string environment)
    {
        using var missing = new KnockApiFactory(environment, withSigningKey: false);
        using var blank = new KnockApiFactory(environment, withSigningKey: false, settings: Key("   "));

        var missingAct = () => missing.CreateClient();
        var blankAct = () => blank.CreateClient();

        missingAct.Should().Throw<InvalidOperationException>().WithMessage("*Jwt:SigningKey*");
        blankAct.Should().Throw<InvalidOperationException>().WithMessage("*Jwt:SigningKey*");
    }

    [Theory]
    [InlineData("Production", 31)]
    [InlineData("Production", 16)]
    [InlineData("Staging", 31)]
    [InlineData("Development", 31)]
    public static void ConfiguredKeyShorterThan32Bytes_DoesNotStart_TC_TT_API_02_12(string environment, int bytes)
    {
        using var factory = new KnockApiFactory(environment, withSigningKey: false, settings: Key(Base64Key(bytes)));

        var act = () => factory.CreateClient();

        act.Should().Throw<InvalidOperationException>().WithMessage("*32*");
    }

    [Fact]
    public static void ConfiguredKeyNotBase64_DoesNotStart_TC_TT_API_02_12()
    {
        using var factory = new KnockApiFactory("Production", withSigningKey: false, settings: Key("esto no es base64 !!!"));

        var act = () => factory.CreateClient();

        act.Should().Throw<InvalidOperationException>().WithMessage("*Jwt:SigningKey*");
    }

    [Theory]
    [InlineData("Production", 32)]
    [InlineData("Staging", 64)]
    public static async Task OutsideDevelopment_WithKeyOf32BytesOrMore_Starts_TC_TT_API_02_12(string environment, int bytes)
    {
        using var factory = new KnockApiFactory(environment, withSigningKey: false, settings: Key(Base64Key(bytes)));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative));

        response.IsSuccessStatusCode.Should().BeTrue();
    }
}
