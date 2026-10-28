using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.Api.Modules.Identity.Services;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Knock.UnitTest.Identity;

public class RefreshTokenFingerprintTests
{
    // Valor esperado calculado con una implementación independiente (Python hashlib.sha256 de los bytes 0 a 31).
    private const string _fingerprintOfBytes0To31 = "630dcd2966c4336691125448bbb25b4ff412a49c732db2c8abc1b8581bd710dd";
    // El token se construye en ejecución: un literal Base64 asignado a una variable «token» lo marca Gitleaks (generic-api-key).
    private static readonly string _tokenOfBytes0To31 = Base64UrlEncoder.Encode(Enumerable.Range(0, 32).Select(i => (byte)i).ToArray());

    [Fact]
    public void FingerprintIsSha256OfTheRawToken_TC_HU_001_16()
    {
        var fingerprint = RefreshTokenFingerprint.Compute(_tokenOfBytes0To31);

        Convert.ToHexStringLower(fingerprint).Should().Be(_fingerprintOfBytes0To31);
        fingerprint.Length.Should().Be(32);
    }

    [Fact]
    public void FingerprintOfIssuedTokensMatchesAnIndependentCalculation_TC_HU_001_16()
    {
        for (var i = 0; i < 10; i++)
        {
            var raw = RandomNumberGenerator.GetBytes(RefreshTokenFingerprint.TokenBytes);
            var token = Base64UrlEncoder.Encode(raw);

            RefreshTokenFingerprint.Compute(token).Should().Equal(SHA256.HashData(raw));
        }
    }

    [Fact]
    public void FingerprintDiffersPerTokenAndIsNotTheHashOfTheTokenText_TC_HU_001_16()
    {
        var other = Base64UrlEncoder.Encode(Enumerable.Repeat((byte)7, 32).ToArray());

        var first = RefreshTokenFingerprint.Compute(_tokenOfBytes0To31);
        var second = RefreshTokenFingerprint.Compute(other);

        first.Should().NotEqual(second);
        first.Should().NotEqual(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(_tokenOfBytes0To31)));
        Convert.ToHexStringLower(first).Should().NotContain(_tokenOfBytes0To31, "la huella no contiene el token");
    }

    [Theory]
    [InlineData("")]
    [InlineData("no es base64url !!")]
    [InlineData("AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh")]
    [InlineData("AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8AAQ")]
    public void InvalidTokensAreRejectedWithoutEchoingThem_TC_HU_001_16(string token)
    {
        var act = () => RefreshTokenFingerprint.Compute(token);

        var thrown = act.Should().Throw<ArgumentException>().Which;
        if (token.Length > 0)
        {
            thrown.Message.Should().NotContain(token);
        }
    }
}
