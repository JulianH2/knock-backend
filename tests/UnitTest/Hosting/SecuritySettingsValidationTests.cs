using AwesomeAssertions;
using Knock.Api.Hosting;
using Xunit;

namespace Knock.UnitTest.Hosting;

public class SecuritySettingsValidationTests
{
    private static SecuritySettings Valid() => new()
    {
        Hsts = new HstsSettings { MaxAge = TimeSpan.FromDays(365), IncludeSubDomains = true },
        Https = new HttpsSettings { RedirectPort = null },
        Cors = new CorsSettings { AllowedOrigins = ["https://panel.example.test"], PreflightMaxAge = TimeSpan.FromMinutes(10) },
    };

    [Fact]
    public void ValidSettings_HaveNoErrors_TC_TT_API_06_13()
    {
        SecuritySettingsValidator.Errors(Valid()).Should().BeEmpty();
    }

    [Fact]
    public void EmptyOriginList_IsValid_AndMeansCorsOff_TC_TT_API_06_13()
    {
        var settings = Valid();
        settings.Cors.AllowedOrigins = [];
        settings.Cors.PreflightMaxAge = TimeSpan.Zero;

        SecuritySettingsValidator.Errors(settings).Should().BeEmpty();
    }

    [Theory]
    [InlineData("https://panel.example.test")]
    [InlineData("https://panel.example.test:8443")]
    [InlineData("http://localhost:4200")]
    [InlineData("http://127.0.0.1:4200")]
    [InlineData("http://[::1]:4200")]
    public void ExactOrigins_AreAccepted_TC_TT_API_06_13(string origin)
    {
        SecuritySettingsValidator.IsValidOrigin(origin).Should().BeTrue();
    }

    [Theory]
    [InlineData("*")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("panel.example.test")]
    [InlineData("https://panel.example.test/")]
    [InlineData("https://panel.example.test/path")]
    [InlineData("https://panel.example.test?x=1")]
    [InlineData("https://panel.example.test#frag")]
    [InlineData("https://user:pass@panel.example.test")]
    [InlineData("http://panel.example.test")]
    [InlineData("ftp://panel.example.test")]
    [InlineData("https://*.example.test")]
    [InlineData("https://PANEL.example.test")]
    public void LooseOrigins_AreRejected_TC_TT_API_06_13(string? origin)
    {
        SecuritySettingsValidator.IsValidOrigin(origin).Should().BeFalse();
    }

    [Fact]
    public void ZeroHstsMaxAge_IsRejectedNamingTheKey_TC_TT_API_06_13()
    {
        var settings = Valid();
        settings.Hsts.MaxAge = TimeSpan.Zero;

        SecuritySettingsValidator.Errors(settings).Should().ContainSingle().Which.Should().Contain("Security:Hsts:MaxAge");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    public void RedirectPortOutOfRange_IsRejected_TC_TT_API_06_13(int port)
    {
        var settings = Valid();
        settings.Https.RedirectPort = port;

        SecuritySettingsValidator.Errors(settings).Should().ContainSingle().Which.Should().Contain("Security:Https:RedirectPort");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(443)]
    [InlineData(65535)]
    public void RedirectPortInRange_IsAccepted_TC_TT_API_06_13(int port)
    {
        var settings = Valid();
        settings.Https.RedirectPort = port;

        SecuritySettingsValidator.Errors(settings).Should().BeEmpty();
    }

    [Fact]
    public void PreflightMaxAge_IsRequiredWhenThereAreOrigins_TC_TT_API_06_13()
    {
        var settings = Valid();
        settings.Cors.PreflightMaxAge = TimeSpan.Zero;

        SecuritySettingsValidator.Errors(settings).Should().ContainSingle().Which.Should().Contain("Security:Cors:PreflightMaxAge");
    }

    [Fact]
    public void ErrorsNameTheEntryButNeverRepeatTheValue_TC_TT_API_06_13()
    {
        var settings = Valid();
        settings.Cors.AllowedOrigins = ["https://panel.example.test/secret-path"];

        var message = SecuritySettingsValidator.Errors(settings).Should().ContainSingle().Subject;

        message.Should().Contain("Security:Cors:AllowedOrigins:0").And.NotContain("secret-path");
    }
}
