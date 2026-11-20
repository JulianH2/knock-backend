extern alias MarketApi;

using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using Tasks = MarketApi::Knock.Market.Api.Common.TareasProgramadas;
using TaskStore = MarketApi::Knock.Market.Api.Modules.Requests.Data.TareasProgramadasStore;

namespace Knock.IntegrationTest.Requests;

public sealed class Spec028EjecutorTests
{
    [Fact]
    public async Task TwoExecutors_Compete_OnlyOneRuns_CA_028_05()
    {
        var fixture = await DocumentDbFixture.SharedAsync();
        using var client = new MongoClient(fixture.ConnectionString);
        var name = "s028_exec_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        var database = client.GetDatabase(name);
        try
        {
            var clock = new TestClock();
            await InsertAsync(database, clock);
            var handler = new TestHandler { Block = true };
            var first = Create(database, clock, handler);
            var second = Create(database, clock, handler);
            var running = first.ExecuteOneAsync(CancellationToken.None);
            await handler.Started.Task.WaitAsync(TimeSpan.FromSeconds(15));
            (await second.ExecuteOneAsync(CancellationToken.None)).Should().BeFalse();
            handler.Release.TrySetResult();
            (await running).Should().BeTrue();
            handler.Calls.Should().Be(1);
            (await TasksCollection(database).Find(FilterDefinition<BsonDocument>.Empty).SingleAsync())["estado"].AsString.Should().Be("hecha");
        }
        finally
        {
            await client.DropDatabaseAsync(name);
        }
    }

    [Fact]
    public async Task ExpiredLease_IsTakenOver_StaleOwnerCannotComplete_CA_028_15()
    {
        var fixture = await DocumentDbFixture.SharedAsync();
        using var client = new MongoClient(fixture.ConnectionString);
        var name = "s028_lease_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        var database = client.GetDatabase(name);
        try
        {
            var clock = new TestClock();
            await InsertAsync(database, clock);
            var first = new TaskStore(database);
            var second = new TaskStore(database);
            var old = await first.ReserveAsync(clock.GetUtcNow().UtcDateTime, CancellationToken.None);
            old.Should().NotBeNull();
            clock.Advance(TimeSpan.FromMinutes(2) + TimeSpan.FromMilliseconds(1));
            var current = await second.ReserveAsync(clock.GetUtcNow().UtcDateTime, CancellationToken.None);
            current.Should().NotBeNull();
            current.ReservaId.Should().NotBe(old.ReservaId);
            (await first.CompleteAsync(old, clock.GetUtcNow().UtcDateTime, CancellationToken.None)).Should().BeFalse();
            (await first.FailAsync(old, clock.GetUtcNow().UtcDateTime, CancellationToken.None)).Should().BeFalse();
            using var session = await client.StartSessionAsync();
            session.StartTransaction();
            (await first.ConfirmLeaseAsync(session, old, clock.GetUtcNow().UtcDateTime, CancellationToken.None)).Should().BeFalse();
            (await second.ConfirmLeaseAsync(session, current, clock.GetUtcNow().UtcDateTime, CancellationToken.None)).Should().BeTrue();
            await session.CommitTransactionAsync();
            (await second.CompleteAsync(current, clock.GetUtcNow().UtcDateTime, CancellationToken.None)).Should().BeTrue();
        }
        finally
        {
            await client.DropDatabaseAsync(name);
        }
    }

    [Fact]
    public async Task Failures_RetryWithSpecifiedDelays_ThenFail_CA_028_15()
    {
        var fixture = await DocumentDbFixture.SharedAsync();
        using var client = new MongoClient(fixture.ConnectionString);
        var name = "s028_retry_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        var database = client.GetDatabase(name);
        try
        {
            var clock = new TestClock();
            await InsertAsync(database, clock);
            var handler = new TestHandler { Fail = true };
            var executor = Create(database, clock, handler);
            foreach (var delay in new[] { 5, 30, 120, 600 })
            {
                (await executor.ExecuteOneAsync(CancellationToken.None)).Should().BeTrue();
                var task = await TasksCollection(database).Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
                task["estado"].AsString.Should().Be("pendiente");
                task["programadaPara"].ToUniversalTime().Should().Be(clock.GetUtcNow().UtcDateTime.AddSeconds(delay));
                (await executor.ExecuteOneAsync(CancellationToken.None)).Should().BeFalse();
                clock.Advance(TimeSpan.FromSeconds(delay));
            }

            (await executor.ExecuteOneAsync(CancellationToken.None)).Should().BeTrue();
            var failed = await TasksCollection(database).Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
            failed["estado"].AsString.Should().Be("fallida");
            failed["intentos"].AsInt32.Should().Be(5);
            handler.Calls.Should().Be(5);
        }
        finally
        {
            await client.DropDatabaseAsync(name);
        }
    }

    private static Tasks.EjecutorDeTareasProgramadas Create(IMongoDatabase database, TestClock clock, TestHandler handler) =>
        new(new TaskStore(database), [handler], clock, NullLogger<Tasks.EjecutorDeTareasProgramadas>.Instance);

    private static IMongoCollection<BsonDocument> TasksCollection(IMongoDatabase database) => database.GetCollection<BsonDocument>("tareas_programadas");

    private static Task InsertAsync(IMongoDatabase database, TestClock clock) =>
        TasksCollection(database).InsertOneAsync(new BsonDocument
        {
            ["tipo"] = "test",
            ["claveNegocio"] = "synthetic",
            ["estado"] = "pendiente",
            ["intentos"] = 0,
            ["programadaPara"] = clock.GetUtcNow().UtcDateTime,
            ["creadoEn"] = clock.GetUtcNow().UtcDateTime,
            ["actualizadoEn"] = clock.GetUtcNow().UtcDateTime,
        });

    private sealed class TestClock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 11, 19, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan value) => _now += value;
    }

    private sealed class TestHandler : Tasks.IManejadorDeTarea
    {
        private int _calls;
        public string Tipo => "test";
        public int Calls => _calls;
        public bool Fail { get; init; }
        public bool Block { get; init; }
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task HandleAsync(Tasks.TareaReservada task, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _calls);
            Started.TrySetResult();
            if (Block)
            {
                await Release.Task.WaitAsync(cancellationToken);
            }

            if (Fail)
            {
                throw new InvalidOperationException("synthetic");
            }
        }
    }
}
