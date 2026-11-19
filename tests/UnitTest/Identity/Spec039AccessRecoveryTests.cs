using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Knock.UnitTest.Identity;

/// <summary>Spec 039, USR-05 (T-04): el caso solo se crea si la cuenta existe, la respuesta es la misma y nada de la cuenta cambia. Sin MongoDB.</summary>
public class Spec039AccessRecoveryTests
{
    private const string _oldPhone = "+525512345678";
    private const string _newPhone = "+525598765432";
    private const string _unknownPhone = "+525500000000";
    private const string _takenPhone = "+525511112222";
    private const string _userId = "665f1b2c3d4e5f6071829304";

    private static readonly AccessRecoveryRequestValidator _validator = new();

    private sealed class FakeLimiter : IRecoveryAttemptLimiter
    {
        public List<string> Keys { get; } = [];

        public int DenyFromCall { get; set; } = int.MaxValue;

        public bool TryAcquire(string key, out TimeSpan retryAfter)
        {
            Keys.Add(key);
            retryAfter = TimeSpan.FromMinutes(42);
            return Keys.Count < DenyFromCall;
        }
    }

    private sealed record Rig(AccessRecoveryService Service, FakeAccountStore Store, PhoneProtector Phones, Spec039Verifier Verifier, FakeLimiter Limiter);

    private static Rig Build(bool technician = false, bool rejectedToken = false)
    {
        var phones = new PhoneProtector(PhoneProtectionTestKeys.Keys());
        var store = new FakeAccountStore();
        store.Accounts.Add(new AccountSnapshot(_userId, "f0000000-0000-8000-8000-000000000001", AccountState.Active, technician, phones.Hash(_oldPhone).Value, null));
        store.Accounts.Add(new AccountSnapshot("665f1b2c3d4e5f6071829399", "f0000000-0000-8000-8000-000000000002", AccountState.Active, false, phones.Hash(_takenPhone).Value, null));
        var verifier = new Spec039Verifier(rejectedToken ? null : new FirebaseIdentity("uid-nuevo-0001", _newPhone));
        var limiter = new FakeLimiter();
        var service = new AccessRecoveryService(
            store, verifier, phones, limiter, new Spec039Clock(new DateTimeOffset(2026, 11, 17, 12, 0, 0, TimeSpan.Zero)), NullLogger<AccessRecoveryService>.Instance);
        return new Rig(service, store, phones, verifier, limiter);
    }

    private static AccessRecoveryInput Input(string previous = _oldPhone, string type = "cliente") => new("synthetic-token", previous, type);

    [Fact]
    public async Task ExistingAccount_CreatesAPendingCaseWithTheNewPhoneProtectedAndChangesNothing_CA_039_34()
    {
        var rig = Build();
        var before = rig.Store.Accounts.Select(a => a with { }).ToList();

        var outcome = await rig.Service.RequestAsync(Input(), CancellationToken.None);

        outcome.Should().BeOfType<AccessRecoveryOutcome.Accepted>();
        var recoveryCase = rig.Store.RecoveryCases.Should().ContainSingle().Subject;
        recoveryCase.UserId.Should().Be(_userId);
        recoveryCase.Type.Should().Be("cliente");
        recoveryCase.NewPhoneHash.Value.Should().Equal(rig.Phones.Hash(_newPhone).Value);
        recoveryCase.NewPhone.Ciphertext.AsSpan().IndexOf(System.Text.Encoding.UTF8.GetBytes(_newPhone)).Should().Be(-1);
        rig.Store.Accounts.Should().BeEquivalentTo(before, "no cambia ningún dato hasta que un verificador la apruebe");
    }

    [Fact]
    public async Task UnknownAccount_GetsTheSameAcceptedAnswerAndNoCase_CA_039_34()
    {
        var rig = Build();

        var outcome = await rig.Service.RequestAsync(Input(_unknownPhone), CancellationToken.None);

        outcome.Should().BeOfType<AccessRecoveryOutcome.Accepted>();
        rig.Store.RecoveryCases.Should().BeEmpty();
    }

    [Fact]
    public async Task ClientAccountAskedAsTechnician_GetsTheSameAnswerAndNoCase_CA_039_34()
    {
        var rig = Build(technician: false);

        var outcome = await rig.Service.RequestAsync(Input(type: "tecnico"), CancellationToken.None);

        outcome.Should().BeOfType<AccessRecoveryOutcome.Accepted>();
        rig.Store.RecoveryCases.Should().BeEmpty();
    }

    [Fact]
    public async Task TechnicianAccountAskedAsTechnician_CreatesTheCase_CA_039_34()
    {
        var rig = Build(technician: true);

        await rig.Service.RequestAsync(Input(type: "tecnico"), CancellationToken.None);

        rig.Store.RecoveryCases.Should().ContainSingle().Which.Type.Should().Be("tecnico");
    }

