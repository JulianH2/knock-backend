using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.DatabaseTest.Support;
using Knock.IntegrationTest.Support;
using Knock.Market.Api.Modules.Assistant.Data;
using MongoDB.Bson;
using MongoDB.Driver;
using StackExchange.Redis;
using Xunit;

namespace Knock.DatabaseTest.Scripts;

public sealed class Spec024StoresTests
{
    [Fact]
    public async Task PersonalLimit_ExactAndExceeded_CA_024_05()
    {
        var fixture = await RedisFixture.SharedAsync();
        using var redis = await ConnectionMultiplexer.ConnectAsync(fixture.ConnectionString);
        var store = new ConsumoIaStore(redis);
        var user = Guid.NewGuid().ToString("N");
        var day = DateOnly.FromDateTime(DateTime.UtcNow);
        (await store.ReserveAsync(user, day, 20, 20, 200, default)).Result.Should().Be(ConsumptionResult.Reserved);
        (await store.ReserveAsync(user, day, 1, 20, 200, default)).Result.Should().Be(ConsumptionResult.BudgetExhausted);
    }

    [Fact]
    public async Task GlobalLimit_AllUsersAndNextDay_CA_024_06()
    {
        var fixture = await RedisFixture.StartAsync();
        await using var cleanup = fixture;
        using var redis = await ConnectionMultiplexer.ConnectAsync(fixture.ConnectionString);
        var store = new ConsumoIaStore(redis);
        var day = DateOnly.FromDateTime(DateTime.UtcNow);
        (await store.ReserveAsync("a", day, 20, 50, 20, default)).Result.Should().Be(ConsumptionResult.Reserved);
        (await store.ReserveAsync("b", day, 1, 50, 20, default)).Result.Should().Be(ConsumptionResult.BudgetExhausted);
        (await store.ReserveAsync("b", day.AddDays(1), 1, 50, 20, default)).Result.Should().Be(ConsumptionResult.Reserved);
    }

    [Fact]
    public async Task TwoInstances_ReserveAtomicallyAndAdjustOriginalDay_CA_024_07()
    {
        await using var fixture = await RedisFixture.StartAsync();
        using var redis = await ConnectionMultiplexer.ConnectAsync(fixture.ConnectionString);
        using var otherRedis = await ConnectionMultiplexer.ConnectAsync(fixture.ConnectionString);
        var first = new ConsumoIaStore(redis);
        var second = new ConsumoIaStore(otherRedis);
        var day = DateOnly.FromDateTime(DateTime.UtcNow);
        var results = await Task.WhenAll(first.ReserveAsync("same", day, 60, 100, 100, default), second.ReserveAsync("same", day, 60, 100, 100, default));
        results.Select(x => x.Result).Should().BeEquivalentTo(new[] { ConsumptionResult.Reserved, ConsumptionResult.BudgetExhausted });
        var reservation = results.Single(x => x.Result == ConsumptionResult.Reserved).Reservation!;
        var reservationKey = $"ia:reserva:same:{day:yyyyMMdd}:{reservation.Id:N}";
        (await redis.GetDatabase().StringGetAsync(reservationKey)).ToString().Should().Be("60");
        (await redis.GetDatabase().KeyTimeToLiveAsync(reservationKey)).Should().BeCloseTo(TimeSpan.FromHours(48), TimeSpan.FromSeconds(10));
        (await first.AdjustAsync(reservation, 25, default)).Should().Be(AdjustmentResult.Applied);
        var db = redis.GetDatabase();
        var date = day.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
        ((long)await db.StringGetAsync($"ia:consumo:{date}")).Should().Be(25);
        ((long)await db.StringGetAsync($"ia:usuario:same:{date}")).Should().Be(25);
        (await db.KeyTimeToLiveAsync($"ia:consumo:{date}")).Should().BeCloseTo(TimeSpan.FromHours(48), TimeSpan.FromSeconds(10));
        (await second.ReserveAsync("same", day, 75, 100, 100, default)).Result.Should().Be(ConsumptionResult.Reserved);
    }

    [Fact]
    public async Task RedisUnavailable_FailsClosed_CA_024_14()
    {
        await using var fixture = await RedisFixture.StartAsync();
        var options = ConfigurationOptions.Parse(fixture.ConnectionString);
        options.ConnectTimeout = 200;
        options.SyncTimeout = 200;
        options.AsyncTimeout = 200;
        options.ConnectRetry = 0;
        using var redis = await ConnectionMultiplexer.ConnectAsync(options);
        var store = new ConsumoIaStore(redis);
        var reservation = await store.ReserveAsync("user", DateOnly.FromDateTime(DateTime.UtcNow), 10, 20, 200, default);
        reservation.Result.Should().Be(ConsumptionResult.Reserved);
        await fixture.StopAsync();
        (await store.ReserveAsync("user", DateOnly.FromDateTime(DateTime.UtcNow), 1, 20, 200, default)).Result.Should().Be(ConsumptionResult.LimitUnavailable);
        (await store.AdjustAsync(reservation.Reservation!, 1, default)).Should().Be(AdjustmentResult.Unavailable);
    }

    [Fact]
    public async Task RepeatedAdjustment_DoesNotReleaseAnotherReservation_CA_024_07()
    {
        await using var fixture = await RedisFixture.StartAsync();
        using var redis = await ConnectionMultiplexer.ConnectAsync(fixture.ConnectionString);
        var store = new ConsumoIaStore(redis);
        var day = DateOnly.FromDateTime(DateTime.UtcNow);
        var reservation = await store.ReserveAsync("a", day, 60, 200, 120, default);
        reservation.Result.Should().Be(ConsumptionResult.Reserved);
        (await store.ReserveAsync("a", day, 60, 200, 120, default)).Result.Should().Be(ConsumptionResult.Reserved);
        (await store.AdjustAsync(reservation.Reservation!, 20, default)).Should().Be(AdjustmentResult.Applied);
        (await store.AdjustAsync(reservation.Reservation!, 20, default)).Should().Be(AdjustmentResult.AlreadyApplied);
        (await store.ReserveAsync("b", day, 41, 200, 120, default)).Result.Should().Be(ConsumptionResult.BudgetExhausted);
    }

