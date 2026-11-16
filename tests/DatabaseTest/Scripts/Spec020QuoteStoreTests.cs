using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Requests.Data;
using Knock.Market.Api.Modules.Requests.Domain;
using Knock.Market.Api.Modules.Requests.Services;
using Knock.Market.Api.Modules.Requests.Validators;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.DatabaseTest.Scripts;

public sealed class Spec020QuoteStoreTests
{
    [Fact]
    public async Task TechnicianDirectory_FiltersTradeAndNeighborhood_R_23()
    {
        var fixture = await DocumentDbFixture.SharedAsync();
        using var client = new MongoClient(fixture.ConnectionString);
        var name = "s020_eligibility_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        try
        {
            var db = client.GetDatabase(name);
            await db.GetCollection<BsonDocument>("tecnicos").InsertManyAsync([
                new BsonDocument { ["usuarioId"] = "eligible", ["estado"] = "activo", ["oculto"] = false, ["oficios"] = new BsonArray { "plo" }, ["coloniaBaseId"] = "colonia" },
                new BsonDocument { ["usuarioId"] = "trade", ["estado"] = "activo", ["oculto"] = false, ["oficios"] = new BsonArray { "ele" }, ["coloniaBaseId"] = "colonia" },
                new BsonDocument { ["usuarioId"] = "zone", ["estado"] = "activo", ["oculto"] = false, ["oficios"] = new BsonArray { "plo" }, ["coloniaBaseId"] = "otra" },
            ]);
            var directory = new MongoTechnicianDirectory(db, Options.Create(new KnockMongoOptions()));
            var request = new QuoteRequestSnapshot("plo", "colonia", RequestState.Enviada, DateTimeOffset.UtcNow.AddHours(1), DateOnly.FromDateTime(DateTime.UtcNow), null, null);
            (await directory.FindEligibleAsync("eligible", request, CancellationToken.None)).Should().NotBeNull();
            (await directory.FindEligibleAsync("trade", request, CancellationToken.None)).Should().BeNull();
            (await directory.FindEligibleAsync("zone", request, CancellationToken.None)).Should().BeNull();
            await db.GetCollection<BsonDocument>("tecnicos").InsertManyAsync([
                new BsonDocument { ["usuarioId"] = "near", ["estado"] = "activo", ["oculto"] = false, ["oficios"] = new BsonArray { "plo" }, ["ubicacionBase"] = new BsonDocument { ["type"] = "Point", ["coordinates"] = new BsonArray { -99.0, 19.0 } }, ["radioAtencionKm"] = 5 },
                new BsonDocument { ["usuarioId"] = "far", ["estado"] = "activo", ["oculto"] = false, ["oficios"] = new BsonArray { "plo" }, ["ubicacionBase"] = new BsonDocument { ["type"] = "Point", ["coordinates"] = new BsonArray { -99.0, 19.1 } }, ["radioAtencionKm"] = 5 },
            ]);
            var geographicRequest = request with { Latitude = 19.0, Longitude = -99.0 };
            (await directory.FindEligibleAsync("near", geographicRequest, CancellationToken.None)).Should().NotBeNull();
            (await directory.FindEligibleAsync("far", geographicRequest, CancellationToken.None)).Should().BeNull();
        }
        finally
        {
            await client.DropDatabaseAsync(name);
        }
    }

    [Fact]
    public async Task TechnicianDirectory_FiltersInactiveAndHidden_R_23()
    {
        var fixture = await DocumentDbFixture.SharedAsync();
        using var client = new MongoClient(fixture.ConnectionString);
        var name = "s020_directory_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        try
        {
            var db = client.GetDatabase(name);
            await db.GetCollection<BsonDocument>("tecnicos").InsertManyAsync([
                new BsonDocument { ["usuarioId"] = "active", ["estado"] = "activo", ["oculto"] = false },
                new BsonDocument { ["usuarioId"] = "suspended", ["estado"] = "suspendido", ["oculto"] = false },
                new BsonDocument { ["usuarioId"] = "hidden", ["estado"] = "activo", ["oculto"] = true },
            ]);
            var directory = new MongoTechnicianDirectory(db, Options.Create(new KnockMongoOptions()));
            (await directory.FindBySubAsync("active", CancellationToken.None)).Should().NotBeNull();
            (await directory.FindBySubAsync("suspended", CancellationToken.None)).Should().BeNull();
            (await directory.FindBySubAsync("hidden", CancellationToken.None)).Should().BeNull();
            (await directory.ExistsAsync("suspended", CancellationToken.None)).Should().BeTrue();
            (await directory.ExistsAsync("unknown", CancellationToken.None)).Should().BeFalse();
        }
        finally
        {
            await client.DropDatabaseAsync(name);
        }
    }

