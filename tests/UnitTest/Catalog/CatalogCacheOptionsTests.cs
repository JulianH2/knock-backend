using AwesomeAssertions;
using Knock.Market.Api.Modules.Catalog.Data;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Knock.UnitTest.Catalog;

/// <summary>Spec 010, R-04: TTL del catálogo configurable con vigencia inicial de 24 horas y validación al arrancar.</summary>
public sealed class CatalogCacheOptionsTests
{
    [Fact]
    public void DefaultLifetimeIsAbout24Hours_CA_010_01()
    {
        new CatalogCacheOptions().Lifetime.Should().Be(TimeSpan.FromHours(24));
    }

    [Fact]
    public void ConfigurableLifetimeBindsFromConfiguration_CA_010_01()
    {
        var values = new Dictionary<string, string?> { [CatalogCacheOptions.LifetimeKey] = "3600" };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        var options = configuration.GetSection(CatalogCacheOptions.SectionName).Get<CatalogCacheOptions>();

        options.Should().NotBeNull();
        options.Lifetime.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public void ValidConfigurationIsAccepted_CA_010_01()
    {
        var result = new CatalogCacheOptionsValidator().Validate(null, new CatalogCacheOptions { CatalogLifetimeSeconds = 3600 });

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(30)]
    [InlineData(604801)]
    [InlineData(null)]
    public void InvalidConfigurationFailsNamingTheKey_CA_010_01(int? lifetimeSeconds)
    {
        var result = new CatalogCacheOptionsValidator().Validate(null, new CatalogCacheOptions { CatalogLifetimeSeconds = lifetimeSeconds });

        result.Failed.Should().BeTrue();
        string.Join(" | ", result.Failures!).Should().Contain(CatalogCacheOptions.LifetimeKey);
    }
}
