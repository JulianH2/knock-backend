using AwesomeAssertions;
using Knock.Market.Api.Modules.Catalog.Domain;
using Xunit;

namespace Knock.UnitTest.Catalog;

public sealed class Spec027InterpretacionTests
{
    private static readonly SearchTrade[] _trades = [new("plo", "Plomería"), new("ele", "Electricidad"), new("mec", "Mecánica")];
    private static readonly SearchSynonym[] _synonyms = [new("boiler", "plo"), new("agua", "plo"), new("luz", "ele"), new("frenos", "mec")];

    [Theory]
    [InlineData("Bóiler", "boiler")]
    [InlineData(" BOILER!! ", "boiler")]
    [InlineData("El Niño, año", "el niño año")]
    public void Normaliza_Consulta_CA_027_02(string input, string expected) =>
        SearchNameNormalizer.NormalizeQuery(input).Should().Be(expected);

    [Theory]
    [InlineData("frenso", "mec")]
    [InlineData("bóiler!!", "plo")]
    [InlineData("electricidab", "ele")]
    public void Detecta_Error_Y_Acentos_CA_027_02(string query, string expected) =>
        InterpretadorDeConsulta.Interpret(SearchNameNormalizer.NormalizeQuery(query), _trades, _synonyms).Detected!.Key.Should().Be(expected);

    [Fact]
    public void Informa_Ambiguedad_CA_027_05()
    {
        var result = InterpretadorDeConsulta.Interpret("luz agua", _trades, _synonyms);
        result.State.Should().Be("ambiguo");
        result.Suggestions.Select(x => x.Key).Should().BeEquivalentTo("plo", "ele");
    }

    [Fact]
    public void Filtro_Manda_Sobre_Deteccion_CA_027_06()
    {
        var result = InterpretadorDeConsulta.Interpret("boiler", _trades, _synonyms, _trades[1]);
        result.State.Should().Be("elegido");
        result.Detected.Should().Be(_trades[1]);
    }

    [Fact]
    public void Sin_Coincidencia_No_Entendido_R_027_05() =>
        InterpretadorDeConsulta.Interpret("quiero pastel", _trades, _synonyms).State.Should().Be("no_entendido");

    [Theory]
    [InlineData("lu", "luz", false)]
    [InlineData("fuga", "fugo", true)]
    [InlineData("fuga", "fxgo", false)]
    [InlineData("calentador", "calentadxx", true)]
    [InlineData("calentador", "calentaxxx", false)]
    [InlineData("ca", "abc", false)]
    public void Respeta_Limites_De_Distancia_CA_027_02(string query, string synonym, bool expected)
    {
        var result = InterpretadorDeConsulta.Interpret(query, [_trades[0]], [new(synonym, "plo")]);
        (result.State == "detectado").Should().Be(expected);
    }

    [Fact]
    public void Coincidencia_Exacta_Prioritaria_CA_027_05()
    {
        var result = InterpretadorDeConsulta.Interpret("fuga", _trades, [new("fuga", "plo"), new("fugo", "ele")]);
        result.Detected!.Key.Should().Be("plo");
    }

    [Fact]
    public void Detecta_Par_De_Palabras_CA_027_02()
    {
        var result = InterpretadorDeConsulta.Interpret("se fue la luz", _trades, [new("se fue", "ele")]);
        result.Detected!.Key.Should().Be("ele");
    }
}
