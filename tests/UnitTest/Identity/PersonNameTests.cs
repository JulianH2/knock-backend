using AwesomeAssertions;
using Knock.Api.Modules.Identity.Domain;
using Xunit;

namespace Knock.UnitTest.Identity;

public class PersonNameTests
{
    [Theory]
    [InlineData("José Pérez", "José Pérez")]
    [InlineData("  José   Pérez ", "José Pérez")]
    [InlineData("José Pérez", "José Pérez")]
    [InlineData("José Pérez", "José Pérez")]
    [InlineData("María-José", "María-José")]
    public void NormalizesWhitespaceAndForm_TC_HU_002_02(string input, string expected)
    {
        PersonName.TryNormalize(input, out var normalized).Should().BeTrue();

        normalized.Should().Be(expected);
        normalized.IsNormalized(System.Text.NormalizationForm.FormC).Should().BeTrue();
    }

    [Fact]
    public void NormalizationIsStable_TC_HU_002_02()
    {
        PersonName.TryNormalize("  José   Pérez ", out var first).Should().BeTrue();

        PersonName.TryNormalize(first, out var second).Should().BeTrue();

        second.Should().Be(first);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyOrBlank_IsRejected_TC_HU_002_02(string? input)
    {
        PersonName.TryNormalize(input, out var normalized).Should().BeFalse();

        normalized.Should().BeEmpty();
    }
}
