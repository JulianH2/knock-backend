using AwesomeAssertions;
using Knock.Identity.Api.Hosting;
using Knock.UnitTest.Infrastructure;
using MongoDB.Driver;
using Xunit;

namespace Knock.UnitTest.Safety;

/// <summary>Spec 043, CA-37: bloqueos simultáneos; ante 11000 o 112 se relee el bloqueo y se reintenta con espera creciente.</summary>
public sealed class Spec043ConflictoDeEscrituraTests
{
    [Theory]
    [InlineData(MongoConflictErrors.WriteConflict)]
    [InlineData(MongoConflictErrors.DuplicateKey)]
    public async Task InsertIfAbsent_ConflictWithExistingBlock_ReportsAlreadyExisted_CA_043_37(int code)
    {
        var inserted = await MongoWriteErrors.InsertIfAbsentAsync(() => throw MongoConflictErrors.Command(code), () => Task.FromResult(true), _ => Task.CompletedTask);

        inserted.Should().BeFalse();
    }

    [Fact]
    public async Task InsertIfAbsent_ConflictWithoutBlock_RetriesWithGrowingWaitUntilItInserts_CA_043_37()
    {
        var calls = 0;
        var waits = new List<TimeSpan>();

        var inserted = await MongoWriteErrors.InsertIfAbsentAsync(
            () => ++calls < 4 ? throw MongoConflictErrors.Command(MongoConflictErrors.WriteConflict) : Task.CompletedTask,
            () => Task.FromResult(false),
            delay => { waits.Add(delay); return Task.CompletedTask; });

        inserted.Should().BeTrue();
        waits.Should().Equal(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(150));
    }

    [Fact]
    public async Task InsertIfAbsent_ConflictNeverResolves_RethrowsAfterSixAttempts_CA_043_37()
    {
        var calls = 0;

        var act = () => MongoWriteErrors.InsertIfAbsentAsync(
            () => { calls++; throw MongoConflictErrors.Command(MongoConflictErrors.WriteConflict); },
            () => Task.FromResult(false),
            _ => Task.CompletedTask);

        await act.Should().ThrowAsync<MongoCommandException>();
        calls.Should().Be(6);
    }

    [Fact]
    public async Task InsertIfAbsent_DeadlockWithExistingBlock_ReportsAlreadyExisted_CA_043_37()
    {
        var inserted = await MongoWriteErrors.InsertIfAbsentAsync(
            () => throw MongoConflictErrors.Command(MongoConflictErrors.LockError, MongoConflictErrors.DeadlockMessage),
            () => Task.FromResult(true),
            _ => Task.CompletedTask);

        inserted.Should().BeFalse();
    }

    [Fact]
    public async Task InsertIfAbsent_DeadlockWithoutBlock_RetriesWithGrowingWaitUntilItInserts_CA_043_37()
    {
        var calls = 0;
        var waits = new List<TimeSpan>();

        var inserted = await MongoWriteErrors.InsertIfAbsentAsync(
            () => ++calls < 3 ? throw MongoConflictErrors.Command(MongoConflictErrors.LockError, MongoConflictErrors.DeadlockMessage) : Task.CompletedTask,
            () => Task.FromResult(false),
            delay => { waits.Add(delay); return Task.CompletedTask; });

        inserted.Should().BeTrue();
        waits.Should().Equal(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public async Task InsertIfAbsent_DeadlockNeverResolves_RethrowsAfterSixAttempts_CA_043_37()
    {
        var calls = 0;

        var act = () => MongoWriteErrors.InsertIfAbsentAsync(
            () => { calls++; throw MongoConflictErrors.Command(MongoConflictErrors.LockError, MongoConflictErrors.DeadlockMessage); },
            () => Task.FromResult(false),
            _ => Task.CompletedTask);

        await act.Should().ThrowAsync<MongoCommandException>();
        calls.Should().Be(6);
    }

    [Fact]
    public async Task InsertIfAbsent_LockErrorWithoutDeadlockMessage_IsNotSwallowed_CA_043_37()
    {
        var calls = 0;

        var act = () => MongoWriteErrors.InsertIfAbsentAsync(
            () => { calls++; throw MongoConflictErrors.Command(MongoConflictErrors.LockError, "Some other lock failure"); },
            () => Task.FromResult(true),
            _ => Task.CompletedTask);

        await act.Should().ThrowAsync<MongoCommandException>();
        calls.Should().Be(1);
    }
}
