using Knock.IntegrationTest.Support;
using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Tecnicos.Data;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.DatabaseTest;

public sealed class Spec036TrabajoStoreTests
{
    [Theory]
    [InlineData("ninguno", "sin_verificar")]
    [InlineData("basica", "verificada")]
    [InlineData("completa", "verificada")]
    [InlineData("inesperado", "sin_verificar")]
    [InlineData(null, "sin_verificar")]
    public async Task Verificacion_Y_Nivel_Desde_Tecnicos_CA_036_08(string? level, string expected)
    {
        var fixture = await DocumentDbFixture.SharedAsync();
        using var client = new MongoClient(fixture.ConnectionString);
        var database = client.GetDatabase("work_" + Guid.NewGuid().ToString("N"));
        try
        {
            var doc = new BsonDocument { ["usuarioId"] = "synthetic-sub", ["nivel"] = new BsonDocument("clave", "untrusted") };
            if (level is not null) doc["nivelVerificacion"] = level;
            await database.GetCollection<BsonDocument>("tecnicos").InsertOneAsync(doc);
            var result = await new TrabajoStore(database, Options.Create(new KnockMongoOptions())).ReadVerificationAsync("synthetic-sub", CancellationToken.None);
            Assert.NotNull(result);
            Assert.Null(result.ErrorCode);
            Assert.Equal(expected, result.Estado);
            Assert.Null(result.Nivel);
        }
        finally { await client.DropDatabaseAsync(database.DatabaseNamespace.DatabaseName); }
    }

    [Fact]
    public async Task Campo_No_Texto_Y_Tecnico_Ausente_CA_036_08()
    {
        var fixture = await DocumentDbFixture.SharedAsync();
        using var client = new MongoClient(fixture.ConnectionString);
        var database = client.GetDatabase("work_" + Guid.NewGuid().ToString("N"));
        try
        {
            await database.GetCollection<BsonDocument>("tecnicos").InsertOneAsync(new BsonDocument { ["usuarioId"] = "synthetic-sub", ["nivelVerificacion"] = 42 });
            var store = new TrabajoStore(database, Options.Create(new KnockMongoOptions()));
            var result = await store.ReadVerificationAsync("synthetic-sub", CancellationToken.None);
            Assert.NotNull(result);
            Assert.Equal("sin_verificar", result.Estado);
            Assert.Null(result.ErrorCode);
            Assert.Null(await store.ReadVerificationAsync("another-sub", CancellationToken.None));
        }
        finally { await client.DropDatabaseAsync(database.DatabaseNamespace.DatabaseName); }
    }

    [Fact]
    public async Task Base_Caida_Unavailable_Sin_Valores_De_Respaldo_CA_036_05()
    {
        await using var fixture = await DocumentDbFixture.StartAsync();
        var settings = MongoClientSettings.FromConnectionString(fixture.ConnectionString);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(2);
        using var client = new MongoClient(settings);
        var database = client.GetDatabase("work_failure");
        await database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
        await fixture.StopAsync();
        var result = await new TrabajoStore(database, Options.Create(new KnockMongoOptions())).ReadVerificationAsync("synthetic-sub", CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal("unavailable", result.ErrorCode);
        Assert.Null(result.Estado);
        Assert.Null(result.Nivel);
    }
}
