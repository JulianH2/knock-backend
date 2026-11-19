using AwesomeAssertions;
using Knock.Market.Api.Modules.Media.Domain;
using Knock.Market.Api.Modules.Media.Options;
using Knock.Market.Api.Modules.Media.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace Knock.UnitTest.Media;

/// <summary>Spec 032, R-13 y CA-19: <c>ImagenPublicaUrl</c> acepta solo la dirección que produce y rechaza cualquier otra.</summary>
public sealed class Spec032UrlTests
{
    private const string _base = "https://stknockdevab12.blob.core.windows.net/imagenes-publicas";
    private static readonly string _huella = new('a', 64);

    private static ImagenPublicaUrl Create() => new(Microsoft.Extensions.Options.Options.Create(new ImagenesOptions { BaseUrlPublica = _base }));

    [Theory]
    [InlineData("perfil")]
    [InlineData("trabajo")]
    public void Valida_Direccion_Publica_CA_032_19(string proposito)
    {
        var url = Create();
        var huella = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

        url.TryParse($"{_base}/{proposito}/{huella}.jpg", out var imagen).Should().BeTrue();

        imagen!.Proposito.Should().Be(PropositoImagenExtensions.Parse(proposito));
        imagen.Huella.Should().Be(huella);
        imagen.Url.AbsoluteUri.Should().Be($"{_base}/{proposito}/{huella}.jpg");
    }

    [Fact]
    public void La_Direccion_Construida_Se_Acepta_CA_032_19()
    {
        var url = Create();

        var construida = url.Build(PropositoImagen.Trabajo, _huella);

        url.TryParse(construida.Url.AbsoluteUri, out var leida).Should().BeTrue();
        leida.Should().Be(construida);
    }

    [Theory]
    [InlineData("https://otro.blob.core.windows.net/imagenes-publicas/perfil/{h}.jpg")]
    [InlineData("http://stknockdevab12.blob.core.windows.net/imagenes-publicas/perfil/{h}.jpg")]
    [InlineData("https://stknockdevab12.blob.core.windows.net/imagenes-publicas/perfil/{h}.jpg?sv=1")]
    [InlineData("https://stknockdevab12.blob.core.windows.net/imagenes-publicas/perfil/{h}.jpg#x")]
    [InlineData("https://stknockdevab12.blob.core.windows.net:443/imagenes-publicas/perfil/{h}.jpg")]
    [InlineData("https://u:p@stknockdevab12.blob.core.windows.net/imagenes-publicas/perfil/{h}.jpg")]
    [InlineData("https://stknockdevab12.blob.core.windows.net/imagenes-publicas/solicitud/{h}.jpg")]
    [InlineData("https://stknockdevab12.blob.core.windows.net/imagenes-publicas/otro/{h}.jpg")]
    [InlineData("https://stknockdevab12.blob.core.windows.net/imagenes-publicas/{h}.jpg")]
    [InlineData("https://stknockdevab12.blob.core.windows.net/imagenes-publicas/perfil/{h}.png")]
    [InlineData("https://stknockdevab12.blob.core.windows.net/imagenes-publicas/perfil/{h}")]
    [InlineData("https://stknockdevab12.blob.core.windows.net/imagenes-publicas/perfil/{h}.jpg/")]
    [InlineData("https://stknockdevab12.blob.core.windows.net/imagenes-publicas/perfil/extra/{h}.jpg")]
    [InlineData("https://stknockdevab12.blob.core.windows.net/imagenes-publicas/perfil/nombre.jpg")]
    [InlineData("https://stknockdevab12.blob.core.windows.net/imagenes-publicas/perfil/{H}.jpg")]
    [InlineData("https://stknockdevab12.blob.core.windows.net/imagenes-publicas/perfil/{h}aa.jpg")]
    [InlineData("https://stknockdevab12.blob.core.windows.net/imagenes-publicas/perfil/{g}.jpg")]
    [InlineData("https://stknockdevab12.blob.core.windows.net/imagenes-publicas/Perfil/{h}.jpg")]
    [InlineData("https://stknockdevab12.blob.core.windows.net/IMAGENES-PUBLICAS/perfil/{h}.jpg")]
    [InlineData("https://stknockdevab12.blob.core.windows.net/imagenes-cuarentena/perfil/{h}.jpg")]
    [InlineData("")]
    public void Rechaza_Direcciones_Fuera_De_La_Regla_CA_032_19(string plantilla)
    {
        var direccion = plantilla
            .Replace("{h}", _huella, StringComparison.Ordinal)
            .Replace("{H}", _huella.ToUpperInvariant(), StringComparison.Ordinal)
            .Replace("{g}", new string('g', 64), StringComparison.Ordinal);

        Create().TryParse(direccion, out var imagen).Should().BeFalse();
        imagen.Should().BeNull();
    }

    [Fact]
    public void Rechaza_Nulo_CA_032_19() => Create().TryParse(null, out _).Should().BeFalse();

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    public void Build_Con_Huella_Invalida_Falla_CA_032_19(string huella)
    {
        var construir = () => Create().Build(PropositoImagen.Perfil, huella);

        construir.Should().Throw<ArgumentException>();
    }
}
