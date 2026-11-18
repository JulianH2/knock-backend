using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.DatabaseTest.Support;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.DatabaseTest.Scripts;

/// <summary>
/// Spec 040, T-01 (§6): <c>db/040-direcciones-del-cliente.js</c> crea <c>direcciones</c> en la base de Identidad con el índice normal de <c>usuarioId</c>, reaplicarlo no
/// cambia nada ni borra documentos y no toca otras colecciones.
/// </summary>
public sealed class Spec040AddressesScriptTests
{
    [Fact]
    public async Task Script_CreatesTheCollectionAndItsIndexIdempotently_AndKeepsTheDocuments_R_10()
    {
        var documentDb = await DocumentDbFixture.SharedAsync();
        var name = "s040_dir_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        using var client = new MongoClient(documentDb.ConnectionString);
        try
        {
            await using var tools = await MongoToolsContainer.StartAsync(documentDb, new Dictionary<string, string> { ["KNOCK_DB_IDENTITY"] = name });
            var script = $"{MongoToolsContainer.DbDirectory}/040-direcciones-del-cliente.js";

            var applied = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);

            applied.ExitCode.Should().Be(0, applied.Output);
            var addresses = client.GetDatabase(name).GetCollection<BsonDocument>("direcciones");
            await addresses.InsertOneAsync(new BsonDocument { ["usuarioId"] = ObjectId.GenerateNewId(), ["principal"] = true });
            var again = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);

            again.ExitCode.Should().Be(0, $"reaplicar el script no falla: {again.Output}");
            (await addresses.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(1, "reaplicar no borra datos");
            (await MongoIndexes.DescribeAsync(client.GetDatabase(name), "direcciones")).Should().Equal(
                "_id_ _id:1 - ttl:-",
                "usuarioId_1 usuarioId:1 - ttl:-");
            using var names = await client.GetDatabase(name).ListCollectionNamesAsync();
            (await names.ToListAsync()).Should().Equal("direcciones");
        }
        finally
        {
            await client.DropDatabaseAsync(name);
        }
    }

    [Theory]
    [InlineData("nombre con espacios")]
    [InlineData("a;b")]
    public async Task Script_RejectsAnInvalidDatabaseName_R_10(string name)
    {
        var documentDb = await DocumentDbFixture.SharedAsync();
        await using var tools = await MongoToolsContainer.StartAsync(documentDb, new Dictionary<string, string> { ["KNOCK_DB_IDENTITY"] = name });

        var result = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", $"{MongoToolsContainer.DbDirectory}/040-direcciones-del-cliente.js");

        result.ExitCode.Should().NotBe(0);
    }
}