    [Fact]
    public async Task SavesQuoteAndOutboxRejectsDuplicatesAndConcurrentSixth_CA_020_19()
    {
        var fixture = await DocumentDbFixture.SharedAsync();
        using var client = new MongoClient(fixture.ConnectionString);
        var name = "s020_quotes_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        try
        {
            var db = client.GetDatabase(name);
            await db.CreateCollectionAsync("solicitudes");
            await db.CreateCollectionAsync("cotizaciones");
            await db.CreateCollectionAsync("outbox");
            var quotes = db.GetCollection<BsonDocument>("cotizaciones");
            await quotes.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("solicitudId").Ascending("tecnicoId"), new CreateIndexOptions { Unique = true }));
            var id = ObjectId.GenerateNewId();
            var now = DateTimeOffset.UtcNow;
            await db.GetCollection<BsonDocument>("solicitudes").InsertOneAsync(new BsonDocument { ["_id"] = id, ["estado"] = "enviada", ["venceEn"] = now.AddHours(1).UtcDateTime });
            var store = new QuoteStore(db, Options.Create(new KnockMongoOptions()), Options.Create(new QuotePricingOptions()));
            var body = new QuoteValidationInput("fijo", 100, null, null, DateOnly.FromDateTime(now.UtcDateTime), "12:00", null, DateOnly.FromDateTime(now.UtcDateTime));
            var technician = ObjectId.GenerateNewId();
            (await store.SaveAsync(id, technician, "sub", "plo", body, now, CancellationToken.None)).Should().Be(QuoteCheckResult.Created);
            var saved = await quotes.Find(Builders<BsonDocument>.Filter.Eq("tecnicoId", technician)).FirstAsync();
            saved["tope"].AsInt32.Should().Be(110);
            saved["estado"].AsString.Should().Be("enviada");
            (await store.SaveAsync(id, technician, "sub", "plo", body, now, CancellationToken.None)).Should().Be(QuoteCheckResult.QuoteDuplicate);
            for (var i = 0; i < 3; i++)
                (await store.SaveAsync(id, ObjectId.GenerateNewId(), "sub-" + i, "plo", body, now, CancellationToken.None)).Should().Be(QuoteCheckResult.Created);
            QuoteCheckResult[] both;
            try
            {
                both = await Task.WhenAll(
                    store.SaveAsync(id, ObjectId.GenerateNewId(), "a", "plo", body, now, CancellationToken.None),
                    store.SaveAsync(id, ObjectId.GenerateNewId(), "b", "plo", body, now, CancellationToken.None));
            }
            catch (MongoCommandException ex)
            {
                throw new InvalidOperationException($"Mongo command failed: code={ex.Code}, name={ex.CodeName}, response={ex.Result}", ex);
            }
            both.Should().BeEquivalentTo(new[] { QuoteCheckResult.Created, QuoteCheckResult.QuoteQuotaFull });
            (await quotes.CountDocumentsAsync(Builders<BsonDocument>.Filter.Eq("solicitudId", id))).Should().Be(5);
            (await db.GetCollection<BsonDocument>("outbox").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(5);
            var events = await db.GetCollection<BsonDocument>("outbox").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            events.Should().OnlyContain(e => e["tipo"] == "CotizacionEnviada" && e["solicitudId"] == id);
            var request = await db.GetCollection<BsonDocument>("solicitudes").Find(Builders<BsonDocument>.Filter.Eq("_id", id)).FirstAsync();
            request["estado"].AsString.Should().Be("cotizando");
        }
        finally
        {
            await client.DropDatabaseAsync(name);
        }
    }
}
