using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.DatabaseTest.Support;
using Knock.IntegrationTest.Support;
using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Requests.Data;
using Knock.Market.Api.Modules.Requests.Domain;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using Xunit;

namespace Knock.DatabaseTest.Scripts;

public sealed class Spec048RequestsScriptTests
{
    [Fact]
    public async Task ComplementaryIndexes_AfterExistingScripts_AreIdempotent_R_048_09()
    {
        var documentDb = await DocumentDbFixture.SharedAsync();
        var name = "s048_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        var commands = new System.Collections.Concurrent.ConcurrentQueue<BsonDocument>();
        var settings = MongoClientSettings.FromConnectionString(documentDb.ConnectionString);
        settings.ClusterConfigurator = cluster => cluster.Subscribe<CommandStartedEvent>(e =>
        {
            if (e.CommandName == "find" && e.Command.GetValue("find", "") == "cotizaciones")
                commands.Enqueue(e.Command.DeepClone().AsBsonDocument);
        });
        using var client = new MongoClient(settings);
        try
        {
            await using var tools = await MongoToolsContainer.StartAsync(documentDb, new Dictionary<string, string> { ["KNOCK_DB_MARKET"] = name });
            foreach (var script in new[] { "020-requests-solicitudes-cotizaciones.js", "022-visitas.js", "048-solicitudes-y-cotizaciones-complementos.js", "048-solicitudes-y-cotizaciones-complementos.js" })
            {
                var result = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", $"{MongoToolsContainer.DbDirectory}/{script}");
                result.ExitCode.Should().Be(0, result.Output);
            }

            var db = client.GetDatabase(name);
            var requests = await MongoIndexes.DescribeAsync(db, "solicitudes");
            var quotes = await MongoIndexes.DescribeAsync(db, "cotizaciones");
            requests.Should().Contain("clienteId_1_estado_1_creadoEn_-1 clienteId:1,estado:1,creadoEn:-1 - ttl:-");
            requests.Should().Contain("coloniaId_1_oficioClave_1_estado_1 coloniaId:1,oficioClave:1,estado:1 - ttl:-");
            quotes.Should().Contain("tecnicoUsuarioId_1_estado_1_creadoEn_-1 tecnicoUsuarioId:1,estado:1,creadoEn:-1 - ttl:-");
            requests.Concat(quotes).Should().NotContain(index => index.Contains("idempotencyKey", StringComparison.Ordinal));
            quotes.Should().Contain("solicitudId_1_tecnicoId_1 solicitudId:1,tecnicoId:1 unique ttl:-");

            var now = DateTime.UtcNow;
            var documents = Enumerable.Range(0, 30).Select(i => new BsonDocument
            {
                ["_id"] = ObjectId.GenerateNewId(),
                ["solicitudId"] = ObjectId.GenerateNewId(),
                ["tecnicoId"] = ObjectId.GenerateNewId(),
                ["tecnicoUsuarioId"] = "synthetic-subject",
                ["estado"] = "enviada",
                ["tipoPrecio"] = "fijo",
                ["precio"] = 100,
                ["tope"] = 110,
                ["dia"] = "2026-12-01",
                ["hora"] = "12:00",
                ["creadoEn"] = now.AddMinutes(-i),
            });
            await db.GetCollection<BsonDocument>("cotizaciones").InsertManyAsync(documents);
            var store = new QuoteStore(db, Options.Create(new KnockMongoOptions()), Options.Create(new QuotePricingOptions()),
                Options.Create(new OleadasOptions()), new EnviosStore(db));
            var page = await store.ListAsync("synthetic-subject", "enviada", null, 20, CancellationToken.None);
            page.Items.Should().HaveCount(20);
            page.NextCursor.Should().NotBeNullOrEmpty();
            var command = commands.Should().ContainSingle().Which;
            var excluded = new[] { "lsid", "$db", "$clusterTime", "$readPreference", "txnNumber", "apiVersion" };
            var clean = new BsonDocument(command.Elements.Where(element => !excluded.Contains(element.Name, StringComparer.Ordinal)));
            var explanation = await db.RunCommandAsync<BsonDocument>(new BsonDocument { ["explain"] = clean, ["verbosity"] = "executionStats" });
            var winning = explanation["queryPlanner"]["winningPlan"].ToJson();
            winning.Should().Contain("IXSCAN").And.Contain("tecnicoUsuarioId_1_estado_1_creadoEn_-1").And.NotContain("COLLSCAN");
        }
        finally
        {
            await client.DropDatabaseAsync(name);
        }
    }
}
