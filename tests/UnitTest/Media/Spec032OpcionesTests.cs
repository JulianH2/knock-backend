using AwesomeAssertions;
using Knock.Market.Api.Modules.Media.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Knock.UnitTest.Media;

/// <summary>Spec 032, R-03 y CA-24: <c>Imagenes:BaseUrlPublica</c> se valida al arrancar y el mensaje nombra la llave sin su valor.</summary>
public sealed class Spec032OpcionesTests
{
    private const string Valida = "https://stknockdevab12.blob.core.windows.net/imagenes-publicas";

    [Fact]
    public void Base_Url_Valida_Arranca_CA_032_24()
    {
        using var provider = Build(Valida);

        provider.GetRequiredService<IOptions<ImagenesOptions>>().Value.BaseUrlPublica.Should().Be(Valida);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("http://stknockdevab12.blob.core.windows.net/imagenes-publicas")]
    [InlineData("https://stknockdevab12.blob.core.windows.net/imagenes-publicas?sv=secreto")]
    [InlineData("https://stknockdevab12.blob.core.windows.net/imagenes-publicas#parte")]
    [InlineData("https://stknockdevab12.blob.core.windows.net:8443/imagenes-publicas")]
    [InlineData("https://stknockdevab12.blob.core.windows.net:443/imagenes-publicas")]
    [InlineData("https://usuario:clave@stknockdevab12.blob.core.windows.net/imagenes-publicas")]
    [InlineData("https://stknockdevab12.blob.core.windows.net")]
    [InlineData("https://stknockdevab12.blob.core.windows.net/")]
    [InlineData("https://stknockdevab12.blob.core.windows.net/otro-contenedor")]
    [InlineData("https://stknockdevab12.blob.core.windows.net/imagenes-publicas/")]
    [InlineData("https://stknockdevab12.blob.core.windows.net/imagenes-publicas/perfil")]
    [InlineData("valor-ajeno-sin-esquema")]
    [InlineData(" https://stknockdevab12.blob.core.windows.net/imagenes-publicas")]
    public void Base_Url_Invalida_No_Arranca_CA_032_24(string? valor)
    {
        using var provider = Build(valor);

        var arranque = () => provider.GetRequiredService<IOptions<ImagenesOptions>>().Value;

        var mensaje = arranque.Should().Throw<OptionsValidationException>().Which.Message;
        mensaje.Should().Contain("Imagenes:BaseUrlPublica");
        if (!string.IsNullOrWhiteSpace(valor))
        {
            mensaje.Should().NotContain(valor.Trim());
        }
    }

    [Fact]
    public void Sin_Seccion_No_Arranca_Sin_Valor_Por_Omision_CA_032_24()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddImagenesPublicas();
        using var provider = services.BuildServiceProvider();

        var arranque = () => provider.GetRequiredService<IOptions<ImagenesOptions>>().Value;

        arranque.Should().Throw<OptionsValidationException>().Which.Message.Should().Contain("Imagenes:BaseUrlPublica");
    }

    private static ServiceProvider Build(string? baseUrl)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Imagenes:BaseUrlPublica"] = baseUrl })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddImagenesPublicas();
        return services.BuildServiceProvider();
    }
}
