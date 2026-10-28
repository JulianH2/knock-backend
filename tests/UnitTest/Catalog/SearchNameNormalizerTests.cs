using AwesomeAssertions;
using Knock.Api.Modules.Catalog.Domain;
using Xunit;

namespace Knock.UnitTest.Catalog;

public class SearchNameNormalizerTests
{
    [Theory]
    [InlineData("Plomería", "PLOMERIA")]
    [InlineData("  electrí  ", "ELECTRI")]
    [InlineData("MECÁNICA", "MECANICA")]
    [InlineData("El Llano 1a Sección", "EL LLANO 1A SECCION")]
    [InlineData("Peña", "PENA")]
    [InlineData("El   Llano\t2a  Sección", "EL LLANO 2A SECCION")]
    public void RemovesDiacriticsUppercasesAndCollapsesSpaces_TC_TT_BD_06_10(string input, string expected)
    {
        SearchNameNormalizer.Normalize(input).Should().Be(expected);
    }
}
