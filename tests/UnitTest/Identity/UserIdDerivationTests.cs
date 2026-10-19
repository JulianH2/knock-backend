using AwesomeAssertions;
using Knock.Api.Modules.Identity.Services;
using Xunit;

namespace Knock.UnitTest.Identity;

public class UserIdDerivationTests
{
    // Valores esperados calculados con una implementación independiente: Python `uuid.uuid5(NAMESPACE, uid)`,
    // con NAMESPACE = bcfea58b-95dd-4c0e-80d1-baf839f2e36e. Los uid son ficticios.
    [Theory]
    [InlineData("uid-prueba-0001", "60772198-f50f-584a-8da8-cb0e1d8a47a9")]
    [InlineData("uid-prueba-0002", "1f35014e-49ed-5e08-9b83-f6e16e865909")]
    [InlineData("wVSEsCC4m04ROIgLCrHUGoxMyjn5", "4cab69ee-bb7b-55b7-abc9-b967d5f9200f")]
    [InlineData("nándú-π", "523bee67-06f2-5f1f-ae93-849482e19dce")]
    // Frontera del relleno de SHA-1: 16 + 39 = 55 bytes caben en un bloque; 16 + 40 = 56 ya usan dos.
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz", "23029c41-83b4-53da-8214-ca29a784eeea")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz", "dc82fae4-d4ae-5d8f-b919-44ddd33ac24a")]
    [InlineData("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx", "29aa6422-0059-5b42-998a-300594727f4a")]
    public void KnownVector_MatchesGuidV5_TC_TT_API_02_10(string uid, string expected)
    {
        var derived = UserIdDerivation.Derive(uid);

        derived.Should().Be(Guid.Parse(expected));
    }

    [Fact]
    public void Namespace_IsTheFixedKnockValue_TC_TT_API_02_10()
    {
        UserIdDerivation.Namespace.Should().Be(Guid.Parse("bcfea58b-95dd-4c0e-80d1-baf839f2e36e"));
    }

    [Fact]
    public void DifferentUids_GiveDifferentGuids_AndSameUidIsStable_TC_TT_API_02_10()
    {
        var first = UserIdDerivation.Derive("uid-prueba-0001");
        var second = UserIdDerivation.Derive("uid-prueba-0002");

        first.Should().NotBe(second);
        UserIdDerivation.Derive("uid-prueba-0001").Should().Be(first);
    }

    [Fact]
    public void Derive_SetsVersion5AndRfcVariant_TC_TT_API_02_10()
    {
        var text = UserIdDerivation.Derive("uid-prueba-0001").ToString("D");

        text[14].Should().Be('5');
        "89ab".Should().Contain(text[19].ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Derive_WithEmptyUid_Throws_TC_TT_API_02_10(string uid)
    {
        var act = () => UserIdDerivation.Derive(uid);

        act.Should().Throw<ArgumentException>();
    }
}
