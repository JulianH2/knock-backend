using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Knock.IntegrationTest.Support;
using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Requests.Data;
using Knock.Market.Api.Modules.Requests.Domain;
using Knock.Market.Api.Modules.Requests.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.DatabaseTest.Demo;

public sealed class DemoTechnicianLoaderTests
{
    private static readonly string _path = Path.Combine(CatalogTestDatabase.SeedsDirectory, "demo", "market", "tecnicos.json");

    [Fact]
    public async Task Demo_Twice_IsIdempotent_AndEligible()
    {
        await WithDatabase(async (database, loader, options) =>
        {
            await loader.SeedAsync(_path, CancellationToken.None);
            var collection = database.GetCollection<BsonDocument>("tecnicos");
            var before = await collection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            await loader.SeedAsync(_path, CancellationToken.None);
            var after = await collection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            Assert.Equal(3, after.Count);
            Assert.Equal(before.OrderBy(x => x["_id"]), after.OrderBy(x => x["_id"]));
            var directory = new MongoTechnicianDirectory(database, options);
            await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Geo2DSphere("ubicacionBase")));
            foreach (var technician in after)
            {
                var request = new QuoteRequestSnapshot("ele", technician["coloniaBaseId"].AsString, RequestState.Cotizando,
                    DateTimeOffset.UtcNow.AddDays(1), DateOnly.FromDateTime(DateTime.UtcNow), 20.0499735, -99.344394146);
                Assert.NotNull(await directory.FindEligibleAsync(technician["usuarioId"].AsString, request, CancellationToken.None));
            }
        });
    }

    [Fact]
    public async Task Demo_WriteFailure_RollsBackTransaction()
    {
        await WithDatabase(async (database, loader, _) =>
        {
            var collection = database.GetCollection<BsonDocument>("tecnicos");
            await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("usuarioId"), new CreateIndexOptions { Unique = true }));
            var existing = new BsonDocument { ["_id"] = ObjectId.GenerateNewId(), ["demo"] = true, ["usuarioId"] = "7b484abb-e9e0-8104-a08d-d30ce15e8148" };
            await collection.InsertOneAsync(existing);
            await Assert.ThrowsAnyAsync<MongoException>(() => loader.SeedAsync(_path, CancellationToken.None));
            Assert.Equal(existing, Assert.Single(await collection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync()));
        });
    }

    [Fact]
    public async Task Demo_InvalidLastEntry_WritesNothing()
    {
        await WithDatabase(async (database, loader, _) =>
        {
            var input = JsonNode.Parse(await File.ReadAllTextAsync(_path))!.AsArray();
            input[^1]!["radioAtencionKm"] = 0;
            var file = Path.GetTempFileName();
            try
            {
                await File.WriteAllTextAsync(file, input.ToJsonString());
                await Assert.ThrowsAnyAsync<Exception>(() => loader.SeedAsync(file, CancellationToken.None));
                Assert.Equal(0, await database.GetCollection<BsonDocument>("tecnicos").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            }
            finally { File.Delete(file); }
        });
    }

    [Fact]
    public async Task Demo_NonDemoConflict_WritesNothing()
    {
        await WithDatabase(async (database, loader, _) =>
        {
            var collection = database.GetCollection<BsonDocument>("tecnicos");
            var original = new BsonDocument { ["_id"] = ObjectId.Parse("ea0000000000000000000003"), ["demo"] = false };
            await collection.InsertOneAsync(original);
            await Assert.ThrowsAsync<InvalidOperationException>(() => loader.SeedAsync(_path, CancellationToken.None));
            Assert.Equal(original, Assert.Single(await collection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync()));
        });
    }

    [Theory]
    [InlineData(false, "Development")]
    [InlineData(true, "Production")]
    public async Task Demo_DisabledOrProduction_WritesNothing(bool enabled, string environment)
    {
        await WithDatabase(async (database, loader, _) =>
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => loader.SeedAsync(_path, CancellationToken.None));
            Assert.Equal(0, await database.GetCollection<BsonDocument>("tecnicos").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        }, enabled, environment);
    }

    [Theory]
    [InlineData("mongodb://remote.invalid", false)]
    [InlineData("mongodb://localhost,remote.invalid", false)]
    [InlineData("mongodb+srv://localhost", false)]
    [InlineData("mongodb://localhost.remote.invalid", false)]
    [InlineData("mongodb://localhost", true)]
    [InlineData("mongodb://127.0.0.1", true)]
    [InlineData("mongodb://documentdb-local", true)]
    [InlineData("localhost", true)]
    public async Task Demo_GuardMatchesIdentity(string connection, bool allowed)
    {
        if (connection == "localhost") { connection = new MongoUrlBuilder { Server = new MongoServerAddress(connection), Username = "fake", Password = "example" }.ToString(); }
        await WithDatabase(async (database, _, _) =>
        {
            var collection = database.GetCollection<BsonDocument>("tecnicos");
            if (allowed) await DemoTechnicianGuard.ValidateAsync(connection, collection, CancellationToken.None);
            else
            {
                var error = await Assert.ThrowsAsync<InvalidOperationException>(() => DemoTechnicianGuard.ValidateAsync(connection, collection, CancellationToken.None));
                Assert.DoesNotContain(connection, error.ToString(), StringComparison.Ordinal);
            }
        });
    }

    private static async Task WithDatabase(Func<IMongoDatabase, DemoTechnicianSeeder, IOptions<KnockMongoOptions>, Task> test, bool enabled = true, string environment = "Development")
    {
        var fixture = await DocumentDbFixture.SharedAsync();
        using var client = new MongoClient(fixture.ConnectionString);
        var name = "s246_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(8));
        try
        {
            var database = client.GetDatabase(name);
            var options = Options.Create(new KnockMongoOptions { ConnectionString = fixture.ConnectionString });
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Seed:Demo"] = enabled.ToString() }).Build();
            var loader = new DemoTechnicianSeeder(new DemoTechnicianStore(database, options), config, new FakeEnvironment { EnvironmentName = environment });
            await test(database, loader, options);
        }
        finally { await client.DropDatabaseAsync(name); }
    }

    private sealed class FakeEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = string.Empty;
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
