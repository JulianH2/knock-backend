using AwesomeAssertions;
using Knock.Identity.Api.Common.Errors;
using Knock.Identity.Api.Modules.Account.Domain;
using Knock.Identity.Api.Modules.Account.Dtos;
using Knock.Identity.Api.Modules.Account.Services;
using Knock.Identity.Api.Modules.Account.Validators;
using Knock.Identity.Api.Modules.Sesiones.Domain;
using Knock.Identity.Api.Modules.Sesiones.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Knock.UnitTest.Identity;

/// <summary>Spec 039, USR-03 (T-02): el cuerpo y la regla de «el número ya tiene cuenta». Sin MongoDB.</summary>
public class Spec039PhoneAvailabilityTests
{
    private const string _taken = "+525512345678";
    private const string _free = "+525598765432";

    private static readonly PhoneAvailabilityRequestValidator _validator = new();

    private static (PhoneChangeService Service, FakeAccountStore Store, PhoneProtector Phones) Build()
    {
        var phones = new PhoneProtector(PhoneProtectionTestKeys.Keys());
        var store = new FakeAccountStore();
        store.Accounts.Add(new AccountSnapshot("665f1b2c3d4e5f6071829304", "f0000000-0000-8000-8000-000000000001", AccountState.Active, false, phones.Hash(_taken).Value, null));
        return (new PhoneChangeService(store, phones, NullLogger<PhoneChangeService>.Instance), store, phones);
    }

    [Fact]
    public async Task FreeNumber_Completes_CA_039_01()
    {
        var (service, _, _) = Build();

        var act = () => service.EnsureAvailableAsync(_free, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task NumberOfAnAccount_IsRefusedWithPhoneAlreadyRegistered_CA_039_05()
    {
        var (service, _, _) = Build();

        var act = () => service.EnsureAvailableAsync(_taken, CancellationToken.None);

        var error = (await act.Should().ThrowAsync<DomainException>()).Which;
        error.Kind.Should().Be(DomainErrorKind.Conflict);
        error.Code.Should().Be("phone_already_registered");
        error.Message.Should().NotContain(_taken);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("5512345678")]
    [InlineData("+15512345678")]
    [InlineData("+52551234567")]
    [InlineData("+5255123456789")]
    [InlineData("+52551234567a")]
    [InlineData("+52551234567\n")]
    public void InvalidNumber_FailsOnItsOwnFieldWithoutRepeatingTheValue_CA_039_03(string? phone)
    {
        var result = _validator.Validate(new PhoneAvailabilityRequest(phone));

        result.Errors.Select(e => e.PropertyName).Distinct().Should().Equal("CelularNuevo");
        result.Errors.Select(e => e.ErrorMessage).Should().OnlyContain(message => phone == null || phone.Length == 0 || !message.Contains(phone, StringComparison.Ordinal));
    }

    [Fact]
    public void MexicanE164Number_IsValid_CA_039_01()
    {
        _validator.Validate(new PhoneAvailabilityRequest(_free)).IsValid.Should().BeTrue();
    }
}