    [Fact]
    public async Task ConcurrentDifferentUsers_GlobalLimitAndConcurrentAdjustment_CA_024_07()
    {
        await using var fixture = await RedisFixture.StartAsync();
        using var redis = await ConnectionMultiplexer.ConnectAsync(fixture.ConnectionString);
        using var otherRedis = await ConnectionMultiplexer.ConnectAsync(fixture.ConnectionString);
        var first = new ConsumoIaStore(redis);
        var second = new ConsumoIaStore(otherRedis);
        var day = DateOnly.FromDateTime(DateTime.UtcNow);
        var attempts = await Task.WhenAll(first.ReserveAsync("a", day, 60, 100, 100, default), second.ReserveAsync("b", day, 60, 100, 100, default));
        attempts.Select(x => x.Result).Should().BeEquivalentTo(new[] { ConsumptionResult.Reserved, ConsumptionResult.BudgetExhausted });
        var reservation = attempts.Single(x => x.Reservation is not null).Reservation!;
        var adjustments = await Task.WhenAll(first.AdjustAsync(reservation, 20, default), second.AdjustAsync(reservation, 20, default));
        adjustments.Should().BeEquivalentTo(new[] { AdjustmentResult.Applied, AdjustmentResult.AlreadyApplied });
        (await first.ReserveAsync("c", day, 81, 100, 100, default)).Result.Should().Be(ConsumptionResult.BudgetExhausted);
    }

    [Fact]
    public async Task ActualAboveReservation_ChargesExcessOnceAndBlocksFurtherReservations_CA_024_07()
    {
        await using var fixture = await RedisFixture.StartAsync();
        using var redis = await ConnectionMultiplexer.ConnectAsync(fixture.ConnectionString);
        var store = new ConsumoIaStore(redis);
        var day = new DateOnly(2026, 11, 16);
        var reservation = (await store.ReserveAsync("excess", day, 60, 100, 100, default)).Reservation!;
        (await store.AdjustAsync(reservation, 120, default)).Should().Be(AdjustmentResult.Applied);
        (await store.AdjustAsync(reservation, 120, default)).Should().Be(AdjustmentResult.AlreadyApplied);
        var db = redis.GetDatabase();
        ((long)await db.StringGetAsync("ia:consumo:20261116")).Should().Be(120);
        ((long)await db.StringGetAsync("ia:usuario:excess:20261116")).Should().Be(120);
        (await store.ReserveAsync("excess", day, 1, 100, 100, default)).Result.Should().Be(ConsumptionResult.BudgetExhausted);
    }

    [Fact]
    public async Task MissingOrInconsistentReservation_IsDistinguished_CA_024_07()
    {
        await using var fixture = await RedisFixture.StartAsync();
        using var redis = await ConnectionMultiplexer.ConnectAsync(fixture.ConnectionString);
        var store = new ConsumoIaStore(redis);
        var day = new DateOnly(2026, 11, 16);
        (await store.AdjustAsync(new ConsumptionReservation("missing", day, Guid.NewGuid()), 1, default)).Should().Be(AdjustmentResult.NotFound);
        var reservation = (await store.ReserveAsync("inconsistent", day, 10, 100, 100, default)).Reservation!;
        await redis.GetDatabase().KeyDeleteAsync("ia:usuario:inconsistent:20261116");
        (await store.AdjustAsync(reservation, 1, default)).Should().Be(AdjustmentResult.Unavailable);
    }

    [Fact]
    public async Task Usage_OnlySpecifiedFieldsAndIndexes_NoTtl_CA_024_11()
    {
        var fixture = await DocumentDbFixture.SharedAsync();
        using var client = new MongoClient(fixture.ConnectionString);
        var name = "s024_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        try
        {
            await using var tools = await MongoToolsContainer.StartAsync(fixture, new Dictionary<string, string> { ["KNOCK_DB_MARKET"] = name });
            var script = $"{MongoToolsContainer.DbDirectory}/024-assistant-uso-ia.js";
            (await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script)).ExitCode.Should().Be(0);
            (await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script)).ExitCode.Should().Be(0);
            var database = client.GetDatabase(name);
            var store = new UsoIaStore(database);
            await store.SaveAsync(new UsageRecord("synthetic-sub", "asistente_knock", "model", "asistente_knock.v1", 10, 5, 100, "proveedor", null, false, DateTime.UtcNow), default);
            var doc = await database.GetCollection<BsonDocument>("uso_ia").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
            doc.Names.Should().BeEquivalentTo("_id", "usuarioId", "funcion", "modelo", "versionPrompt", "tokensEntrada", "tokensSalida", "latenciaMs", "origen", "recortado", "creadoEn");
            doc["tokensEntrada"].AsInt32.Should().Be(10);
            (await MongoIndexes.DescribeAsync(database, "uso_ia")).Should().BeEquivalentTo(
                "_id_ _id:1 - ttl:-", "usuarioId_1_creadoEn_-1 usuarioId:1,creadoEn:-1 - ttl:-", "creadoEn_-1 creadoEn:-1 - ttl:-", "funcion_1_origen_1_creadoEn_-1 funcion:1,origen:1,creadoEn:-1 - ttl:-");
        }
        finally
        {
            await client.DropDatabaseAsync(name);
        }
    }
}
