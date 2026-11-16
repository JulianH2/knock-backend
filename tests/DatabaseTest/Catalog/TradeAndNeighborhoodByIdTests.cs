using AwesomeAssertions;
using Knock.DatabaseTest.Support;
using Knock.Market.Api.Modules.Catalog;
using MongoDB.Bson;
using Xunit;

namespace Knock.DatabaseTest.Catalog;

/// <summary>Consultas de solo lectura por clave de oficio y por id de colonia que usa Requests (spec 020, R-02).</summary>
public sealed class TradeAndNeighborhoodByIdTests
{
    private static readonly Lazy<Task<CatalogDatabase>> _shared = CatalogDatabase.SharedSeeded();

    [Fact]
    public async Task ActiveTradeReturnsNameAndModality_TC_020_R02_01()
    {
        (await FindTradeAsync("ele")).Should().Be(("Electricidad", "domicilio"));
        (await FindTradeAsync("mec"))!.Value.Modalidad.Should().Be("taller");
        (await FindTradeAsync("nope")).Should().BeNull("una clave que no existe no es un oficio");
    }

    [Fact]
    public async Task TradeQueryIsTypedAndOnlyLooksAtActiveTrades_TC_020_R02_02()
    {
        var database = await _shared.Value;
        database.ClearCommands();

        await FindTradeAsync("ele");

        var find = database.FindCommands.Should().ContainSingle().Which;
        find["filter"].AsBsonDocument.Names.Should().BeEquivalentTo("clave", "activo");
        find["filter"]["activo"].AsBoolean.Should().BeTrue("un oficio inactivo no se puede pedir");
        (await FindTradeAsync("""{"$ne":null}""")).Should().BeNull("la entrada es un texto, nunca un operador");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abcdefghijklmnopq")]
    public async Task MalformedTradeKeyIsRejectedBeforeQuerying_TC_020_R02_03(string clave)
    {
        var database = await _shared.Value;
        database.ClearCommands();

        var act = () => FindTradeAsync(clave);

        await act.Should().ThrowExactlyAsync<ArgumentException>();
        database.Commands.Should().BeEmpty("la clave se valida antes de enviar cualquier comando");
    }

    [Fact]
    public async Task NeighborhoodByIdReturnsNameAndPostalCode_TC_020_R02_04()
    {
        var centro = CatalogSeedData.Colonia("Centro");

        (await FindNeighborhoodAsync(centro["_id"].AsObjectId.ToString())).Should().Be(("Centro", "42800"));
        (await FindNeighborhoodAsync(ObjectId.GenerateNewId().ToString())).Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("xyz")]
    [InlineData("""{"$ne":null}""")]
    [InlineData("0123456789abcdef012345678")]
    public async Task MalformedNeighborhoodIdIsRejectedBeforeQuerying_TC_020_R02_05(string coloniaId)
    {
        var database = await _shared.Value;
        database.ClearCommands();

        var act = () => FindNeighborhoodAsync(coloniaId);

        var error = await act.Should().ThrowExactlyAsync<ArgumentException>();
        if (coloniaId.Length > 0)
        {
            error.Which.Message.Should().NotContain(coloniaId);
        }

        database.Commands.Should().BeEmpty("la entrada se valida antes de enviar cualquier comando");
    }

    private static async Task<(string Nombre, string Modalidad)?> FindTradeAsync(string clave) =>
        await (await _shared.Value).InScopeAsync<ICatalogApi, (string Nombre, string Modalidad)?>(
            catalog => catalog.FindActiveTradeAsync(clave, CancellationToken.None));

    private static async Task<(string Nombre, string Cp)?> FindNeighborhoodAsync(string coloniaId) =>
        await (await _shared.Value).InScopeAsync<ICatalogApi, (string Nombre, string Cp)?>(
            catalog => catalog.FindNeighborhoodAsync(coloniaId, CancellationToken.None));
}
