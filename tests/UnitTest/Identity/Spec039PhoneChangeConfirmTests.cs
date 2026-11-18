using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.Identity.Api.Common.Errors;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Knock.UnitTest.Identity;

/// <summary>Spec 039, USR-04 (T-03): validación del token del número nuevo, cambio protegido del celular y cierre de las demás sesiones. Sin MongoDB.</summary>
public class Spec039PhoneChangeConfirmTests
{
    private const string _oldPhone = "+525512345678";
    private const string _newPhone = "+525598765432";
    private const string _otherPhone = "+525511112222";
    private const string _userId = "665f1b2c3d4e5f6071829304";
    private const string _sub = "f0000000-0000-8000-8000-000000000001";
    private const string _current = "665f1b2c3d4e5f6071829a01";
    private const string _other1 = "665f1b2c3d4e5f6071829a02";
    private const string _other2 = "665f1b2c3d4e5f6071829a03";

    private static readonly ConfirmPhoneChangeRequestValidator _validator = new();

    private sealed record Rig(
        PhoneChangeService Service,
        FakeAccountStore Store,
        PhoneProtector Phones,
        Spec039Verifier Verifier,
        Spec039Cancelled Cancelled,
        Spec039Outbox Outbox);

    private static Rig Build(string newPhone = _newPhone, bool rejectedToken = false)
    {
        var phones = new PhoneProtector(PhoneProtectionTestKeys.Keys());
        var store = new FakeAccountStore();
        store.Accounts.Add(new AccountSnapshot(_userId, _sub, AccountState.Active, false, phones.Hash(_oldPhone).Value, null));
        store.Accounts.Add(new AccountSnapshot("665f1b2c3d4e5f6071829399", "f0000000-0000-8000-8000-000000000002", AccountState.Active, false, phones.Hash(_otherPhone).Value, null));
        foreach (var id in new[] { _current, _other1, _other2 })
        {
            store.Sessions.Add(new FakeAccountSession(id, _userId));
        }

        var verifier = new Spec039Verifier(rejectedToken ? null : new FirebaseIdentity("uid-prueba-0001", newPhone));
        var cancelled = new Spec039Cancelled();
        var outbox = new Spec039Outbox();
        var service = new PhoneChangeService(
            store, verifier, phones, cancelled, outbox, new Spec039Clock(new DateTimeOffset(2026, 11, 17, 12, 0, 0, TimeSpan.Zero)), NullLogger<PhoneChangeService>.Instance);
        return new Rig(service, store, phones, verifier, cancelled, outbox);
    }

    [Fact]
    public async Task ValidToken_ChangesThePhoneKeepsTheCurrentSessionAndClosesTheOthers_CA_039_06()
    {
        var rig = Build();

        var changed = await rig.Service.ConfirmAsync(_sub, _current, "synthetic-token", CancellationToken.None);

        changed.Should().BeTrue();
        rig.Store.Accounts.Single(a => a.Id == _userId).PhoneHash.Should().Equal(rig.Phones.Hash(_newPhone).Value);
        rig.Store.Sessions.Where(s => s.Revoked).Select(s => s.Id).Should().BeEquivalentTo(_other1, _other2);
        rig.Store.Sessions.Single(s => s.Id == _current).Revoked.Should().BeFalse();
        rig.Cancelled.Cancelled.Select(c => c.SessionId).Should().BeEquivalentTo(_other1, _other2);
        rig.Cancelled.Cancelled.Should().OnlyContain(c => c.Lifetime == SessionTokenIssuer.AccessTokenLifetime);
    }

    [Fact]
    public async Task TheNewPhone_IsStoredProtectedAsInTheRegistration_CA_039_06()
    {
        var rig = Build();

        await rig.Service.ConfirmAsync(_sub, _current, "synthetic-token", CancellationToken.None);

        var (phone, hash) = rig.Store.StoredPhones[_userId];
        // Cifrado AES-256-GCM ligado al _id del usuario: nonce (12) + texto + etiqueta (16); el texto claro no aparece.
        phone.Ciphertext.Length.Should().Be(12 + _newPhone.Length + 16);
        phone.Ciphertext.AsSpan().IndexOf(System.Text.Encoding.UTF8.GetBytes(_newPhone)).Should().Be(-1);
        hash.Value.Should().Equal(rig.Phones.Hash(_newPhone).Value);
    }

