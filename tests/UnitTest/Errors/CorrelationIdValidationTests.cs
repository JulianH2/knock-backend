using AwesomeAssertions;
using Knock.Api.Hosting;
using Xunit;

namespace Knock.UnitTest.Errors;

public class CorrelationIdValidationTests
{
    [Theory]
    [InlineData("a")]
    [InlineData("abc-123_X.y")]
    [InlineData("0123456789abcdef0123456789abcdef")]
    public void ValidValue_IsAccepted_TC_TT_API_03_09(string value) => CorrelationId.IsValid(value).Should().BeTrue();

    [Fact]
    public void ValueOfExactlyMaxLength_IsAccepted_AndOneMore_IsRejected_TC_TT_API_03_09()
    {
        CorrelationId.IsValid(new string('a', 64)).Should().BeTrue();
        CorrelationId.IsValid(new string('a', 65)).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("with space")]
    [InlineData("line\r\nbreak")]
    [InlineData("tab\there")]
    [InlineData("{\"a\":1}")]
    [InlineData("a/b")]
    [InlineData("ñandú")]
    [InlineData("<script>")]
    [InlineData("a,b")]
    public void HostileOrMalformedValue_IsRejected_TC_TT_API_03_10(string? value) => CorrelationId.IsValid(value).Should().BeFalse();

    [Fact]
    public void Generated_IsGuidWithoutDashesAndDifferentEachTime_TC_TT_API_03_08()
    {
        var first = CorrelationId.Create();
        var second = CorrelationId.Create();

        first.Should().MatchRegex("^[0-9a-f]{32}$");
        CorrelationId.IsValid(first).Should().BeTrue();
        second.Should().NotBe(first);
    }
}
