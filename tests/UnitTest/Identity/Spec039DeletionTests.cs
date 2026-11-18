using AwesomeAssertions;
using Knock.Identity.Api.Common.Errors;
using Knock.Identity.Api.Modules.Account.Domain;
using Knock.Identity.Api.Modules.Account.Dtos;
using Knock.Identity.Api.Modules.Account.Services;
using Knock.Identity.Api.Modules.Account.Validators;
using Knock.Identity.Api.Modules.Sesiones.Domain;
using Knock.Identity.Api.Modules.Sesiones.Services;
using Knock.Identity.Api.Modules.Usuarios.Data;
using Knock.Identity.Api.Modules.Usuarios.Domain;
using Knock.Identity.Api.Modules.Usuarios.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Knock.UnitTest.Identity;

/// <summary>Spec 039, USR-10 y USR-11 (T-09 y T-10): gracia de 7 días, reautenticación, visita en curso y cancelación. Sin MongoDB.</summary>
public class Spec039DeletionTests
{
    private const string _phone = "+525512345678";
    private const string _uid = "uid-prueba-0001";
    private const string _userId = "665f1b2c3d4e5f6071829304";
    private const string _session = "665f1b2c3d4e5f6071829a01";

    private static readonly DateTimeOffset _now = new(2026, 11, 17, 12, 0, 0, TimeSpan.Zero);
    private static readonly string _sub = UserIdDerivation.Derive(_uid).ToString("D");
    private static readonly DeleteAccountRequestValidator _validator = new();

    private sealed class NoCache : IProfileCache
    {
        public int Removed { get; private set; }

        public Task<UserProfile?> GetAsync(string sub, CancellationToken cancellationToken) => Task.FromResult<UserProfile?>(null);

        public Task SetAsync(UserProfile profile, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RemoveAsync(string sub, CancellationToken cancellationToken)
        {
            Removed++;
            return Task.CompletedTask;
        }
    }

    private sealed record Rig(AccountDeletionService Service, FakeAccountStore Store, Spec039Market Market, Spec039Verifier Verifier, NoCache Cache);

    private static Rig Build(TimeSpan sessionAge = default, string state = AccountState.Active)
    {
        var phones = new PhoneProtector(PhoneProtectionTestKeys.Keys());
        var store = new FakeAccountStore();
        store.Accounts.Add(new AccountSnapshot(_userId, _sub, state, false, phones.Hash(_phone).Value, null));
        store.SessionStarts[_session] = _now.UtcDateTime - sessionAge;
        var market = new Spec039Market();
        var verifier = new Spec039Verifier(new FirebaseIdentity(_uid, _phone));
        var cache = new NoCache();
        return new Rig(new AccountDeletionService(store, market, verifier, phones, new ProfileCacheInvalidator(cache), new Spec039Clock(_now), NullLogger<AccountDeletionService>.Instance), store, market, verifier, cache);
    }

    private static DeleteAccountInput Input(string? token = null) => new(token);

    [Fact]
    public async Task Schedule_SetsTheGracePeriodCancelsOpenItemsAndKeepsTheAccountReadable_CA_039_25()
    {
        var rig = Build();

        var outcome = await rig.Service.ScheduleAsync(_sub, _session, Input(), CancellationToken.None);

        outcome.Should().Be(new DeletionOutcome.Scheduled(_now.UtcDateTime.AddDays(7)));
        var account = rig.Store.Accounts.Single();
        account.State.Should().Be("eliminacion_programada");
        account.DeleteAt.Should().Be(_now.UtcDateTime.AddDays(7));
        rig.Market.Cancelled.Should().Equal(_sub);
        rig.Cache.Removed.Should().Be(1);
    }

    [Fact]
    public async Task AVisitInProgress_Is409DeletionNotAllowedAndNothingIsCancelled_CA_039_29()
    {
        var rig = Build();
        rig.Market.Pending = new AccountPending(1, ["a"], 0, [], VisitInProgress: true);

        var act = () => rig.Service.ScheduleAsync(_sub, _session, Input(), CancellationToken.None);

        (await act.Should().ThrowAsync<DomainException>()).Which.Code.Should().Be("deletion_not_allowed");
        rig.Market.Cancelled.Should().BeEmpty();
        rig.Store.Accounts.Single().State.Should().Be("activa");
    }

