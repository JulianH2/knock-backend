using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.DatabaseTest.Support;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.DatabaseTest.Scripts;

/// <summary>
/// Spec 021, T-01 (R-10 y R-11): <c>db/022-visitas.js</c> crea <c>visitas</c> con sus índices en la base de Mercado, reaplicarlo no cambia nada ni borra documentos, y el
/// índice único por <c>solicitudId</c> impide una segunda cita para la misma solicitud.
/// </summary>
public sealed class Spec021VisitsScriptTests
{
    [Fact]
    public async Task VisitsScript_CreatesTheIndexesIdempotently_AndAllowsOnlyOneVisitPerRequest_R_11()
    {
        var documentDb = await DocumentDbFixture.SharedAsync();
        var name = "s021_visits_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        using var client = new MongoClient(documentDb.ConnectionString);
        try
        {
            await using var tools = await MongoToolsContainer.StartAsync(documentDb, new Dictionary<string, string> { ["KNOCK_DB_MARKET"] = name });
            var script = $"{MongoToolsContainer.DbDirectory}/022-visitas.js";

            var first = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);

            first.ExitCode.Should().Be(0, first.Output);
            var visits = client.GetDatabase(name).GetCollection<BsonDocument>("visitas");
            var expected = new[]
            {
                "_id_ _id:1 - ttl:-",
                "clienteId_1_dia_1 clienteId:1,dia:1 - ttl:-",
                "estado_1_dia_1 estado:1,dia:1 - ttl:-",
                "solicitudId_1 solicitudId:1 unique ttl:-",
                "tecnicoUsuarioId_1_dia_1 tecnicoUsuarioId:1,dia:1 - ttl:-",
            };
            (await MongoIndexes.DescribeAsync(client.GetDatabase(name), "visitas")).Should().Equal(expected);

            var request = ObjectId.GenerateNewId();
            await visits.InsertOneAsync(new BsonDocument { ["solicitudId"] = request, ["estado"] = "confirmada" });
            var second = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);

            second.ExitCode.Should().Be(0, $"reaplicar el script no falla: {second.Output}");
            (await visits.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(1, "reaplicar no borra datos");
            (await MongoIndexes.DescribeAsync(client.GetDatabase(name), "visitas")).Should().Equal(expected);
            var duplicate = () => visits.InsertOneAsync(new BsonDocument { ["solicitudId"] = request });
            (await duplicate.Should().ThrowAsync<MongoWriteException>()).Which.WriteError.Category.Should().Be(ServerErrorCategory.DuplicateKey);
            var other = () => visits.InsertOneAsync(new BsonDocument { ["solicitudId"] = ObjectId.GenerateNewId() });
            await other.Should().NotThrowAsync();
        }
        finally
        {
            await client.DropDatabaseAsync(name);
        }
    }
}
