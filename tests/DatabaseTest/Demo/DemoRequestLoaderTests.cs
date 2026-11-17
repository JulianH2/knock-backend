using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Knock.IntegrationTest.Support;
using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Requests.Data;
using Knock.Market.Api.Modules.Requests.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.DatabaseTest.Demo;

/// <summary>KNOCK-246, spec 020 T-13: la carga de solicitudes y cotizaciones de demo es idempotente, atómica, solo de demo y legible por el servicio (MongoDB real).</summary>
public sealed class DemoRequestLoaderTests
{
    private static readonly string _requests = Path.Combine(CatalogTestDatabase.SeedsDirectory, "demo", "market", "solicitudes.json");
    private static readonly string _quotes = Path.Combine(CatalogTestDatabase.SeedsDirectory, "demo", "market", "cotizaciones.json");

    [Fact]
    public async Task Demo_Twice_IsIdempotent_AndReadableByTheService()
    {
        await WithDatabase(async (database, loader, options) =>
        {
            await loader.SeedAsync(_requests, _quotes, CancellationToken.None);
            var requests = database.GetCollection<BsonDocument>("solicitudes");
            var quotes = database.GetCollection<BsonDocument>("cotizaciones");
            var before = await requests.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            await loader.SeedAsync(_requests, _quotes, CancellationToken.None);
            var after = await requests.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();

            Assert.Equal(2, after.Count);
            Assert.Equal(3, await quotes.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            Assert.Equal(before.OrderBy(x => x["_id"]), after.OrderBy(x => x["_id"]));
            var store = new RequestsStore(database, options, new MongoOutbox(database));
            foreach (var document in after)
            {
                Assert.True(document["venceEn"].IsBsonDateTime, "las fechas se guardan como fecha, no como texto");
                var record = await store.FindAsync(document["_id"].AsObjectId, CancellationToken.None);
                Assert.NotNull(record);
                Assert.Equal("cotizando", RequestStatesText(record));
            }

            var live = await store.CountLiveQuotesAsync([.. after.Select(x => x["_id"].AsObjectId)], CancellationToken.None);
            Assert.Equal(2, live.Values.Max());
            Assert.Equal(3, live.Values.Sum());
        });
    }

    [Fact]
    public async Task Demo_InvalidLastQuote_WritesNothingInEitherCollection()
    {
        await WithDatabase(async (database, loader, _) =>
        {
            var input = JsonNode.Parse(await File.ReadAllTextAsync(_quotes))!.AsArray();
            input[^1]!["tope"] = 0;
            var file = Path.GetTempFileName();
            try
            {
                await File.WriteAllTextAsync(file, input.ToJsonString());
                await Assert.ThrowsAnyAsync<Exception>(() => loader.SeedAsync(_requests, file, CancellationToken.None));
                Assert.Equal(0, await database.GetCollection<BsonDocument>("solicitudes").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
                Assert.Equal(0, await database.GetCollection<BsonDocument>("cotizaciones").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            }
            finally
            {
                File.Delete(file);
            }
        });
    }

    [Theory]
    [InlineData("solicitudes")]
    [InlineData("cotizaciones")]
    public async Task Demo_NonDemoDocument_WritesNothing(string collectionName)
    {
        await WithDatabase(async (database, loader, _) =>
        {
            var collection = database.GetCollection<BsonDocument>(collectionName);
            var original = new BsonDocument { ["_id"] = ObjectId.GenerateNewId(), ["demo"] = false };
            await collection.InsertOneAsync(original);

            await Assert.ThrowsAsync<InvalidOperationException>(() => loader.SeedAsync(_requests, _quotes, CancellationToken.None));

            Assert.Equal(original, Assert.Single(await collection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync()));
            var other = collectionName == "solicitudes" ? "cotizaciones" : "solicitudes";
            Assert.Equal(0, await database.GetCollection<BsonDocument>(other).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        });
    }

    [Theory]
    [InlineData(false, "Development")]
    [InlineData(true, "Production")]
    public async Task Demo_DisabledOrProduction_WritesNothing(bool enabled, string environment)
    {
        await WithDatabase(async (database, loader, _) =>
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => loader.SeedAsync(_requests, _quotes, CancellationToken.None));
            Assert.Equal(0, await database.GetCollection<BsonDocument>("solicitudes").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            Assert.Equal(0, await database.GetCollection<BsonDocument>("cotizaciones").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        }, enabled, environment);
    }

    private static string RequestStatesText(RequestRecord record) => Knock.Market.Api.Modules.Requests.Domain.RequestStates.ToStored(record.Estado);

    private static async Task WithDatabase(Func<IMongoDatabase, DemoRequestSeeder, IOptions<KnockMongoOptions>, Task> test, bool enabled = true, string environment = "Development")
    {
        var fixture = await DocumentDbFixture.SharedAsync();
        using var client = new MongoClient(fixture.ConnectionString);
        var name = "s246r_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(8));
        try
        {
            var database = client.GetDatabase(name);
            var options = Options.Create(new KnockMongoOptions { ConnectionString = fixture.ConnectionString });
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Seed:Demo"] = enabled.ToString() }).Build();
            var loader = new DemoRequestSeeder(new DemoRequestStore(database, options), config, new FakeEnvironment(environment));
            await test(database, loader, options);
        }
        finally
        {
            await client.DropDatabaseAsync(name);
        }
    }

    private sealed class FakeEnvironment(string name) : IHostEnvironment
    {
        public string ApplicationName { get; set; } = "knock-demo-tests";

        public string EnvironmentName { get; set; } = name;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    }
}