    [Fact]
    public async Task AskingTwice_Is409DeletionNotAllowed_CA_039_38()
    {
        var rig = Build();
        await rig.Service.ScheduleAsync(_sub, _session, Input(), CancellationToken.None);

        var act = () => rig.Service.ScheduleAsync(_sub, _session, Input(), CancellationToken.None);

        var error = (await act.Should().ThrowAsync<DomainException>()).Which;
        error.Kind.Should().Be(DomainErrorKind.Conflict);
        error.Code.Should().Be("deletion_not_allowed");
        rig.Market.Cancelled.Should().ContainSingle();
    }

    [Fact]
    public async Task OldSessionWithoutToken_RequiresReauthAndChangesNothing_CA_039_27()
    {
        var rig = Build(TimeSpan.FromHours(25));

        var outcome = await rig.Service.ScheduleAsync(_sub, _session, Input(), CancellationToken.None);

        outcome.Should().BeOfType<DeletionOutcome.ReauthRequired>();
        rig.Store.Accounts.Single().State.Should().Be("activa");
        rig.Market.Cancelled.Should().BeEmpty();
    }

    [Fact]
    public async Task OldSessionWithTheAccountsFirebaseToken_Schedules_CA_039_25()
    {
        var rig = Build(TimeSpan.FromHours(25));

        var outcome = await rig.Service.ScheduleAsync(_sub, _session, Input("synthetic"), CancellationToken.None);

        outcome.Should().BeOfType<DeletionOutcome.Scheduled>();
    }

    [Fact]
    public async Task OldSessionWithAnotherPersonsToken_IsRejected_CA_039_27()
    {
        var rig = Build(TimeSpan.FromHours(25));
        rig.Verifier.Identity = new FirebaseIdentity("uid-de-otra-persona", "+525500000001");

        var outcome = await rig.Service.ScheduleAsync(_sub, _session, Input("synthetic"), CancellationToken.None);

        outcome.Should().BeOfType<DeletionOutcome.ReauthRejected>();
        rig.Store.Accounts.Single().State.Should().Be("activa");
    }

    [Fact]
    public async Task FreshSession_DoesNotCallFirebase_CA_039_25()
    {
        var rig = Build(TimeSpan.FromHours(23));

        await rig.Service.ScheduleAsync(_sub, _session, Input(), CancellationToken.None);

        rig.Verifier.Calls.Should().Be(0);
    }

    [Fact]
    public async Task MarketDown_Is503AndTheAccountStaysActive_CA_039_25()
    {
        var rig = Build();
        rig.Market.Down = true;

        var act = () => rig.Service.ScheduleAsync(_sub, _session, Input(), CancellationToken.None);

        (await act.Should().ThrowAsync<DomainException>()).Which.Kind.Should().Be(DomainErrorKind.ServiceUnavailable);
        rig.Store.Accounts.Single().State.Should().Be("activa");
    }

    [Fact]
    public async Task Cancel_DuringTheGraceReturnsTheAccountToActive_CA_039_30()
    {
        var rig = Build();
        await rig.Service.ScheduleAsync(_sub, _session, Input(), CancellationToken.None);

        await rig.Service.CancelAsync(_sub, CancellationToken.None);

        var account = rig.Store.Accounts.Single();
        account.State.Should().Be("activa");
        account.DeleteAt.Should().BeNull();
    }

    [Fact]
    public async Task CancelWithNothingScheduled_Is409NoDeletionScheduled_CA_039_32()
    {
        var rig = Build();

        var act = () => rig.Service.CancelAsync(_sub, CancellationToken.None);

        (await act.Should().ThrowAsync<DomainException>()).Which.Code.Should().Be("no_deletion_scheduled");
    }

    [Fact]
    public void ValidBody_HasNoErrors_CA_039_25()
    {
        _validator.Validate(new DeleteAccountRequest("ELIMINAR", null, new string('m', 200))).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("eliminar")]
    [InlineData("ELIMINAR ")]
    public void WrongConfirmation_FailsOnItsOwnField_CA_039_27(string? text)
    {
        var errors = _validator.Validate(new DeleteAccountRequest(text)).Errors;

        errors.Select(e => e.PropertyName).Distinct().Should().Equal("Confirmacion");
        errors.Should().OnlyContain(e => string.IsNullOrEmpty(text) || !e.ErrorMessage.Contains(text, StringComparison.Ordinal));
    }

    [Fact]
    public void ReasonOver200_FailsWithoutRepeatingIt_CA_039_27()
    {
        var reason = new string('m', 201);

        var errors = _validator.Validate(new DeleteAccountRequest("ELIMINAR", null, reason)).Errors;

        errors.Select(e => e.PropertyName).Distinct().Should().Equal("Motivo");
        errors.Should().OnlyContain(e => !e.ErrorMessage.Contains(reason, StringComparison.Ordinal));
    }
}
