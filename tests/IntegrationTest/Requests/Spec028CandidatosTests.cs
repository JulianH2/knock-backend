extern alias MarketApi;

using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using MarketApi::Knock.Market.Api.Modules.Requests.Data;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoOptions = MarketApi::Knock.Market.Api.Hosting.KnockMongoOptions;

namespace Knock.IntegrationTest.Requests;

public sealed class Spec028CandidatosTests
{
    [Fact]
    public async Task EligibleCandidates_AreOrderedByDistance_AndExcludeEveryIneligibleTechnician_CA_028_04()
    {
        using var factory = await MarketApiFactory.CreateAsync();
        var db = factory.Database;
        var technicians = db.GetCollection<BsonDocument>("tecnicos");
        await technicians.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(new BsonDocument("ubicacionBase", "2dsphere")));
        var now = new DateTime(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);
        var request = ObjectId.GenerateNewId();
        var near = Technician("near", -99.0001, now);
        var far = Technician("far", -99.01, now);
        var hidden = Technician("hidden", -99.0002, now); hidden["oculto"] = true;
        var suspended = Technician("suspended", -99.0002, now); suspended["estado"] = "suspendido";
        var unavailable = Technician("unavailable", -99.0002, now); unavailable["disponibleHasta"] = now;
        var wrongTrade = Technician("wrong-trade", -99.0002, now); wrongTrade["oficios"] = new BsonArray { "electricidad" };
        var client = Technician("client", -99.0002, now);
        var outside = Technician("outside", -99.1, now); outside["radioAtencionKm"] = 1;
        var sent = Technician("sent", -99.0002, now);
        await technicians.InsertManyAsync([far, near, hidden, suspended, unavailable, wrongTrade, client, outside, sent]);
        await db.GetCollection<BsonDocument>("envios").InsertOneAsync(new BsonDocument
        {
            ["solicitudId"] = request,
            ["tecnicoId"] = sent["_id"],
            ["estado"] = "descartado",
        });
        var store = new CandidatosStore(db, Options.Create(new MongoOptions()));
        var candidates = await store.FindAsync(request, "client", "plomeria", "target", 19.0, -99.0, now, CancellationToken.None);
        candidates.Select(x => x.TechnicianId).Should().Equal(near["_id"].ToString(), far["_id"].ToString());
        candidates[0].DistanceKm.Should().NotBeNull();
        candidates[1].DistanceKm.Should().NotBeNull();
        candidates[0].DistanceKm!.Value.Should().BeLessThan(candidates[1].DistanceKm!.Value);
    }

    [Fact]
    public async Task BaseColonia_WithoutLocation_RemainsEligible_CA_028_04()
    {
        using var factory = await MarketApiFactory.CreateAsync();
        var collection = factory.Database.GetCollection<BsonDocument>("tecnicos");
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(new BsonDocument("ubicacionBase", "2dsphere")));
        var now = DateTime.UtcNow;
        var technician = Technician("base", -99.0, now);
        technician.Remove("ubicacionBase");
        technician["coloniaBaseId"] = "target";
        await collection.InsertOneAsync(technician);
        var store = new CandidatosStore(factory.Database, Options.Create(new MongoOptions()));
        var candidates = await store.FindAsync(ObjectId.GenerateNewId(), "client", "plomeria", "target", 19, -99, now, CancellationToken.None);
        candidates.Should().ContainSingle(x => x.TechnicianId == technician["_id"].ToString() && x.DistanceKm == null);
        var withoutCenter = await store.FindAsync(ObjectId.GenerateNewId(), "client", "plomeria", "target", null, null, now, CancellationToken.None);
        withoutCenter.Should().ContainSingle(x => x.TechnicianId == technician["_id"].ToString() && x.DistanceKm == null);
    }

    [Fact]
    public async Task Delivery_IsStored_AndUniqueIndexRejectsDuplicates_CA_028_04()
    {
        using var factory = await MarketApiFactory.CreateAsync();
        var db = factory.Database;
        var collection = db.GetCollection<BsonDocument>("envios");
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            new BsonDocument { ["solicitudId"] = 1, ["tecnicoId"] = 1 }, new CreateIndexOptions { Unique = true }));
        var request = ObjectId.GenerateNewId();
        var technician = ObjectId.GenerateNewId();
        var now = DateTime.UtcNow;
        var store = new EnviosStore(db);
        await store.InsertAsync(request, technician, "synthetic", 1, "nuevo", now, CancellationToken.None);
        var saved = await collection.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        saved["solicitudId"].AsObjectId.Should().Be(request);
        saved["tecnicoId"].AsObjectId.Should().Be(technician);
        saved["estado"].AsString.Should().Be("activo");
        var duplicate = () => store.InsertAsync(request, technician, "synthetic", 2, "relleno", now, CancellationToken.None);
        (await duplicate.Should().ThrowAsync<MongoWriteException>()).Which.WriteError.Category.Should().Be(ServerErrorCategory.DuplicateKey);
    }

    private static BsonDocument Technician(string user, double longitude, DateTime now) => new()
    {
        ["_id"] = ObjectId.GenerateNewId(),
        ["usuarioId"] = user,
        ["estado"] = "activo",
        ["oculto"] = false,
        ["oficios"] = new BsonArray { "plomeria" },
        ["disponibleHasta"] = now.AddHours(1),
        ["coloniaBaseId"] = "other",
        ["radioAtencionKm"] = 10,
        ["ubicacionBase"] = new BsonDocument { ["type"] = "Point", ["coordinates"] = new BsonArray { longitude, 19.0 } },
        ["calificacion"] = 4.5,
        ["trabajosTerminados"] = 5,
    };
}
