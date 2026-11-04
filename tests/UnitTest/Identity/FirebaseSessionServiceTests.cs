using System.Security.Cryptography;
using System.Text;
using AwesomeAssertions;
using Knock.Api.Common.Errors;
using Knock.Api.Common.Security;
using Knock.Api.Modules.Identity.Data;
using Knock.Api.Modules.Identity.Domain;
using Knock.Api.Modules.Identity.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Knock.UnitTest.Identity;

/// <summary>HU-001: flujo del canje con dobles del verificador y de la base (sin MongoDB; la base real se prueba en Database e Integration).</summary>
public class FirebaseSessionServiceTests
{
    private const string _phone = "+525512345678";
    private const string _otherPhone = "+525598765432";
    private const string _uid = "uid-prueba-0001";
    private const string _deviceId = "3f2b8c1e-5a7d-4e9b-8c3a-1d2e3f4a5b6c";

    private static readonly DateTimeOffset _now = new(2026, 10, 28, 12, 0, 0, TimeSpan.Zero);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeVerifier(FirebaseIdentity? identity) : IFirebaseIdTokenVerifier
    {
        public Task<FirebaseIdentity?> VerifyAsync(string idToken, CancellationToken cancellationToken) => Task.FromResult(identity);
    }

    /// <summary>Doble de la base: guarda en listas lo que el servicio escribe y deja simular la carrera de creación.</summary>
    private sealed class FakeStore : IIdentityStore
    {
        public List<UserAccount> Users { get; } = [];

        public List<NewAccount> Created { get; } = [];

        public List<RefreshTokenRecord> Refreshes { get; } = [];

        /// <summary>Si se asigna, el siguiente alta la rechaza (otro canje ganó) y deja esta cuenta guardada.</summary>
        public UserAccount? WinnerOfRace { get; set; }

        public Task<UserAccount?> FindBySubAsync(string sub, CancellationToken cancellationToken) =>
            Task.FromResult(Users.FirstOrDefault(a => a.Sub == sub));

        public Task<UserAccount?> FindByPhoneHashAsync(byte[] phoneHash, CancellationToken cancellationToken) =>
            Task.FromResult(Users.FirstOrDefault(a => a.PhoneHash.AsSpan().SequenceEqual(phoneHash)));

        public Task<AccountCreation> CreateAccountAsync(NewAccount account, CancellationToken cancellationToken)
        {
            if (WinnerOfRace is { } winner)
            {
                Users.Add(winner);
                WinnerOfRace = null;
                return Task.FromResult(AccountCreation.AlreadyExists);
            }

            Created.Add(account);
            Refreshes.Add(account.Refresh);
            Users.Add(new UserAccount(account.Id, account.Sub, UserState.Active, false, account.PhoneHash));
            return Task.FromResult(AccountCreation.Created);
        }

        public Task AddRefreshTokenAsync(RefreshTokenRecord refresh, CancellationToken cancellationToken)
        {
            Refreshes.Add(refresh);
            return Task.CompletedTask;
        }
    }

    private static readonly ExchangeRequest _request = new("synthetic-id-token", _deviceId, "correlation-1", "trace-1");

