extern alias identity;
using System.Security.Cryptography;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;
using DemoSeedGuard = identity::Knock.Identity.Api.Modules.Usuarios.Data.DemoSeedGuard;

namespace Knock.DatabaseTest.Demo;

public sealed class DemoSeedGuardTests
{
    [Theory]
    [InlineData("mongodb://remote.invalid")]
    [InlineData("mongodb://localhost,remote.invalid")]
    [InlineData("mongodb+srv://localhost")]
    [InlineData("remote.invalid", true)]
    [InlineData("mongodb://localhost.remote.invalid")]
    public async Task NonLocalHost_CA_038_12(string connection, bool credentials = false)
    {
        if (credentials) { connection = new MongoUrlBuilder { Server = new MongoServerAddress(connection), Username = "fake", Password = "example" }.ToString(); }
        var fixture = await DocumentDbFixture.SharedAsync();
        using var client = new MongoClient(fixture.ConnectionString);
        var db = client.GetDatabase("s038_guard_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(8)));
        try
        {
            var users = db.GetCollection<BsonDocument>("usuarios");
            var before = await users.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => DemoSeedGuard.ValidateAsync(connection, users, CancellationToken.None));
            Assert.DoesNotContain(connection, error.ToString(), StringComparison.Ordinal);
            Assert.Equal(before, await users.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        }
        finally { await client.DropDatabaseAsync(db.DatabaseNamespace.DatabaseName); }
    }

    [Theory]
    [InlineData("mongodb://localhost")]
    [InlineData("mongodb://127.0.0.1")]
    [InlineData("mongodb://documentdb-local")]
    [InlineData("mongodb://localhost,documentdb-local")]
    [InlineData("localhost", true)]
    public async Task LocalHostsAreAccepted_CA_038_12(string connection, bool credentials = false)
    {
        if (credentials) { connection = new MongoUrlBuilder { Server = new MongoServerAddress(connection), Username = "fake", Password = "example" }.ToString(); }
        var fixture = await DocumentDbFixture.SharedAsync();
        using var client = new MongoClient(fixture.ConnectionString);
        var users = client.GetDatabase("s038_guard_empty").GetCollection<BsonDocument>("usuarios");
        await DemoSeedGuard.ValidateAsync(connection, users, CancellationToken.None);
    }

    [Fact]
    public async Task NonDemoDocumentIsRejected_CA_038_12()
    {
        var fixture = await DocumentDbFixture.SharedAsync();
        using var client = new MongoClient(fixture.ConnectionString);
        var db = client.GetDatabase("s038_guard_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(8)));
        try
        {
            var users = db.GetCollection<BsonDocument>("usuarios");
            await users.InsertOneAsync(new BsonDocument { ["_id"] = ObjectId.GenerateNewId() });
            var before = await users.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(() => DemoSeedGuard.ValidateAsync("mongodb://localhost", users, CancellationToken.None));
            Assert.Equal(before, await users.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync());
        }
        finally { await client.DropDatabaseAsync(db.DatabaseNamespace.DatabaseName); }
    }
}
