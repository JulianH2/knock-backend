using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.DatabaseTest.Support;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.DatabaseTest.Scripts;

public sealed class Spec023WaitlistScriptTests
{
    [Fact]
    public async Task WaitlistScript_CreatesIndexes_Idempotently_R_023_08()
    {
        var documentDb = await DocumentDbFixture.SharedAsync();
        var name = "s023_market_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        using var client = new MongoClient(documentDb.ConnectionString);
        try
        {
            await using var tools = await MongoToolsContainer.StartAsync(documentDb, new Dictionary<string, string> { ["KNOCK_DB_MARKET"] = name });
            var script = $"{MongoToolsContainer.DbDirectory}/023-catalog-listas-espera.js";
            var first = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);
            first.ExitCode.Should().Be(0, first.Output);
            var market = client.GetDatabase(name);
            var collection = market.GetCollection<BsonDocument>("listas_espera");
            (await MongoIndexes.DescribeAsync(market, "listas_espera")).Should().Equal(
                "_id_ _id:1 - ttl:-",
                "cp_1_creadoEn_-1 cp:1,creadoEn:-1 - ttl:-",
                "usuarioId_1_cp_1 usuarioId:1,cp:1 unique ttl:-");
            await collection.InsertOneAsync(new BsonDocument { ["usuarioId"] = "user-1", ["cp"] = "91000", ["creadoEn"] = DateTime.UtcNow });
            var second = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);
            second.ExitCode.Should().Be(0, second.Output);
            var document = await collection.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
            document.Names.Should().BeEquivalentTo("_id", "usuarioId", "cp", "creadoEn");
            (await MongoIndexes.DescribeAsync(market, "listas_espera")).Should().HaveCount(3);
            var duplicate = () => collection.InsertOneAsync(new BsonDocument { ["usuarioId"] = "user-1", ["cp"] = "91000" });
            (await duplicate.Should().ThrowAsync<MongoWriteException>()).Which.WriteError.Category.Should().Be(ServerErrorCategory.DuplicateKey);
        }
        finally
        {
            await client.DropDatabaseAsync(name);
        }
    }
}