    [Fact]
    public async Task NewNumberThatAlreadyHasAnOwner_GetsTheSameAnswerAndNoCase_CA_039_34()
    {
        var rig = Build();
        rig.Verifier.Identity = new FirebaseIdentity("uid-nuevo-0001", _takenPhone);

        var outcome = await rig.Service.RequestAsync(Input(), CancellationToken.None);

        outcome.Should().BeOfType<AccessRecoveryOutcome.Accepted>();
        rig.Store.RecoveryCases.Should().BeEmpty();
    }

    [Fact]
    public async Task RepeatedRequest_KeepsASingleOpenCase_CA_039_43()
    {
        var rig = Build();

        await rig.Service.RequestAsync(Input(), CancellationToken.None);
        var second = await rig.Service.RequestAsync(Input(), CancellationToken.None);

        second.Should().BeOfType<AccessRecoveryOutcome.Accepted>();
        rig.Store.RecoveryCases.Should().ContainSingle();
    }

    [Fact]
    public async Task TokenFirebaseRejects_IsInvalidTokenAndCreatesNothing_CA_039_12()
    {
        var rig = Build(rejectedToken: true);

        var outcome = await rig.Service.RequestAsync(Input(), CancellationToken.None);

        outcome.Should().BeOfType<AccessRecoveryOutcome.InvalidToken>();
        rig.Store.RecoveryCases.Should().BeEmpty();
        rig.Limiter.Keys.Should().BeEmpty("un token inválido no gasta el límite de nadie");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task OverTheLimitForTheNewOrThePreviousNumber_IsRateLimitedWithRetryAfter_CA_039_34(int deniedCall)
    {
        var rig = Build();
        rig.Limiter.DenyFromCall = deniedCall;

        var outcome = await rig.Service.RequestAsync(Input(), CancellationToken.None);

        outcome.Should().BeEquivalentTo(new AccessRecoveryOutcome.RateLimited(TimeSpan.FromMinutes(42)));
        rig.Store.RecoveryCases.Should().BeEmpty();
    }

    [Fact]
    public async Task LimiterKeys_AreHashesAndNeverThePhone_CA_039_41()
    {
        var rig = Build();

        await rig.Service.RequestAsync(Input(), CancellationToken.None);

        rig.Limiter.Keys.Should().HaveCount(2);
        rig.Limiter.Keys.Should().OnlyContain(key => !key.Contains("5512345678", StringComparison.Ordinal) && !key.Contains("5598765432", StringComparison.Ordinal));
    }

    [Fact]
    public void FourthAttemptInTheHourForTheSameKey_IsDenied_CA_039_34()
    {
        using var limiter = new RecoveryAttemptLimiter();

        var results = Enumerable.Range(0, 4).Select(_ => limiter.TryAcquire("nuevo:abc", out TimeSpan _)).ToList();
        var other = limiter.TryAcquire("nuevo:otra", out _);
        _ = limiter.TryAcquire("nuevo:abc", out TimeSpan retryAfter);

        results.Should().Equal(true, true, true, false);
        other.Should().BeTrue("el límite es por clave");
        retryAfter.Should().BeGreaterThan(TimeSpan.Zero).And.BeLessThanOrEqualTo(TimeSpan.FromHours(1));
    }

    [Fact]
    public void ValidBody_HasNoErrors_CA_039_11()
    {
        _validator.Validate(new AccessRecoveryRequest("token", _oldPhone, "tecnico")).IsValid.Should().BeTrue();
        _validator.Validate(new AccessRecoveryRequest("token", _oldPhone, "cliente")).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, _oldPhone, "cliente", "FirebaseIdToken")]
    [InlineData("", _oldPhone, "cliente", "FirebaseIdToken")]
    [InlineData("token", null, "cliente", "CelularAnterior")]
    [InlineData("token", "5512345678", "cliente", "CelularAnterior")]
    [InlineData("token", _oldPhone, null, "Tipo")]
    [InlineData("token", _oldPhone, "admin", "Tipo")]
    [InlineData("token", _oldPhone, "Tecnico", "Tipo")]
    public void InvalidField_FailsOnItsOwnFieldWithoutRepeatingTheValue_CA_039_12(string? token, string? previous, string? type, string field)
    {
        var errors = _validator.Validate(new AccessRecoveryRequest(token, previous, type)).Errors;

        errors.Select(e => e.PropertyName).Distinct().Should().Equal(field);
        var sent = field switch { "FirebaseIdToken" => token, "CelularAnterior" => previous, _ => type };
        errors.Should().OnlyContain(e => string.IsNullOrEmpty(sent) || !e.ErrorMessage.Contains(sent, StringComparison.Ordinal));
    }
}
