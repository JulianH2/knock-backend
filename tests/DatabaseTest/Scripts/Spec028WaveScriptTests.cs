using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.DatabaseTest.Support;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.DatabaseTest.Scripts;

public sealed class Spec028WaveScriptTests
{
    [Fact]
    public async Task Indexes_AreIdempotent_AndRejectDuplicates_CA_028_05()
    {
        var fixture = await DocumentDbFixture.SharedAsync();
        var name = "s028_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        using var client = new MongoClient(fixture.ConnectionString);
        try
        {
            await using var tools = await MongoToolsContainer.StartAsync(fixture, new Dictionary<string, string> { ["KNOCK_DB_MARKET"] = name });
            var script = $"{MongoToolsContainer.DbDirectory}/028-requests-oleadas.js";
            var first = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);
            first.ExitCode.Should().Be(0, first.Output);
            var market = client.GetDatabase(name);
            var envios = market.GetCollection<BsonDocument>("envios");
            var tareas = market.GetCollection<BsonDocument>("tareas_programadas");
            var indexes = await (await tareas.Indexes.ListAsync()).ToListAsync();
            var ttl = indexes.Single(x => x["name"] == "actualizadoEn_1");
            ttl["expireAfterSeconds"].ToInt64().Should().Be(2592000);
            ttl["partialFilterExpression"].AsBsonDocument["estado"].AsString.Should().Be("hecha");
            (await MongoIndexes.DescribeAsync(market, "envios")).Should().Equal(
                "_id_ _id:1 - ttl:-",
                "solicitudId_1_estado_1 solicitudId:1,estado:1 - ttl:-",
                "solicitudId_1_tecnicoId_1 solicitudId:1,tecnicoId:1 unique ttl:-",
                "tecnicoUsuarioId_1_estado_1_enviadoEn_-1 tecnicoUsuarioId:1,estado:1,enviadoEn:-1 - ttl:-");
            (await MongoIndexes.DescribeAsync(market, "tecnicos")).Should().Contain("oficios_1_estado_1_oculto_1_disponibleHasta_1 oficios:1,estado:1,oculto:1,disponibleHasta:1 - ttl:-");
            await envios.InsertOneAsync(new BsonDocument { ["solicitudId"] = 1, ["tecnicoId"] = 2 });
            await tareas.InsertOneAsync(new BsonDocument { ["tipo"] = "test", ["claveNegocio"] = "1" });
            var duplicateSend = () => envios.InsertOneAsync(new BsonDocument { ["solicitudId"] = 1, ["tecnicoId"] = 2 });
            var duplicateTask = () => tareas.InsertOneAsync(new BsonDocument { ["tipo"] = "test", ["claveNegocio"] = "1" });
            (await duplicateSend.Should().ThrowAsync<MongoWriteException>()).Which.WriteError.Category.Should().Be(ServerErrorCategory.DuplicateKey);
            (await duplicateTask.Should().ThrowAsync<MongoWriteException>()).Which.WriteError.Category.Should().Be(ServerErrorCategory.DuplicateKey);
            var second = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);
            second.ExitCode.Should().Be(0, second.Output);
            (await envios.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(1);
            (await tareas.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(1);
        }
        finally
        {
            await client.DropDatabaseAsync(name);
        }
    }
}
