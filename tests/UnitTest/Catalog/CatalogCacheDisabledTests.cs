using AwesomeAssertions;
using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Catalog;
using Knock.Market.Api.Modules.Catalog.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Knock.UnitTest.Catalog;

/// <summary>Spec 010, R-01: sin cadena de Redis el módulo arranca con la caché deshabilitada (lecturas a MongoDB); con cadena sin contraseña sigue fallando la validación.</summary>
public sealed class CatalogCacheDisabledTests
{
    [Fact]
    public async Task MissingConnectionString_DisablesCatalogCache_CA_010_01()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>());
        using var provider = BuildProvider(configuration);
        var cache = provider.GetRequiredService<ICatalogCache>();

        cache.Should().BeOfType<DisabledCatalogCache>();
        (await cache.GetOficiosAsync(CancellationToken.None)).Should().BeNull();
        (await cache.GetOficiosGenerationAsync(CancellationToken.None)).Should().Be(-1);
        (await cache.SetOficiosIfGenerationAsync([], -1, CancellationToken.None)).Should().BeFalse();
        await cache.RemoveOficiosAsync(CancellationToken.None);
    }

    [Fact]
    public void ConnectionStringWithoutPassword_StillFailsValidation_CA_010_01()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            [KnockRedisOptions.ConnectionStringKey] = "localhost:6379,defaultDatabase=0",
        });
        using var provider = BuildProvider(configuration);

        var startup = () => provider.GetRequiredService<IOptions<KnockRedisOptions>>().Value;
        startup.Should().Throw<OptionsValidationException>()
            .WithMessage("*must include a password*");
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static ServiceProvider BuildProvider(IConfiguration configuration)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConfiguration>(configuration)
            .AddCatalogModule(configuration);
        return services.BuildServiceProvider();
    }
}
