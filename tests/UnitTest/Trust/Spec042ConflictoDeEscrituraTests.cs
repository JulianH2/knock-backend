using AwesomeAssertions;
using Knock.Market.Api.Modules.Trust.Data;
using Knock.UnitTest.Infrastructure;
using MongoDB.Driver;
using Xunit;

namespace Knock.UnitTest.Trust;

/// <summary>Spec 042, CA-19: dos peticiones iguales a la vez; ante 11000 o 112 se relee el guardado y se reintenta con espera creciente.</summary>
public sealed class Spec042ConflictoDeEscrituraTests
{
    private static readonly DateTime _now = new(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _earlier = new(2026, 11, 19, 8, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(MongoConflictErrors.WriteConflict)]
    [InlineData(MongoConflictErrors.DuplicateKey)]
    public async Task Save_ConflictWithExistingRecord_ReturnsTheExistingOneAsNotCreated_CA_042_19(int code)
    {
        var result = await TrustStore.SaveOnceAsync(
            () => throw MongoConflictErrors.Command(code),
            () => Task.FromResult<DateTime?>(_earlier),
            _now,
            _ => Task.CompletedTask);

        result.Created.Should().BeFalse();
        result.SavedAtUtc.Should().Be(_earlier);
    }

    [Fact]
    public async Task Save_ConflictWithoutRecord_RetriesWithGrowingWaitUntilItCreates_CA_042_19()
    {
        var calls = 0;
        var waits = new List<TimeSpan>();

        var result = await TrustStore.SaveOnceAsync(
            () => ++calls < 3 ? throw MongoConflictErrors.Command(MongoConflictErrors.WriteConflict) : Task.FromResult(true),
            () => Task.FromResult<DateTime?>(null),
            _now,
            delay => { waits.Add(delay); return Task.CompletedTask; });

        result.Created.Should().BeTrue();
        result.SavedAtUtc.Should().Be(_now);
        waits.Should().Equal(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public async Task Save_ConflictNeverResolves_RethrowsAfterSixAttempts_CA_042_19()
    {
        var calls = 0;

        var act = () => TrustStore.SaveOnceAsync(
            () => { calls++; throw MongoConflictErrors.Command(MongoConflictErrors.WriteConflict); },
            () => Task.FromResult<DateTime?>(null),
            _now,
            _ => Task.CompletedTask);

        await act.Should().ThrowAsync<MongoCommandException>();
        calls.Should().Be(6);
    }

    [Fact]
    public async Task Save_UpsertThatDidNotInsert_ReturnsTheExistingRecord_CA_042_19()
    {
        var result = await TrustStore.SaveOnceAsync(() => Task.FromResult(false), () => Task.FromResult<DateTime?>(_earlier), _now, _ => Task.CompletedTask);

        result.Created.Should().BeFalse();
        result.SavedAtUtc.Should().Be(_earlier);
    }
}
