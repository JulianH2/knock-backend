using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.DatabaseTest.Support;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.DatabaseTest.Scripts;

/// <summary>Spec 039, T-01 (§6): <c>db/039-cuenta-y-datos-personales.js</c> crea el índice de eliminación y la colección <c>recuperaciones</c>, y repetirlo no cambia nada.</summary>
public sealed class Spec039AccountScriptTests
{
    [Fact]
    public async Task AccountScript_CreatesIndexesAndCollection_Idempotently_R_039_T01()
    {
        var documentDb = await DocumentDbFixture.SharedAsync();
        var name = "s039_identity_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        using var client = new MongoClient(documentDb.ConnectionString);
        try
        {
            await using var tools = await MongoToolsContainer.StartAsync(documentDb, new Dictionary<string, string> { ["KNOCK_DB_IDENTITY"] = name });
            var script = $"{MongoToolsContainer.DbDirectory}/039-cuenta-y-datos-personales.js";

            var first = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);
            first.ExitCode.Should().Be(0, first.Output);
            var identity = client.GetDatabase(name);
            var expectedRecoveries = new[]
            {
                "_id_ _id:1 - ttl:-",
                "actualizadoEn_1 actualizadoEn:1 - ttl:7776000",
                "estado_1_creadoEn_1 estado:1,creadoEn:1 - ttl:-",
                "usuarioId_1_estado_1 usuarioId:1,estado:1 - ttl:-",
            };
            (await MongoIndexes.DescribeAsync(identity, "recuperaciones")).Should().Equal(expectedRecoveries);
            (await MongoIndexes.DescribeAsync(identity, "usuarios")).Should().Equal("_id_ _id:1 - ttl:-", "estado_1_eliminarEn_1 estado:1,eliminarEn:1 - ttl:-");

            await identity.GetCollection<BsonDocument>("recuperaciones").InsertOneAsync(new BsonDocument { ["estado"] = "pendiente", ["creadoEn"] = DateTime.UtcNow });
            var second = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);

            second.ExitCode.Should().Be(0, second.Output);
            (await MongoIndexes.DescribeAsync(identity, "recuperaciones")).Should().Equal(expectedRecoveries);
            (await identity.GetCollection<BsonDocument>("recuperaciones").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(1);
        }
        finally
        {
            await client.DropDatabaseAsync(name);
        }
    }
}
