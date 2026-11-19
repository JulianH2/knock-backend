using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.DatabaseTest.Support;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.DatabaseTest.Scripts;

/// <summary>
/// Spec 032, T-03 (§6): <c>db/044-market-subidas-imagen.js</c> crea <c>subidas_imagen</c> y <c>accesos_privados</c> con sus índices y TTL, reaplicarlo no cambia nada
/// ni borra documentos y no toca otras colecciones.
/// </summary>
public sealed class Spec032ImagesScriptTests
{
    private const string Script = "044-market-subidas-imagen.js";

    [Fact]
    public async Task Script_CreatesCollectionsIndexesAndTtlIdempotently_AndKeepsTheDocuments_R_19()
    {
        var documentDb = await DocumentDbFixture.SharedAsync();
        var name = "s032_img_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        using var client = new MongoClient(documentDb.ConnectionString);
        try
        {
            await using var tools = await MongoToolsContainer.StartAsync(documentDb, new Dictionary<string, string> { ["KNOCK_DB_MARKET"] = name });
            var script = $"{MongoToolsContainer.DbDirectory}/{Script}";

            var applied = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);

            applied.ExitCode.Should().Be(0, applied.Output);
            var database = client.GetDatabase(name);
            await database.GetCollection<BsonDocument>("subidas_imagen").InsertOneAsync(new BsonDocument { ["estado"] = "publicada" });
            var again = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);

            again.ExitCode.Should().Be(0, $"reaplicar el script no falla: {again.Output}");
            (await database.GetCollection<BsonDocument>("subidas_imagen").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(1, "reaplicar no borra datos");
            (await MongoIndexes.DescribeAsync(database, "subidas_imagen")).Should().Equal(
                "_id_ _id:1 - ttl:-",
                "expiraEn_1 expiraEn:1 - ttl:0",
                "sub_1_estado_1_confirmarAntesDe_1 sub:1,estado:1,confirmarAntesDe:1 - ttl:-");
            (await MongoIndexes.DescribeAsync(database, "accesos_privados")).Should().Equal(
                "_id_ _id:1 - ttl:-",
                "emitidoEn_1 emitidoEn:1 - ttl:15552000");
            using var names = await database.ListCollectionNamesAsync();
            (await names.ToListAsync()).Order(StringComparer.Ordinal).Should().Equal("accesos_privados", "subidas_imagen");
        }
        finally
        {
            await client.DropDatabaseAsync(name);
        }
    }

    [Theory]
    [InlineData("nombre con espacios")]
    [InlineData("a;b")]
    public async Task Script_RejectsAnInvalidDatabaseName_R_19(string name)
    {
        var documentDb = await DocumentDbFixture.SharedAsync();
        await using var tools = await MongoToolsContainer.StartAsync(documentDb, new Dictionary<string, string> { ["KNOCK_DB_MARKET"] = name });

        var result = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", $"{MongoToolsContainer.DbDirectory}/{Script}");

        result.ExitCode.Should().NotBe(0);
    }
}
