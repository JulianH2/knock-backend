using AwesomeAssertions;
using Knock.Identity.Api.Common.Errors;
using Knock.Identity.Api.Modules.Account.Domain;
using Knock.Identity.Api.Modules.Account.Dtos;
using Knock.Identity.Api.Modules.Account.Services;
using Knock.Identity.Api.Modules.Account.Validators;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Knock.UnitTest.Identity;

/// <summary>Spec 039, USR-07 y USR-08 (T-06 y T-07): lista de consentimientos y cambio de los opcionales. Sin MongoDB.</summary>
public class Spec039ConsentTests
{
    private const string _sub = "f0000000-0000-8000-8000-000000000001";

    private static readonly DateTime _signedUp = new(2026, 11, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTimeOffset _now = new(2026, 11, 17, 12, 0, 0, TimeSpan.Zero);
    private static readonly UpdateConsentRequestValidator _validator = new();

    private static (ConsentService Service, FakeAccountStore Store) Build()
    {
        var store = new FakeAccountStore();
        store.Consents[_sub] =
        [
            new StoredConsent("aviso_privacidad", "1.0", _signedUp, null),
            new StoredConsent("terminos", "1.0", _signedUp, null),
        ];
        var service = new ConsentService(store, Options.Create(new OptionalConsentOptions { Opcionales = "1.1" }), new Spec039Clock(_now), NullLogger<ConsentService>.Instance);
        return (service, store);
    }

    [Fact]
    public async Task List_ReturnsTheTwoMandatoryAndTheThreeOptionalInAFixedOrder_CA_039_17()
    {
        var (service, _) = Build();

        var entries = await service.ListAsync(_sub, CancellationToken.None);

        entries.Select(e => e.Type).Should().Equal("aviso_privacidad", "terminos", "promociones", "encuestas", "recomendaciones");
        entries.Take(2).Should().OnlyContain(e => e.Mandatory && e.Active && e.Version == "1.0" && e.ChangedAt == _signedUp);
        entries.Skip(2).Should().OnlyContain(e => !e.Mandatory && !e.Active && e.Version == null && e.ChangedAt == null, "nunca activó un opcional");
    }

    [Fact]
    public async Task List_OfAnAccountThatNoLongerExists_Is404()
    {
        var (service, _) = Build();

        var act = () => service.ListAsync("f0000000-0000-8000-8000-0000000000ff", CancellationToken.None);

        (await act.Should().ThrowAsync<DomainException>()).Which.Kind.Should().Be(DomainErrorKind.NotFound);
    }

    [Fact]
    public async Task Withdrawing_Promotions_LeavesVigenteFalseWithDateAndVersion_CA_039_36()
    {
        var (service, _) = Build();
        await service.SetAsync(_sub, "promociones", true, CancellationToken.None);

        var entry = await service.SetAsync(_sub, "promociones", false, CancellationToken.None);

        entry.Should().Be(new ConsentEntry("promociones", false, false, "1.1", _now.UtcDateTime));
        var listed = (await service.ListAsync(_sub, CancellationToken.None)).Single(e => e.Type == "promociones");
        listed.Active.Should().BeFalse("retirar surte efecto de inmediato");
        listed.Version.Should().Be("1.1");
    }

    [Fact]
    public async Task Activating_AnOptional_ChangesOnlyThatOne_CA_039_19()
    {
        var (service, _) = Build();

        var entry = await service.SetAsync(_sub, "encuestas", true, CancellationToken.None);

        entry.Active.Should().BeTrue();
        var entries = await service.ListAsync(_sub, CancellationToken.None);
        entries.Where(e => e.Active).Select(e => e.Type).Should().BeEquivalentTo("aviso_privacidad", "terminos", "encuestas");
    }

    [Theory]
    [InlineData("aviso_privacidad")]
    [InlineData("terminos")]
    [InlineData("otro")]
    public async Task AMandatoryOrUnknownType_IsRefusedByTheService_CA_039_36(string type)
    {
        var (service, store) = Build();

        var act = () => service.SetAsync(_sub, type, false, CancellationToken.None);

        (await act.Should().ThrowAsync<DomainException>()).Which.Kind.Should().Be(DomainErrorKind.Unprocessable);
        store.Consents[_sub].Should().HaveCount(2, "no se guardó nada");
    }

    [Theory]
    [InlineData("promociones")]
    [InlineData("encuestas")]
    [InlineData("recomendaciones")]
    public void OptionalTypeWithVigente_IsValid_CA_039_19(string type)
    {
        _validator.Validate(new UpdateConsentRequest(type, false)).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, true, "Tipo")]
    [InlineData("", true, "Tipo")]
    [InlineData("aviso_privacidad", false, "Tipo")]
    [InlineData("terminos", false, "Tipo")]
    [InlineData("PROMOCIONES", true, "Tipo")]
    [InlineData("marketing", true, "Tipo")]
    [InlineData("promociones", null, "Vigente")]
    public void InvalidBody_FailsOnItsOwnFieldWithoutRepeatingTheType_CA_039_21(string? type, bool? active, string field)
    {
        var errors = _validator.Validate(new UpdateConsentRequest(type, active)).Errors;

        errors.Select(e => e.PropertyName).Distinct().Should().Equal(field);
        errors.Should().OnlyContain(e => string.IsNullOrEmpty(type) || !e.ErrorMessage.Contains(type, StringComparison.Ordinal));
    }
}
