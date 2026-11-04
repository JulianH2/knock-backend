using AwesomeAssertions;
using Knock.Api.Modules.Identity.Dtos;
using Knock.Api.Modules.Identity.Validators;
using Xunit;

namespace Knock.UnitTest.Identity;

public class FirebaseExchangeRequestValidatorTests
{
    private const string _validDevice = "3f2b8c1e-5a7d-4e9b-8c3a-1d2e3f4a5b6c";

    private static readonly FirebaseExchangeRequestValidator _validator = new();

    private static IEnumerable<string> FieldsWithErrors(FirebaseExchangeRequest request) =>
        _validator.Validate(request).Errors.Select(e => e.PropertyName).Distinct();

    [Fact]
    public void ValidRequest_HasNoErrors_TC_HU_001_21()
    {
        _validator.Validate(new FirebaseExchangeRequest(new string('a', 4096), _validDevice, true)).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, "FirebaseIdToken")]
    [InlineData("", "FirebaseIdToken")]
    [InlineData("   ", "FirebaseIdToken")]
    public void MissingToken_FailsOnItsOwnField_TC_HU_001_21(string? token, string field)
    {
        FieldsWithErrors(new FirebaseExchangeRequest(token, _validDevice, true)).Should().Equal(field);
    }

    [Fact]
    public void TokenOver4096_FailsOnItsOwnField_TC_HU_001_21()
    {
        FieldsWithErrors(new FirebaseExchangeRequest(new string('a', 4097), _validDevice, true)).Should().Equal("FirebaseIdToken");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-uuid")]
    [InlineData("3f2b8c1e5a7d4e9b8c3a1d2e3f4a5b6c")]
    [InlineData("{3f2b8c1e-5a7d-4e9b-8c3a-1d2e3f4a5b6c}")]
    [InlineData("3f2b8c1e-5a7d-4e9b-8c3a-1d2e3f4a5b6c ")]
    [InlineData("3f2b8c1e-5a7d-4e9b-8c3a-1d2e3f4a5b6z")]
    public void InvalidDeviceId_FailsOnItsOwnField_TC_HU_001_21(string? deviceId)
    {
        FieldsWithErrors(new FirebaseExchangeRequest("token", deviceId, true)).Should().Equal("DeviceId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(false)]
    public void NoticeNotAccepted_FailsOnItsOwnField_TC_HU_001_21(bool? accepted)
    {
        FieldsWithErrors(new FirebaseExchangeRequest("token", _validDevice, accepted)).Should().Equal("AceptaAviso");
    }

    [Fact]
    public void EveryFieldInvalid_ReportsEachOne_TC_HU_001_21()
    {
        FieldsWithErrors(new FirebaseExchangeRequest(null, null, null)).Should().BeEquivalentTo("FirebaseIdToken", "DeviceId", "AceptaAviso");
    }
}
