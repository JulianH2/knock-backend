using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.DatabaseTest.Support;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.DatabaseTest.Scripts;

/// <summary>
/// Spec 022, §6: <c>db/024-visitas-llegada.js</c> agrega a <c>visitas</c> los índices de vencimiento de códigos y de agenda del técnico, reaplicarlo no cambia nada ni borra
/// documentos, y convive con los índices de <c>db/022-visitas.js</c>.
/// </summary>
public sealed class Spec022VisitsArrivalScriptTests
{
    [Fact]
    public async Task ArrivalScript_AddsTheIndexesIdempotently_AndKeepsTheVisitDocuments_R_15()
    {
        var documentDb = await DocumentDbFixture.SharedAsync();
        var name = "s022_visits_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        using var client = new MongoClient(documentDb.ConnectionString);
        try
        {
            await using var tools = await MongoToolsContainer.StartAsync(documentDb, new Dictionary<string, string> { ["KNOCK_DB_MARKET"] = name });
            var first = $"{MongoToolsContainer.DbDirectory}/022-visitas.js";
            var second = $"{MongoToolsContainer.DbDirectory}/024-visitas-llegada.js";
            (await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", first)).ExitCode.Should().Be(0);

            var applied = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", second);

            applied.ExitCode.Should().Be(0, applied.Output);
            var visits = client.GetDatabase(name).GetCollection<BsonDocument>("visitas");
            await visits.InsertOneAsync(new BsonDocument { ["solicitudId"] = ObjectId.GenerateNewId(), ["estado"] = "confirmada" });
            var again = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", second);

            again.ExitCode.Should().Be(0, $"reaplicar el script no falla: {again.Output}");
            (await visits.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(1, "reaplicar no borra datos");
            (await MongoIndexes.DescribeAsync(client.GetDatabase(name), "visitas")).Should().Equal(
                "_id_ _id:1 - ttl:-",
                "clienteId_1_dia_1 clienteId:1,dia:1 - ttl:-",
                "estado_1_codigoVenceEn_1 estado:1,codigoVenceEn:1 - ttl:-",
                "estado_1_dia_1 estado:1,dia:1 - ttl:-",
                "solicitudId_1 solicitudId:1 unique ttl:-",
                "tecnicoUsuarioId_1_dia_1 tecnicoUsuarioId:1,dia:1 - ttl:-",
                "tecnicoUsuarioId_1_estado_1_dia_1 tecnicoUsuarioId:1,estado:1,dia:1 - ttl:-");
        }
        finally
        {
            await client.DropDatabaseAsync(name);
        }
    }
}
