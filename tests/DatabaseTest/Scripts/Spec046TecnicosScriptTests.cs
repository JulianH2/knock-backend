using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.DatabaseTest.Support;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.DatabaseTest.Scripts;

/// <summary>Spec 046, §6 y CA-69: un perfil de técnico por cuenta. El script 021 deja único el índice de <c>usuarioId</c>, también sobre una base que ya tenía el anterior.</summary>
public sealed class Spec046TecnicosScriptTests
{
    [Fact]
    public async Task Indice_De_Usuario_Pasa_A_Unico_Y_Conserva_Los_Perfiles_CA_046_69()
    {
        var documentDb = await DocumentDbFixture.SharedAsync();
        var name = "s046_tech_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        using var client = new MongoClient(documentDb.ConnectionString);
        try
        {
            var market = client.GetDatabase(name);
            var technicians = market.GetCollection<BsonDocument>("tecnicos");
            await technicians.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("usuarioId"), new CreateIndexOptions { Name = "usuarioId_1" }));
            await technicians.InsertOneAsync(new BsonDocument { ["usuarioId"] = "user-1", ["estado"] = "activo" });

            await using var tools = await MongoToolsContainer.StartAsync(documentDb, new Dictionary<string, string> { ["KNOCK_DB_MARKET"] = name });
            var script = $"{MongoToolsContainer.DbDirectory}/021-tecnicos-usuario-index.js";
            var first = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);
            var second = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);

            first.ExitCode.Should().Be(0, first.Output);
            second.ExitCode.Should().Be(0, second.Output);
            (await MongoIndexes.DescribeAsync(market, "tecnicos")).Should().Contain("usuarioId_1 usuarioId:1 unique ttl:-");
            (await technicians.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(1);
            var duplicate = () => technicians.InsertOneAsync(new BsonDocument { ["usuarioId"] = "user-1", ["estado"] = "activo" });
            (await duplicate.Should().ThrowAsync<MongoWriteException>()).Which.WriteError.Category.Should().Be(ServerErrorCategory.DuplicateKey);
        }
        finally
        {
            await client.DropDatabaseAsync(name);
        }
    }
}
