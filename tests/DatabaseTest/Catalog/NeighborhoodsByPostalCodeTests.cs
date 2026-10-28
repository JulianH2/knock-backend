using AwesomeAssertions;
using Knock.Api.Modules.Catalog;
using Knock.DatabaseTest.Support;
using Xunit;

namespace Knock.DatabaseTest.Catalog;

public sealed class NeighborhoodsByPostalCodeTests
{
    private static readonly Lazy<Task<CatalogDatabase>> _shared = CatalogDatabase.SharedSeeded();

    [Fact]
    public async Task PostalCodeReturnsOnlyItsNeighborhoodsInAlphabeticalOrder_TC_TT_BD_06_08()
    {
        var cielito = await FindAsync("42803");
        var centro = await FindAsync("42800");
        var empty = await FindAsync("99999");
        var blank = () => FindAsync("  ");

        cielito.Should().Equal(
            "Bugambilias", "Del Llano", "El Cielito", "El Llano 1a Sección", "El Llano 2a Sección", "Iturbe", "La Huerta",
            "Mastrantos", "San Lorenzo", "Tierra Blanca", "Tultengo");
        centro.Should().Equal("Centro");
        cielito.Should().NotContain("Centro");
        empty.Should().BeEmpty();
        await blank.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>Misma regla de 5 dígitos ASCII que <c>NeighborhoodExistsAsync</c>: sin el valor en el mensaje y sin consultar.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("4280")]
    [InlineData("428000")]
    [InlineData("4280a")]
    [InlineData(" 42803")]
    [InlineData("""{"$ne":null}""")]
    [InlineData("４２８０３")]
    public async Task MalformedPostalCodeIsRejectedBeforeQuerying_TC_TT_BD_06_08(string postalCode)
    {
        var database = await _shared.Value;
        database.ClearCommands();

        var act = () => FindAsync(postalCode);

        var error = await act.Should().ThrowExactlyAsync<ArgumentException>();
        if (postalCode.Length > 0)
        {
            error.Which.Message.Should().NotContain(postalCode);
        }

        database.Commands.Should().BeEmpty("la entrada se valida antes de enviar cualquier comando");
    }

    private static async Task<IReadOnlyList<string>> FindAsync(string postalCode) =>
        await (await _shared.Value).InScopeAsync<ICatalogApi, IReadOnlyList<string>>(
            catalog => catalog.FindNeighborhoodsByPostalCodeAsync(postalCode, CancellationToken.None));
}
