using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.DatabaseTest.Support;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.DatabaseTest.Scripts;

/// <summary>Spec 041, §6: los scripts <c>db/041-*.js</c> crean la colección <c>mi_casa_y_auto</c> y el índice parcial de <c>solicitudes</c>; reaplicarlos no cambia nada.</summary>
public sealed class Spec041HomeAndCarScriptTests
{
    [Fact]
    public async Task HomeAndCarScript_CreatesTheCollectionAndItsIndex_Idempotently_DB_041_01()
    {
        var documentDb = await DocumentDbFixture.SharedAsync();
        var name = "s041_identity_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        using var client = new MongoClient(documentDb.ConnectionString);
        try
        {
            await using var tools = await MongoToolsContainer.StartAsync(documentDb, new Dictionary<string, string> { ["KNOCK_DB_IDENTITY"] = name });
            var script = $"{MongoToolsContainer.DbDirectory}/041-mi-casa-y-mi-auto.js";

            var first = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);

            first.ExitCode.Should().Be(0, first.Output);
            var identity = client.GetDatabase(name);
            var collection = identity.GetCollection<BsonDocument>("mi_casa_y_auto");
            (await MongoIndexes.DescribeAsync(identity, "mi_casa_y_auto")).Should().Equal(
                "_id_ _id:1 - ttl:-",
                "usuarioId_1_tipo_1 usuarioId:1,tipo:1 - ttl:-");
            await collection.InsertOneAsync(new BsonDocument { ["usuarioId"] = "sub-ficticio", ["tipo"] = "auto", ["nombre"] = "Auto de prueba", ["anio"] = 2020, ["creadoEn"] = DateTime.UtcNow });

            var second = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);

            second.ExitCode.Should().Be(0, second.Output);
            (await collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(1, "reaplicar no borra documentos");
            (await MongoIndexes.DescribeAsync(identity, "mi_casa_y_auto")).Should().HaveCount(2);
        }
        finally
        {
            await client.DropDatabaseAsync(name);
        }
    }

    [Fact]
    public async Task ElementIdScript_CreatesThePartialIndex_Idempotently_DB_041_02()
    {
        var documentDb = await DocumentDbFixture.SharedAsync();
        var name = "s041_market_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        using var client = new MongoClient(documentDb.ConnectionString);
        try
        {
            await using var tools = await MongoToolsContainer.StartAsync(documentDb, new Dictionary<string, string> { ["KNOCK_DB_MARKET"] = name });
            var script = $"{MongoToolsContainer.DbDirectory}/041-solicitudes-elemento-id.js";

            var first = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);
            var second = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);

            first.ExitCode.Should().Be(0, first.Output);
            second.ExitCode.Should().Be(0, second.Output);
            var market = client.GetDatabase(name);
            (await MongoIndexes.DescribeAsync(market, "solicitudes")).Should().Equal(
                "_id_ _id:1 - ttl:-",
                "clienteId_1_elementoId_1_estado_1 clienteId:1,elementoId:1,estado:1 - ttl:-");
            var indexes = await (await market.GetCollection<BsonDocument>("solicitudes").Indexes.ListAsync()).ToListAsync();
            indexes.Single(index => index["name"] == "clienteId_1_elementoId_1_estado_1")["partialFilterExpression"].ToJson()
                .Should().Be(new BsonDocument("elementoId", new BsonDocument("$exists", true)).ToJson());
        }
        finally
        {
            await client.DropDatabaseAsync(name);
        }
    }
}
