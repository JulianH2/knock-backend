using AwesomeAssertions;
using Knock.Api.Modules.Identity.Dtos;
using Knock.Api.Modules.Identity.Validators;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Knock.UnitTest.Identity;

public class LogoutRequestValidatorTests
{
    private static readonly LogoutRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_HasNoErrors_TC_HU_004_05()
    {
        _validator.Validate(new LogoutRequest(Base64UrlEncoder.Encode(new byte[32]))).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("corto")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA+")]
    public void InvalidToken_FailsOnItsOwnField_TC_HU_004_05(string? token)
    {
        _validator.Validate(new LogoutRequest(token)).Errors.Select(e => e.PropertyName).Distinct().Should().Equal("RefreshToken");
    }

    [Fact]
    public void Message_NeverRepeatsTheSentValue_TC_HU_004_05()
    {
        const string sent = "valor-secreto-que-no-debe-volver";

        _validator.Validate(new LogoutRequest(sent)).Errors.Select(e => e.ErrorMessage).Should().NotContain(m => m.Contains(sent, StringComparison.Ordinal));
    }
}
