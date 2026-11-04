using System.Security.Cryptography;
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

/// <summary>HU-003 y HU-004: reglas de la renovación y del cierre con un doble del almacén (la base real se prueba en Database e Integration).</summary>
public class RefreshSessionServiceTests
{
    private const string _deviceId = "3f2b8c1e-5a7d-4e9b-8c3a-1d2e3f4a5b6c";
    private const string _otherDeviceId = "9a8b7c6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d";
    private const string _sub = "665f1b2c-3d4e-8f60-b182-930400000001";
    private const string _userId = "665f1b2c3d4e5f6071829304";
    private const string _familyId = "665f1b2c3d4e5f6071829399";

    private static readonly DateTimeOffset _now = new(2026, 11, 3, 12, 0, 0, TimeSpan.Zero);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeStore : IRefreshSessionStore
    {
        public RefreshRotation Rotation { get; set; } = RefreshRotation.Missing;

        public UserAccount? Owner { get; set; }

        public List<(string FamilyId, string Reason)> Revoked { get; } = [];

        public StoredRefresh? Session { get; set; }

        public int AccountReads { get; private set; }

        public Task<RefreshRotation> RotateAsync(byte[] fingerprint, byte[] nextFingerprint, DateTime now, CancellationToken cancellationToken) =>
            Task.FromResult(Rotation);

        public Task RevokeFamilyAsync(string familyId, string reason, DateTime now, CancellationToken cancellationToken)
        {
            Revoked.Add((familyId, reason));
            return Task.CompletedTask;
        }

        public Task<StoredRefresh?> FindByFingerprintAsync(byte[] fingerprint, CancellationToken cancellationToken) => Task.FromResult(Session);

        public Task<UserAccount?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            AccountReads++;
            return Task.FromResult(Owner);
        }
    }

    private sealed class FakeRevocations(bool enabled) : IAccessTokenRevocations
    {
        public bool Enabled => enabled;

        public List<(string Jti, TimeSpan Ttl)> Revoked { get; } = [];

        public Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task RevokeAsync(string jti, TimeSpan timeToLive, CancellationToken cancellationToken)
        {
            Revoked.Add((jti, timeToLive));
            return Task.CompletedTask;
        }
    }

    private static string Token() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));

    private static SessionTokenIssuer Issuer() =>
        new(new JwtSettings("knock-api", "knock-app", new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32))), new FixedTimeProvider(_now));

    private static UserAccount Account(string state = UserState.Active, bool profileComplete = false) => new(_userId, _sub, state, profileComplete, new byte[32]);

    private static (RefreshSessionService Service, FakeStore Store) BuildRefresh(FakeStore store) =>
        (new RefreshSessionService(Issuer(), store, new FixedTimeProvider(_now), NullLogger<RefreshSessionService>.Instance), store);

    private static FakeStore RotatedStore(UserAccount? account = null) => new()
    {
        Rotation = RefreshRotation.Rotated(new StoredRefresh(_userId, _familyId, _deviceId)),
        Owner = account ?? Account(),
    };

    [Fact]
    public async Task ValidRotation_IssuesNewPairForTheSameSub_TC_HU_003_01()
    {
        var (service, store) = BuildRefresh(RotatedStore(Account(profileComplete: true)));

        var result = await service.RenewAsync(new RefreshCommand(Token(), _deviceId), CancellationToken.None);

        result.Should().NotBeNull();
        result.ProfileComplete.Should().BeTrue();
        new JsonWebTokenHandler().ReadJsonWebToken(result.Session.AccessToken).GetClaim("sub").Value.Should().Be(_sub);
        RefreshTokenFingerprint.Compute(result.Session.RefreshToken).Should().HaveCount(32);
        store.Revoked.Should().BeEmpty();
    }

    [Fact]
    public async Task ReusedToken_Returns401AndNeverReadsTheAccount_TC_HU_003_03()
    {
        var (service, store) = BuildRefresh(new FakeStore { Rotation = RefreshRotation.Reused, Owner = Account() });

        var result = await service.RenewAsync(new RefreshCommand(Token(), _deviceId), CancellationToken.None);

        result.Should().BeNull();
        store.AccountReads.Should().Be(0, "los rechazos 401 ocurren antes de consultar cuentas");
    }

    [Fact]
    public async Task MissingToken_Returns401AndTouchesNothing_TC_HU_003_05()
    {
        var (service, store) = BuildRefresh(new FakeStore { Rotation = RefreshRotation.Missing });

        var result = await service.RenewAsync(new RefreshCommand(Token(), _deviceId), CancellationToken.None);

        result.Should().BeNull();
        store.Revoked.Should().BeEmpty();
        store.AccountReads.Should().Be(0);
    }

    [Fact]
    public async Task OtherDevice_Returns401AndRevokesTheFamily_TC_HU_003_06()
    {
        var (service, store) = BuildRefresh(RotatedStore());

        var result = await service.RenewAsync(new RefreshCommand(Token(), _otherDeviceId), CancellationToken.None);

        result.Should().BeNull();
        store.Revoked.Should().Equal((_familyId, "dispositivo"));
        store.AccountReads.Should().Be(0);
    }

    [Fact]
    public async Task SuspendedAccount_Throws403AndRevokesTheFamily_TC_HU_003_07()
    {
        var (service, store) = BuildRefresh(RotatedStore(Account(UserState.Suspended)));

        var act = () => service.RenewAsync(new RefreshCommand(Token(), _deviceId), CancellationToken.None);

        var thrown = (await act.Should().ThrowAsync<DomainException>()).Which;
        thrown.Kind.Should().Be(DomainErrorKind.Forbidden);
        thrown.Code.Should().Be(FirebaseSessionService.AccountSuspendedCode);
        store.Revoked.Should().Equal((_familyId, "suspension"));
    }

    private static LogoutService BuildLogout(FakeStore store, FakeRevocations revocations) =>
        new(store, revocations, new FixedTimeProvider(_now), NullLogger<LogoutService>.Instance);

    private static AccessTokenClaims Claims(string sub = _sub) => new(sub, "jti-de-prueba", _now.AddMinutes(10));

    [Fact]
    public async Task Logout_RevokesTheFamilyEvenWithoutAccessToken_TC_HU_004_01()
    {
        var store = new FakeStore { Session = new StoredRefresh(_userId, _familyId, _deviceId), Owner = Account() };

        await BuildLogout(store, new FakeRevocations(false)).LogoutAsync(Token(), null, CancellationToken.None);

        store.Revoked.Should().Equal((_familyId, "logout"));
    }

    [Fact]
    public async Task Logout_UnknownToken_WritesNothing_TC_HU_004_04()
    {
        var store = new FakeStore { Session = null };
        var revocations = new FakeRevocations(true);

        await BuildLogout(store, revocations).LogoutAsync(Token(), Claims(), CancellationToken.None);

        store.Revoked.Should().BeEmpty();
        revocations.Revoked.Should().BeEmpty();
    }

    [Fact]
    public async Task Logout_AccessTokenOfAnotherPerson_RevokesNothing_TC_HU_004_07()
    {
        var store = new FakeStore { Session = new StoredRefresh(_userId, _familyId, _deviceId), Owner = Account() };
        var revocations = new FakeRevocations(true);

        await BuildLogout(store, revocations).LogoutAsync(Token(), Claims("00000000-0000-8000-8000-000000000999"), CancellationToken.None);

        store.Revoked.Should().BeEmpty();
        revocations.Revoked.Should().BeEmpty();
    }

    [Fact]
    public async Task Logout_SamePersonWithRevocations_StoresTheJtiForItsRemainingLife_TC_HU_004_08()
    {
        var store = new FakeStore { Session = new StoredRefresh(_userId, _familyId, _deviceId), Owner = Account() };
        var revocations = new FakeRevocations(true);

        await BuildLogout(store, revocations).LogoutAsync(Token(), Claims(), CancellationToken.None);

        store.Revoked.Should().Equal((_familyId, "logout"));
        revocations.Revoked.Should().Equal(("jti-de-prueba", TimeSpan.FromMinutes(10)));
    }

    [Fact]
    public async Task Logout_WithoutRevocations_DoesNotStoreTheJti_TC_HU_004_09()
    {
        var store = new FakeStore { Session = new StoredRefresh(_userId, _familyId, _deviceId), Owner = Account() };
        var revocations = new FakeRevocations(false);

        await BuildLogout(store, revocations).LogoutAsync(Token(), Claims(), CancellationToken.None);

        store.Revoked.Should().Equal((_familyId, "logout"));
        revocations.Revoked.Should().BeEmpty();
    }
}
