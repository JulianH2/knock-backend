using AwesomeAssertions;
using Knock.Api.Modules.Identity.Domain;
using Xunit;

namespace Knock.UnitTest.Identity;

public class PhoneNumberPolicyTests
{
    [Theory]
    [InlineData("+525512345678")]
    [InlineData("+520000000000")]
    [InlineData("+529999999999")]
    public void OnlyMexicanE164NumbersAreAccepted_TC_HU_001_12(string phone)
    {
        PhoneNumberPolicy.IsValid(phone).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("5512345678")]
    [InlineData("525512345678")]
    [InlineData("+15555550199")]
    [InlineData("+345512345678")]
    [InlineData("+52 5512345678")]
    [InlineData("+52-55-1234-5678")]
    [InlineData("+52551234567")]
    [InlineData("+5255123456789")]
    [InlineData("+52551234567a")]
    [InlineData("+52+512345678")]
    [InlineData(" +525512345678")]
    [InlineData("+525512345678 ")]
    [InlineData("+525512345678\n")]
    [InlineData("+52٥٥١٢٣٤٥٦٧٨")]
    [InlineData("＋525512345678")]
    public void MalformedOrNonMexicanNumbersAreRejected_TC_HU_001_12(string? phone)
    {
        PhoneNumberPolicy.IsValid(phone).Should().BeFalse();
    }
}
