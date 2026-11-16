using AwesomeAssertions;
using Knock.Contracts.Moderation;
using Xunit;

namespace Knock.UnitTest.Contracts;

/// <summary>Spec 013, R-05: el moderador falla cerrado (Unavailable es el valor 0 y por defecto).</summary>
public class ModerationOutcomeTests
{
    [Fact]
    public void Default_IsUnavailable_OI1_04()
    {
        default(ModerationOutcome).Should().Be(ModerationOutcome.Unavailable);
        ((int)ModerationOutcome.Unavailable).Should().Be(0);
        ((int)ModerationOutcome.Allowed).Should().Be(1);
        ((int)ModerationOutcome.Rejected).Should().Be(2);
    }
}