    private static (FirebaseSessionService Service, FakeStore Store, PhoneProtector Phones) Build(
        FirebaseIdentity? identity = null, bool rejected = false)
    {
        var store = new FakeStore();
        var phones = new PhoneProtector(PhoneProtectionTestKeys.Keys());
        var time = new FixedTimeProvider(_now);
        var settings = new JwtSettings("knock-api", "knock-app", new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32)));
        var service = new FirebaseSessionService(
            new FakeVerifier(rejected ? null : identity ?? new FirebaseIdentity(_uid, _phone)),
            new SessionTokenIssuer(settings, time),
            phones,
            store,
            time,
            NullLogger<FirebaseSessionService>.Instance);
        return (service, store, phones);
    }

    private static UserAccount ExistingAccount(PhoneProtector phones, string state = UserState.Active, bool profileComplete = false, string phone = _phone, string uid = _uid) =>
        new("665f1b2c3d4e5f6071829304", UserIdDerivation.Derive(uid).ToString("D"), state, profileComplete, phones.Hash(phone).Value);

    [Fact]
    public async Task ExistingAccount_ReturnsSessionWithoutCreatingAnything_TC_HU_001_03()
    {
        var (service, store, phones) = Build();
        var account = ExistingAccount(phones, profileComplete: true);
        store.Users.Add(account);

        var first = await service.ExchangeAsync(_request, CancellationToken.None);
        var second = await service.ExchangeAsync(_request, CancellationToken.None);

        first.Should().NotBeNull();
        first.IsNew.Should().BeFalse();
        first.ProfileComplete.Should().BeTrue("devuelve el perfilCompleto de la cuenta");
        first.Session.RefreshToken.Should().NotBe(first.Session.AccessToken).And.NotBeNullOrEmpty();
        store.Created.Should().BeEmpty("no se crean usuarios, consentimientos ni auditoría");
        store.Refreshes.Should().HaveCount(2, "cada canje agrega solo una huella de refresh");
        store.Refreshes.Select(r => r.UserId).Should().OnlyContain(id => id == account.Id);
        store.Refreshes.Select(r => r.FamilyId).Distinct().Should().HaveCount(2, "cada canje abre otra familia");
        var handler = new JsonWebTokenHandler();
        var firstJwt = handler.ReadJsonWebToken(first.Session.AccessToken);
        var secondJwt = handler.ReadJsonWebToken(second!.Session.AccessToken);
        firstJwt.GetClaim("sub").Value.Should().Be(account.Sub).And.Be(secondJwt.GetClaim("sub").Value);
        firstJwt.GetClaim("jti").Value.Should().NotBe(secondJwt.GetClaim("jti").Value);
    }

    [Fact]
    public async Task NewAccount_CreatesUserConsentsAuditAndRefreshFingerprint_TC_HU_001_04()
    {
        var (service, store, phones) = Build();

        var result = await service.ExchangeAsync(_request, CancellationToken.None);

        result.Should().NotBeNull();
        result.IsNew.Should().BeTrue();
        result.ProfileComplete.Should().BeFalse();
        var created = store.Created.Should().ContainSingle().Subject;
        created.Sub.Should().Be(UserIdDerivation.Derive(_uid).ToString("D"));
        created.CreatedAt.Should().Be(_now.UtcDateTime, "creadoEn sale del reloj inyectado");
        created.CorrelationId.Should().Be("correlation-1");
        created.TraceId.Should().Be("trace-1");
        created.DeviceId.Should().Be(_deviceId);
        created.PhoneHash.Should().Equal(phones.Hash(_phone).Value);
        created.PhoneCiphertext.Should().NotBeEmpty();
        created.PhoneCiphertext.AsSpan().IndexOf(Encoding.UTF8.GetBytes(_phone)).Should().Be(-1, "el celular no se guarda en claro");
        created.PhoneCiphertextVersion.Should().Be(1);
        created.PhoneHashVersion.Should().Be(1);
        created.Refresh.UserId.Should().Be(created.Id);
        created.Refresh.Fingerprint.Should().Equal(RefreshTokenFingerprint.Compute(result.Session.RefreshToken));
        created.Refresh.DeviceId.Should().Be(_deviceId);
        created.Refresh.ExpiresAt.Should().Be(_now.UtcDateTime.AddDays(30));
    }

    [Fact]
    public async Task SuspendedAccount_IsRejectedBeforeIssuingAnything_TC_HU_001_08()
    {
        var (service, store, phones) = Build();
        store.Users.Add(ExistingAccount(phones, UserState.Suspended));

        var first = await Assert.ThrowsAsync<DomainException>(() => service.ExchangeAsync(_request, CancellationToken.None));
        var second = await Assert.ThrowsAsync<DomainException>(() => service.ExchangeAsync(_request, CancellationToken.None));

        first.Kind.Should().Be(DomainErrorKind.Forbidden);
        first.Code.Should().Be("account_suspended");
        second.Code.Should().Be("account_suspended", "repetirlo da lo mismo");
        store.Refreshes.Should().BeEmpty("no se guarda ninguna huella de refresh");
        store.Created.Should().BeEmpty();
        first.Message.Should().NotContain(_phone).And.NotContain(store.Users[0].Sub);
    }

    [Fact]
    public async Task PhoneLinkedToAnotherSubOrSubWithAnotherPhone_Returns409_TC_HU_001_11()
    {
        // (a) mismo celular con otro uid; (b) mismo uid con otro celular.
        var (service, store, phones) = Build(new FirebaseIdentity("uid-prueba-otro", _phone));
        store.Users.Add(ExistingAccount(phones));
        var (serviceB, storeB, phonesB) = Build(new FirebaseIdentity(_uid, _otherPhone));
        storeB.Users.Add(ExistingAccount(phonesB));

        var a = await Assert.ThrowsAsync<DomainException>(() => service.ExchangeAsync(_request, CancellationToken.None));
        var b = await Assert.ThrowsAsync<DomainException>(() => serviceB.ExchangeAsync(_request, CancellationToken.None));

        foreach (var error in new[] { a, b })
        {
            error.Kind.Should().Be(DomainErrorKind.Conflict);
            error.Code.Should().Be("account_conflict");
            error.Message.Should().NotContain(store.Users[0].Sub, "no revela la otra cuenta");
        }

        store.Created.Should().BeEmpty();
        store.Refreshes.Should().BeEmpty();
        storeB.Created.Should().BeEmpty();
        storeB.Refreshes.Should().BeEmpty();
    }

    [Fact]
    public async Task LosingTheCreationRace_ReturnsTheExistingAccount_TC_HU_001_07()
    {
        var (service, store, phones) = Build();
        store.WinnerOfRace = ExistingAccount(phones);

        var result = await service.ExchangeAsync(_request, CancellationToken.None);

        result.Should().NotBeNull();
        result.IsNew.Should().BeFalse("el perdedor de la carrera recibe 200, no un error");
        store.Created.Should().BeEmpty();
        store.Refreshes.Should().ContainSingle().Which.UserId.Should().Be("665f1b2c3d4e5f6071829304");
    }

    [Fact]
    public async Task RejectedToken_ReturnsNullAndTouchesNothing_TC_HU_001_03()
    {
        var (service, store, _) = Build(rejected: true);

        var result = await service.ExchangeAsync(_request, CancellationToken.None);

        result.Should().BeNull();
        store.Users.Should().BeEmpty();
        store.Refreshes.Should().BeEmpty();
    }

    [Fact]
    public void FirebaseIdentityToString_DoesNotExposeUidOrPhone_TC_HU_001_19()
    {
        var text = new FirebaseIdentity(_uid, _phone).ToString();

        text.Should().NotContain(_uid).And.NotContain(_phone).And.NotContain("5512345678");
    }
}
