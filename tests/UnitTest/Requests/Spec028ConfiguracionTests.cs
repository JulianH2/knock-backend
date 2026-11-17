using Knock.Market.Api.Modules.Requests;
using Knock.Market.Api.Modules.Requests.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Xunit;

namespace Knock.UnitTest.Requests;

public sealed class Spec028ConfiguracionTests
{
    [Fact]
    public async Task Defaults_StartWithWavesDisabled_CA_028_16()
    {
        using var host = CreateHost([]);
        await host.StartAsync();
        var options = host.Services.GetRequiredService<IOptions<OleadasOptions>>().Value;
        Assert.False(options.Habilitado);
        Assert.Equal((5, 3, 3, 15, 2, 3, 5), (options.TamanoOleada, options.MejorOrdenados, options.UmbralNuevo, options.MinutosRevision, options.MinimoCotizaciones, options.MaximoOleadas, options.MaximoCotizaciones));
        await host.StopAsync();
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    public async Task ValidConfiguration_StartsAndBindsSwitch_CA_028_16(string enabled)
    {
        using var host = CreateHost(new Dictionary<string, string?>
        {
            ["Oleadas:Habilitado"] = enabled,
            ["Oleadas:TamanoOleada"] = "6",
            ["Oleadas:MejorOrdenados"] = "4",
            ["Oleadas:UmbralNuevo"] = "7",
            ["Oleadas:MinutosRevision"] = "8",
            ["Oleadas:MinimoCotizaciones"] = "9",
            ["Oleadas:MaximoOleadas"] = "10",
            ["Oleadas:MaximoCotizaciones"] = "11",
        });
        await host.StartAsync();
        var options = host.Services.GetRequiredService<IOptions<OleadasOptions>>().Value;
        Assert.Equal(bool.Parse(enabled), options.Habilitado);
        Assert.Equal((6, 4, 7, 8, 9, 10, 11), (options.TamanoOleada, options.MejorOrdenados, options.UmbralNuevo, options.MinutosRevision, options.MinimoCotizaciones, options.MaximoOleadas, options.MaximoCotizaciones));
        await host.StopAsync();
    }

    [Theory]
    [InlineData("TamanoOleada", "0")]
    [InlineData("MejorOrdenados", "0")]
    [InlineData("UmbralNuevo", "-1")]
    [InlineData("MinutosRevision", "0")]
    [InlineData("MinimoCotizaciones", "0")]
    [InlineData("MaximoOleadas", "0")]
    [InlineData("MaximoCotizaciones", "0")]
    [InlineData("MejorOrdenados", "4")]
    [InlineData("MejorOrdenados", "2147483647")]
    public async Task InvalidConfiguration_PreventsHostStartup_CA_028_16(string key, string value)
    {
        using var host = CreateHost(new Dictionary<string, string?> { [$"Oleadas:{key}"] = value });
        await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());
    }

    [Theory]
    [InlineData("TamanoOleada", "not-an-integer")]
    [InlineData("Habilitado", "not-a-boolean")]
    public async Task MalformedConfiguration_PreventsHostStartup_CA_028_16(string key, string value)
    {
        using var host = CreateHost(new Dictionary<string, string?> { [$"Oleadas:{key}"] = value });
        await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync());
    }

    private static IHost CreateHost(Dictionary<string, string?> values)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = Environments.Production });
        builder.Configuration.AddInMemoryCollection(values);
        builder.Services.AddSingleton<IMongoClient>(_ => throw new InvalidOperationException("MongoDB should not be accessed."));
        builder.Services.AddRequestsModule();
        return builder.Build();
    }
}
