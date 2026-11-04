using AwesomeAssertions;
using Knock.Api.Modules.Identity.Dtos;
using Knock.Api.Modules.Identity.Validators;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Knock.UnitTest.Identity;

public class RefreshSessionRequestValidatorTests
{
    private const string _validDevice = "3f2b8c1e-5a7d-4e9b-8c3a-1d2e3f4a5b6c";

    private static readonly RefreshSessionRequestValidator _validator = new();

    private static string ValidToken() => Base64UrlEncoder.Encode(new byte[32]);

    private static IEnumerable<string> FieldsWithErrors(RefreshSessionRequest request) =>
        _validator.Validate(request).Errors.Select(e => e.PropertyName).Distinct();

    [Fact]
    public void ValidRequest_HasNoErrors_TC_HU_003_11()
    {
        _validator.Validate(new RefreshSessionRequest(ValidToken(), _validDevice)).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("corto")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA+")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    public void InvalidToken_FailsOnItsOwnField_TC_HU_003_11(string? token)
    {
        FieldsWithErrors(new RefreshSessionRequest(token, _validDevice)).Should().Equal("RefreshToken");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-uuid")]
    [InlineData("3f2b8c1e5a7d4e9b8c3a1d2e3f4a5b6c")]
    [InlineData("{3f2b8c1e-5a7d-4e9b-8c3a-1d2e3f4a5b6c}")]
    public void InvalidDeviceId_FailsOnItsOwnField_TC_HU_003_11(string? deviceId)
    {
        FieldsWithErrors(new RefreshSessionRequest(ValidToken(), deviceId)).Should().Equal("DeviceId");
    }

    [Fact]
    public void EveryFieldInvalid_ReportsEachOne_TC_HU_003_11()
    {
        FieldsWithErrors(new RefreshSessionRequest(null, null)).Should().BeEquivalentTo("RefreshToken", "DeviceId");
    }

    [Fact]
    public void Messages_NeverRepeatTheSentValue_TC_HU_003_11()
    {
        const string sent = "valor-secreto-que-no-debe-volver";

        var messages = _validator.Validate(new RefreshSessionRequest(sent, sent)).Errors.Select(e => e.ErrorMessage);

        messages.Should().NotContain(m => m.Contains(sent, StringComparison.Ordinal));
    }
}