    [Fact]
    public async Task EachOtherSession_GetsTheRevokedNoticeBeforeItIsClosed_CA_039_06()
    {
        var rig = Build();

        await rig.Service.ConfirmAsync(_sub, _current, "synthetic-token", CancellationToken.None);

        rig.Outbox.RevokedNotices.Should().BeEquivalentTo(_other1, _other2);
        rig.Outbox.RevokedNotices.Should().NotContain(_current);
    }

    [Fact]
    public async Task TokenFirebaseRejects_ChangesNothingAndReportsIt_CA_039_08()
    {
        var rig = Build(rejectedToken: true);

        var changed = await rig.Service.ConfirmAsync(_sub, _current, "synthetic-token", CancellationToken.None);

        changed.Should().BeFalse();
        rig.Store.Accounts.Single(a => a.Id == _userId).PhoneHash.Should().Equal(rig.Phones.Hash(_oldPhone).Value);
        rig.Store.Sessions.Should().OnlyContain(s => !s.Revoked);
        rig.Outbox.RevokedNotices.Should().BeEmpty();
        rig.Cancelled.Cancelled.Should().BeEmpty();
    }

    [Theory]
    [InlineData(_otherPhone)]
    [InlineData(_oldPhone)]
    public async Task PhoneOfAnAccount_Is409PhoneAlreadyRegisteredAndChangesNothing_CA_039_10(string phone)
    {
        var rig = Build(phone);

        var act = () => rig.Service.ConfirmAsync(_sub, _current, "synthetic-token", CancellationToken.None);

        var error = (await act.Should().ThrowAsync<DomainException>()).Which;
        error.Kind.Should().Be(DomainErrorKind.Conflict);
        error.Code.Should().Be("phone_already_registered");
        rig.Store.Sessions.Should().OnlyContain(s => !s.Revoked);
        rig.Outbox.RevokedNotices.Should().BeEmpty();
        rig.Cancelled.Cancelled.Should().BeEmpty();
    }

    [Fact]
    public async Task UniqueIndexRace_Is409AndDoesNotCloseSessions_CA_039_10()
    {
        var rig = Build();
        rig.Store.PhoneTakenOnChange = true;

        var act = () => rig.Service.ConfirmAsync(_sub, _current, "synthetic-token", CancellationToken.None);

        (await act.Should().ThrowAsync<DomainException>()).Which.Code.Should().Be("phone_already_registered");
        rig.Cancelled.Cancelled.Should().BeEmpty("no se cierra nada si el cambio no se guardó");
    }

    [Fact]
    public async Task AccountOfTheTokenThatNoLongerExists_Is404()
    {
        var rig = Build();

        var act = () => rig.Service.ConfirmAsync("f0000000-0000-8000-8000-0000000000ff", _current, "synthetic-token", CancellationToken.None);

        (await act.Should().ThrowAsync<DomainException>()).Which.Kind.Should().Be(DomainErrorKind.NotFound);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingToken_FailsOnItsOwnField_CA_039_08(string? token)
    {
        _validator.Validate(new ConfirmPhoneChangeRequest(token)).Errors.Select(e => e.PropertyName).Distinct().Should().Equal("FirebaseIdToken");
    }

    [Fact]
    public void TokenOver4096_FailsAndDoesNotRepeatTheValue_CA_039_08()
    {
        var token = RandomNumberGenerator.GetHexString(4097);

        var errors = _validator.Validate(new ConfirmPhoneChangeRequest(token)).Errors;

        errors.Select(e => e.PropertyName).Distinct().Should().Equal("FirebaseIdToken");
        errors.Should().OnlyContain(e => !e.ErrorMessage.Contains(token, StringComparison.Ordinal));
    }

    [Fact]
    public void TokenOfNormalSize_IsValid_CA_039_06()
    {
        _validator.Validate(new ConfirmPhoneChangeRequest(new string('a', 4096))).IsValid.Should().BeTrue();
    }
}
