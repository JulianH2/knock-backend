using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.DatabaseTest.Support;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.DatabaseTest.Scripts;

/// <summary>Spec 045, T-01 (§6): <c>db/045-catalogos-y-lista-de-espera.js</c> crea los índices de <c>oficios</c> y <c>oficios_pedidos</c>, es idempotente y rechaza duplicados del mismo técnico.</summary>
public sealed class Spec045CatalogScriptTests
{
    [Fact]
    public async Task Script_CreaIndicesDeFormaIdempotenteYEvitaDuplicados_R_21_CA_045_41()
    {
        var documentDb = await DocumentDbFixture.SharedAsync();
        var name = "s045_market_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        using var client = new MongoClient(documentDb.ConnectionString);
        try
        {
            await using var tools = await MongoToolsContainer.StartAsync(documentDb, new Dictionary<string, string> { ["KNOCK_DB_MARKET"] = name });
            var script = $"{MongoToolsContainer.DbDirectory}/045-catalogos-y-lista-de-espera.js";

            var first = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);

            first.ExitCode.Should().Be(0, first.Output);
            var market = client.GetDatabase(name);
            var requested = market.GetCollection<BsonDocument>("oficios_pedidos");
            await requested.InsertOneAsync(new BsonDocument { ["usuarioId"] = "tecnico-1", ["textoNormalizado"] = "plomero", ["texto"] = "Plomero" });
            var second = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);

            second.ExitCode.Should().Be(0, $"reaplicar el script no falla: {second.Output}");
            (await requested.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(1, "reaplicar no borra datos");
            (await MongoIndexes.DescribeAsync(market, "oficios_pedidos")).Should().Equal(
                "_id_ _id:1 - ttl:-",
                "textoNormalizado_1 textoNormalizado:1 - ttl:-",
                "usuarioId_1_textoNormalizado_1 usuarioId:1,textoNormalizado:1 unique ttl:-");
            (await MongoIndexes.DescribeAsync(market, "oficios")).Should().Equal("_id_ _id:1 - ttl:-", "activo_1_clave_1 activo:1,clave:1 - ttl:-");
            var sameTechnician = () => requested.InsertOneAsync(new BsonDocument { ["usuarioId"] = "tecnico-1", ["textoNormalizado"] = "plomero" });
            (await sameTechnician.Should().ThrowAsync<MongoWriteException>()).Which.WriteError.Category.Should().Be(ServerErrorCategory.DuplicateKey);
            await requested.InsertOneAsync(new BsonDocument { ["usuarioId"] = "tecnico-2", ["textoNormalizado"] = "plomero" });
            using var names = await market.ListCollectionNamesAsync();
            (await names.ToListAsync()).Should().NotContain("lista_espera", "la lista de espera sigue siendo listas_espera (spec 023)");
        }
        finally
        {
            await client.DropDatabaseAsync(name);
        }
    }

    [Theory]
    [InlineData("nombre con espacios")]
    [InlineData("a;b")]
    public async Task Script_RechazaUnNombreDeBaseNoValido_R_21(string name)
    {
        var documentDb = await DocumentDbFixture.SharedAsync();
        await using var tools = await MongoToolsContainer.StartAsync(documentDb, new Dictionary<string, string> { ["KNOCK_DB_MARKET"] = name });

        var result = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", $"{MongoToolsContainer.DbDirectory}/045-catalogos-y-lista-de-espera.js");

        result.ExitCode.Should().NotBe(0);
    }
}
