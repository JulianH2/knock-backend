using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.DatabaseTest.Support;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.DatabaseTest.Scripts;

public sealed class Spec036TrabajoScriptTests
{
    [Fact]
    public async Task Indices_De_Trabajo_Son_Idempotentes_CA_036_03()
    {
        var fixture = await DocumentDbFixture.SharedAsync();
        var name = "s036_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        using var client = new MongoClient(fixture.ConnectionString);
        try
        {
            await using var tools = await MongoToolsContainer.StartAsync(fixture, new Dictionary<string, string> { ["KNOCK_DB_MARKET"] = name });
            var script = $"{MongoToolsContainer.DbDirectory}/036-resumen-trabajo.js";
            var first = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);
            first.ExitCode.Should().Be(0, first.Output);
            var database = client.GetDatabase(name);
            await database.GetCollection<BsonDocument>("visitas").InsertOneAsync(new BsonDocument("estado", "terminada"));
            var second = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);
            second.ExitCode.Should().Be(0, second.Output);
            (await database.GetCollection<BsonDocument>("visitas").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(1);
            (await MongoIndexes.DescribeAsync(database, "visitas")).Should().Contain("tecnicoUsuarioId_1_estado_1_terminadaEn_-1__id_-1 tecnicoUsuarioId:1,estado:1,terminadaEn:-1,_id:-1 - ttl:-");
            (await MongoIndexes.DescribeAsync(database, "cotizaciones")).Should().Contain("tecnicoUsuarioId_1_creadoEn_-1 tecnicoUsuarioId:1,creadoEn:-1 - ttl:-");
        }
        finally
        {
            await client.DropDatabaseAsync(name);
        }
    }
}
