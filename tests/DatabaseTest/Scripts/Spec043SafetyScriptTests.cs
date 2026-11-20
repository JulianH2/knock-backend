using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.DatabaseTest.Support;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.DatabaseTest.Scripts;

public sealed class Spec043SafetyScriptTests
{
    [Fact]
    public async Task Scripts_ConservanDocumentosYEvitanBloqueosDuplicados_CA_043_37()
    {
        var fixture = await DocumentDbFixture.SharedAsync();
        var name = "s043_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        using var client = new MongoClient(fixture.ConnectionString);
        try
        {
            await using var tools = await MongoToolsContainer.StartAsync(fixture, new Dictionary<string, string> { ["KNOCK_DB_IDENTITY"] = name });
            var scripts = new[] { "043-bloqueos.js", "043-reportes.js" };
            foreach (var script in scripts)
            {
                var result = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", $"{MongoToolsContainer.DbDirectory}/{script}");
                result.ExitCode.Should().Be(0, result.Output);
            }

            var database = client.GetDatabase(name);
            var blocks = database.GetCollection<BsonDocument>("bloqueos");
            var reports = database.GetCollection<BsonDocument>("reportes");
            var block = new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "bloqueaId", "persona-a" }, { "bloqueadoId", "persona-b" } };
            var report = new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "reportaId", "persona-a" }, { "reportadoId", "persona-b" }, { "estado", "abierto" } };
            await blocks.InsertOneAsync(block);
            await reports.InsertOneAsync(report);
            var duplicate = new BsonDocument { { "bloqueaId", "persona-a" }, { "bloqueadoId", "persona-b" } };
            var act = async () => await blocks.InsertOneAsync(duplicate);
            (await act.Should().ThrowAsync<MongoWriteException>()).Which.WriteError.Category.Should().Be(ServerErrorCategory.DuplicateKey);

            var blockIndexes = await MongoIndexes.DescribeAsync(database, "bloqueos");
            var reportIndexes = await MongoIndexes.DescribeAsync(database, "reportes");
            blockIndexes.Should().Contain("bloqueaId_1_bloqueadoId_1 bloqueaId:1,bloqueadoId:1 unique ttl:-");
            blockIndexes.Should().Contain("bloqueadoId_1 bloqueadoId:1 - ttl:-");
            reportIndexes.Should().Contain("gravedad_1_estado_1_creadoEn_1 gravedad:1,estado:1,creadoEn:1 - ttl:-");
            reportIndexes.Should().Contain("reportadoId_1 reportadoId:1 - ttl:-");
            reportIndexes.Should().Contain("reportaId_1 reportaId:1 - ttl:-");

            foreach (var script in scripts)
            {
                var result = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", $"{MongoToolsContainer.DbDirectory}/{script}");
                result.ExitCode.Should().Be(0, result.Output);
            }

            (await MongoIndexes.DescribeAsync(database, "bloqueos")).Should().Equal(blockIndexes);
            (await MongoIndexes.DescribeAsync(database, "reportes")).Should().Equal(reportIndexes);
            (await blocks.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync()).Should().Equal(block);
            (await reports.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync()).Should().Equal(report);
        }
        finally
        {
            await client.DropDatabaseAsync(name);
        }
    }
}
