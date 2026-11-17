using AwesomeAssertions;
using Knock.Identity.Api.Modules.Usuarios.Data;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Knock.UnitTest.Identity;

/// <summary>Spec 010, R-04: TTL del perfil configurable con vigencia inicial de 60 segundos y validación al arrancar.</summary>
public sealed class ProfileCacheOptionsTests
{
    [Fact]
    public void DefaultLifetimeIs60Seconds_CA_010_01()
    {
        new ProfileCacheOptions().Lifetime.Should().Be(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void ConfigurableLifetimeBindsFromConfiguration_CA_010_01()
    {
        var values = new Dictionary<string, string?> { [ProfileCacheOptions.LifetimeKey] = "120" };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        var options = configuration.GetSection(ProfileCacheOptions.SectionName).Get<ProfileCacheOptions>();

        options.Should().NotBeNull();
        options.Lifetime.Should().Be(TimeSpan.FromSeconds(120));
    }

    [Fact]
    public void ValidConfigurationIsAccepted_CA_010_01()
    {
        var result = new ProfileCacheOptionsValidator().Validate(null, new ProfileCacheOptions { ProfileLifetimeSeconds = 120 });

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(3601)]
    [InlineData(null)]
    public void InvalidConfigurationFailsNamingTheKey_CA_010_01(int? lifetimeSeconds)
    {
        var result = new ProfileCacheOptionsValidator().Validate(null, new ProfileCacheOptions { ProfileLifetimeSeconds = lifetimeSeconds });

        result.Failed.Should().BeTrue();
        string.Join(" | ", result.Failures!).Should().Contain(ProfileCacheOptions.LifetimeKey);
    }
}
