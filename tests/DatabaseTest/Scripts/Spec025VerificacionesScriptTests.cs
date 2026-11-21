using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.DatabaseTest.Support;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.DatabaseTest.Scripts;

/// <summary>
/// Spec 025, T-01 (§6): <c>db/025-verificaciones.js</c> crea <c>verificaciones</c> y <c>consentimientos</c> con sus índices de forma idempotente; el índice de la
/// CURP es único solo entre verificaciones vigentes (<c>curpVigente: true</c>, R-06 y CA-20).
/// </summary>
public sealed class Spec025VerificacionesScriptTests
{
    [Fact]
    public async Task Script_Crea_Colecciones_E_Indices_Idempotente_CA_025_20()
    {
        var documentDb = await DocumentDbFixture.SharedAsync();
        var name = "s025_verif_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        using var client = new MongoClient(documentDb.ConnectionString);
        try
        {
            await using var tools = await MongoToolsContainer.StartAsync(documentDb, new Dictionary<string, string> { ["KNOCK_DB_IDENTITY"] = name });
            var script = $"{MongoToolsContainer.DbDirectory}/025-verificaciones.js";

            var first = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);
            first.ExitCode.Should().Be(0, first.Output);
            var identity = client.GetDatabase(name);
            var verifications = identity.GetCollection<BsonDocument>("verificaciones");
            await verifications.InsertOneAsync(new BsonDocument { ["usuarioId"] = "ficticio-1", ["curpHmac"] = "h1", ["curpVigente"] = true });
            var second = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);

            second.ExitCode.Should().Be(0, $"reaplicar el script no falla: {second.Output}");
            (await verifications.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(1, "reaplicar no borra datos");
            (await MongoIndexes.DescribeAsync(identity, "verificaciones")).Should().Equal(
                "_id_ _id:1 - ttl:-",
                "curpHmac_1_vigente curpHmac:1 unique ttl:-",
                "estado_1_expiraEn_1 estado:1,expiraEn:1 - ttl:-",
                "usuarioId_1_creadoEn_-1 usuarioId:1,creadoEn:-1 - ttl:-");
            (await MongoIndexes.DescribeAsync(identity, "consentimientos")).Should().Equal(
                "_id_ _id:1 - ttl:-",
                "usuarioId_1_aceptadoEn_-1 usuarioId:1,aceptadoEn:-1 - ttl:-",
                "verificacionId_1 verificacionId:1 unique ttl:-");

            // La misma CURP puede repetirse en verificaciones no vigentes, pero no en dos vigentes.
            await verifications.InsertOneAsync(new BsonDocument { ["usuarioId"] = "ficticio-2", ["curpHmac"] = "h1", ["curpVigente"] = false });
            var duplicate = () => verifications.InsertOneAsync(new BsonDocument { ["usuarioId"] = "ficticio-3", ["curpHmac"] = "h1", ["curpVigente"] = true });
            (await duplicate.Should().ThrowAsync<MongoWriteException>()).Which.WriteError.Category.Should().Be(ServerErrorCategory.DuplicateKey);
        }
        finally
        {
            await client.DropDatabaseAsync(name);
        }
    }
}
