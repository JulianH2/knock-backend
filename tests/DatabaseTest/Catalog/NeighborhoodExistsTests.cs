using AwesomeAssertions;
using Knock.Api.Modules.Catalog;
using Knock.DatabaseTest.Support;
using MongoDB.Bson;
using Xunit;

namespace Knock.DatabaseTest.Catalog;

public sealed class NeighborhoodExistsTests
{
    private const string _wellFormedId = "0123456789abcdef01234567";

    private static readonly Lazy<Task<CatalogDatabase>> _shared = CatalogDatabase.SharedSeeded();

    [Fact]
    public async Task ExistsOnlyForMatchingIdAndPostalCode_TC_TT_BD_06_20()
    {
        var centro = CatalogSeedData.Colonia("Centro")["_id"].AsObjectId.ToString();
        var mastrantos = CatalogSeedData.Colonia("Mastrantos")["_id"].AsObjectId.ToString();

        (await ExistsAsync(centro, "42800")).Should().BeTrue();
        (await ExistsAsync(mastrantos, "42803")).Should().BeTrue("también para una colonia sin polígono");
        (await ExistsAsync(ObjectId.GenerateNewId().ToString(), "42800")).Should().BeFalse();
        (await ExistsAsync(centro, "42803")).Should().BeFalse("la colonia existe, pero es de otro CP");

        var database = await _shared.Value;
        database.ClearCommands();
        await ExistsAsync(centro, "42800");
        var find = database.FindCommands.Should().ContainSingle().Which;
        find["filter"].AsBsonDocument.Names.Should().BeEquivalentTo("_id", "cp");
        find["filter"]["_id"].IsObjectId.Should().BeTrue("filtro tipado: la entrada ya es ObjectId, no texto");
        find["projection"].Should().Be(new BsonDocument("_id", 1));
    }

    [Theory]
    [InlineData("", "42800")]
    [InlineData("xyz", "42800")]
    [InlineData("123", "42800")]
    [InlineData("""{"$ne":null}""", "42800")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzz", "42800")]
    [InlineData("0123456789abcdef012345678", "42800")]
    [InlineData(_wellFormedId, "")]
    [InlineData(_wellFormedId, "4280")]
    [InlineData(_wellFormedId, "428000")]
    [InlineData(_wellFormedId, "4280a")]
    [InlineData(_wellFormedId, """{"$ne":null}""")]
    [InlineData(_wellFormedId, "４２８００")]
    public async Task MalformedInputIsRejectedBeforeQuerying_TC_TT_BD_06_20(string coloniaId, string cp)
    {
        var database = await _shared.Value;
        database.ClearCommands();

        var act = () => ExistsAsync(coloniaId, cp);

        var error = await act.Should().ThrowExactlyAsync<ArgumentException>();
        var malformed = coloniaId == _wellFormedId ? cp : coloniaId;
        if (malformed.Length > 0)
        {
            error.Which.Message.Should().NotContain(malformed);
        }

        database.Commands.Should().BeEmpty("la entrada se valida antes de enviar cualquier comando");
    }

    private static async Task<bool> ExistsAsync(string coloniaId, string cp) =>
        await (await _shared.Value).InScopeAsync<ICatalogApi, bool>(catalog => catalog.NeighborhoodExistsAsync(coloniaId, cp, CancellationToken.None));
}
