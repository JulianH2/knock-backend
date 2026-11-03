using System.Text.Json;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.SecurityTest.Hosting;

public class SecurityConfigurationTests
{
    public static TheoryData<string, string, string> InvalidSettings => new()
    {
        { "Security:Cors:AllowedOrigins:0", "*", "Security:Cors:AllowedOrigins:0" },
        { "Security:Cors:AllowedOrigins:0", "https://panel.example.test/api", "Security:Cors:AllowedOrigins:0" },
        { "Security:Cors:AllowedOrigins:0", "http://panel.example.test", "Security:Cors:AllowedOrigins:0" },
        { "Security:Hsts:MaxAge", "00:00:00", "Security:Hsts:MaxAge" },
        { "Security:Https:RedirectPort", "0", "Security:Https:RedirectPort" },
        { "Security:Https:RedirectPort", "70000", "Security:Https:RedirectPort" },
    };

    [Theory]
    [MemberData(nameof(InvalidSettings))]
    public void InvalidSettings_FailTheStartup_TC_TT_API_06_14(string key, string value, string expectedKey)
    {
        using var factory = new KnockApiFactory(settings: new Dictionary<string, string?> { [key] = value });

        var failure = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        failure.ToString().Should().Contain(expectedKey, "el mensaje nombra la clave inválida");
        if (value.Length > 8)
        {
            failure.ToString().Should().NotContain(value, "y no repite el valor");
        }
    }

    [Fact]
    public async Task EmptySecuritySection_StartsWithCorsOff_TC_TT_API_06_14()
    {
        using var factory = new KnockApiFactory(settings: new Dictionary<string, string?> { ["Security:Cors:AllowedOrigins"] = string.Empty });
        using var client = factory.CreateClient();
        var origin = new Dictionary<string, string> { ["Origin"] = SecurityHttp.PanelOrigin };

        using var response = await TestHttp.GetAsync(client, "/api/v1/test/ping", headers: origin);

        response.IsSuccessStatusCode.Should().BeTrue();
        SecurityHttp.HeaderValues(response, "Access-Control-Allow-Origin").Should().BeEmpty();
    }

    [Theory]
    [InlineData("appsettings.json")]
    [InlineData("appsettings.Development.json")]
    public void RepositorySettings_HaveNoRealOrigins_TC_TT_API_06_15(string file)
    {
        var path = Path.Combine(FindRepositoryRoot(), "src", "Knock.Api", file);
        using var document = JsonDocument.Parse(File.ReadAllText(path));

        var origins = document.RootElement.TryGetProperty("Security", out var security)
            && security.TryGetProperty("Cors", out var cors)
            && cors.TryGetProperty("AllowedOrigins", out var list)
            ? list.EnumerateArray().Select(o => o.GetString()!).ToList()
            : [];

        origins.Should().OnlyContain(o => new Uri(o).IsLoopback, "el repositorio no trae orígenes reales, solo loopback");
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
