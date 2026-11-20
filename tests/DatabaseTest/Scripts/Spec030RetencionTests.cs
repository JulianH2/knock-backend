using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.DatabaseTest.Support;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.DatabaseTest.Scripts;

/// <summary>
/// Spec 030, T-02 (§6): <c>db/030-conversaciones.js</c> crea <c>conversaciones</c> y <c>mensajes</c> en la base de Mensajería con sus índices (par único y TTL de 180 días),
/// reaplicarlo no cambia nada ni borra documentos y no toca otras colecciones.
/// </summary>
public sealed class Spec030RetencionTests
{
    private const string _scriptName = "030-conversaciones.js";

    [Fact]
    public async Task Ttl_De_180_Dias_CA_030_19()
    {
        var documentDb = await DocumentDbFixture.SharedAsync();
        var name = "s030_msg_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        using var client = new MongoClient(documentDb.ConnectionString);
        try
        {
            await using var tools = await MongoToolsContainer.StartAsync(documentDb, new Dictionary<string, string> { ["KNOCK_DB_MESSAGING"] = name });
            var script = $"{MongoToolsContainer.DbDirectory}/{_scriptName}";

            var applied = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);

            applied.ExitCode.Should().Be(0, applied.Output);
            var database = client.GetDatabase(name);
            var conversations = database.GetCollection<BsonDocument>("conversaciones");
            var messages = database.GetCollection<BsonDocument>("mensajes");
            await conversations.InsertOneAsync(new BsonDocument { ["solicitudId"] = "s1", ["tecnicoId"] = "t1" });
            await messages.InsertOneAsync(new BsonDocument { ["conversacionId"] = ObjectId.GenerateNewId(), ["creadoEn"] = DateTime.UtcNow });
            var again = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);

            again.ExitCode.Should().Be(0, $"reaplicar el script no falla: {again.Output}");
            (await conversations.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(1, "reaplicar no borra datos");
            (await messages.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(1, "reaplicar no borra datos");
            (await MongoIndexes.DescribeAsync(database, "conversaciones")).Should().Equal(
                "_id_ _id:1 - ttl:-",
                "clienteId_1_ultimoMensajeEn_-1__id_-1 clienteId:1,ultimoMensajeEn:-1,_id:-1 - ttl:-",
                "solicitudId_1_tecnicoId_1 solicitudId:1,tecnicoId:1 unique ttl:-",
                "tecnicoUsuarioId_1_ultimoMensajeEn_-1__id_-1 tecnicoUsuarioId:1,ultimoMensajeEn:-1,_id:-1 - ttl:-");
            (await MongoIndexes.DescribeAsync(database, "mensajes")).Should().Equal(
                "_id_ _id:1 - ttl:-",
                "conversacionId_1_creadoEn_-1__id_-1 conversacionId:1,creadoEn:-1,_id:-1 - ttl:-",
                "creadoEn_1 creadoEn:1 - ttl:15552000");
            using var names = await database.ListCollectionNamesAsync();
            (await names.ToListAsync()).Order(StringComparer.Ordinal).Should().Equal("conversaciones", "mensajes");
        }
        finally
        {
            await client.DropDatabaseAsync(name);
        }
    }

    [Fact]
    public async Task Script_RejectsASecondConversationForTheSamePair_R_01()
    {
        var documentDb = await DocumentDbFixture.SharedAsync();
        var name = "s030_par_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        using var client = new MongoClient(documentDb.ConnectionString);
        try
        {
            await using var tools = await MongoToolsContainer.StartAsync(documentDb, new Dictionary<string, string> { ["KNOCK_DB_MESSAGING"] = name });
            (await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", $"{MongoToolsContainer.DbDirectory}/{_scriptName}")).ExitCode.Should().Be(0);
            var conversations = client.GetDatabase(name).GetCollection<BsonDocument>("conversaciones");
            await conversations.InsertOneAsync(new BsonDocument { ["solicitudId"] = "s1", ["tecnicoId"] = "t1" });

            var duplicate = async () => await conversations.InsertOneAsync(new BsonDocument { ["solicitudId"] = "s1", ["tecnicoId"] = "t1" });

            (await duplicate.Should().ThrowAsync<MongoWriteException>()).Which.WriteError.Category.Should().Be(ServerErrorCategory.DuplicateKey);
            await conversations.InsertOneAsync(new BsonDocument { ["solicitudId"] = "s1", ["tecnicoId"] = "t2" });
        }
        finally
        {
            await client.DropDatabaseAsync(name);
        }
    }

    [Theory]
    [InlineData("nombre con espacios")]
    [InlineData("a;b")]
    public async Task Script_RejectsAnInvalidDatabaseName_R_01(string name)
    {
        var documentDb = await DocumentDbFixture.SharedAsync();
        await using var tools = await MongoToolsContainer.StartAsync(documentDb, new Dictionary<string, string> { ["KNOCK_DB_MESSAGING"] = name });

        var result = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", $"{MongoToolsContainer.DbDirectory}/{_scriptName}");

        result.ExitCode.Should().NotBe(0);
    }
}
