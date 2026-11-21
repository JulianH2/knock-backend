using AwesomeAssertions;
using Knock.Market.Api.Modules.Tecnicos.Data;
using Knock.UnitTest.Infrastructure;
using MongoDB.Driver;
using Xunit;

namespace Knock.UnitTest.Tecnicos;

/// <summary>Spec 046, CA-69: dos registros simultáneos de la misma cuenta; ante 11000 o 112 se relee el perfil y se reintenta con espera creciente.</summary>
public sealed class Spec046ConflictoDeEscrituraTests
{
    [Theory]
    [InlineData(MongoConflictErrors.WriteConflict)]
    [InlineData(MongoConflictErrors.DuplicateKey)]
    public async Task InsertOnce_ConflictWithExistingProfile_ReportsAlreadyExisted_CA_046_69(int code)
    {
        var waits = new List<TimeSpan>();

        var inserted = await TechnicianProfileStore.InsertOnceAsync(
            () => throw MongoConflictErrors.Command(code),
            () => Task.FromResult(true),
            delay => { waits.Add(delay); return Task.CompletedTask; });

        inserted.Should().BeFalse();
        waits.Should().BeEmpty();
    }

    [Fact]
    public async Task InsertOnce_ConflictWithoutProfile_RetriesWithGrowingWaitUntilItInserts_CA_046_69()
    {
        var calls = 0;
        var waits = new List<TimeSpan>();

        var inserted = await TechnicianProfileStore.InsertOnceAsync(
            () => ++calls < 3 ? throw MongoConflictErrors.Command(MongoConflictErrors.WriteConflict) : Task.CompletedTask,
            () => Task.FromResult(false),
            delay => { waits.Add(delay); return Task.CompletedTask; });

        inserted.Should().BeTrue();
        calls.Should().Be(3);
        waits.Should().Equal(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public async Task InsertOnce_ConflictNeverResolves_RethrowsAfterSixAttempts_CA_046_69()
    {
        var calls = 0;

        var act = () => TechnicianProfileStore.InsertOnceAsync(
            () => { calls++; throw MongoConflictErrors.Command(MongoConflictErrors.WriteConflict); },
            () => Task.FromResult(false),
            _ => Task.CompletedTask);

        (await act.Should().ThrowAsync<MongoCommandException>()).Which.Code.Should().Be(MongoConflictErrors.WriteConflict);
        calls.Should().Be(6);
    }

    [Fact]
    public async Task InsertOnce_OtherServerError_IsNotSwallowed_CA_046_69()
    {
        var act = () => TechnicianProfileStore.InsertOnceAsync(
            () => throw MongoConflictErrors.Command(13),
            () => Task.FromResult(true),
            _ => Task.CompletedTask);

        await act.Should().ThrowAsync<MongoCommandException>();
    }
}
