using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.Identity.Api.Common.Crypto;
using Microsoft.Extensions.Options;
using Xunit;

namespace Knock.UnitTest.Identity;

/// <summary>Spec 040, T-00 (§8): cifrado de campo AES-256-GCM de Identidad para la dirección. Llaves ficticias construidas en ejecución.</summary>
public class Spec040FieldCipherTests
{
    private const string _context = "direcciones|calleNumero|0123456789abcdef01234567";
    private const string _plain = "Calle Falsa 123";

    private static string Key(int start) => Convert.ToBase64String(Enumerable.Range(0, 32).Select(i => (byte)((start + i) & 0xFF)).ToArray());

    private static FieldCipher Cipher(string? active = "1", params (string Version, string Key)[] keys)
    {
        var options = new FieldEncryptionOptions { ActiveVersion = active };
        foreach (var (version, key) in keys.Length == 0 ? [("1", Key(1))] : keys)
        {
            options.Keys[version] = key;
        }

        return new FieldCipher(Options.Create(options));
    }

    [Fact]
    public void Value_RoundTripsAndNeverContainsThePlainText_R_10()
    {
        var cipher = Cipher();

        var stored = cipher.Encrypt(_plain, _context);

        stored.Should().StartWith("1.").And.NotContain(_plain).And.NotContain("Falsa");
        cipher.Decrypt(stored, _context).Should().Be(_plain);
    }

    [Fact]
    public void EachEncryptionUsesANewNonce_R_10()
    {
        var cipher = Cipher();

        cipher.Encrypt(_plain, _context).Should().NotBe(cipher.Encrypt(_plain, _context));
    }

    [Fact]
    public void ValueCopiedToAnotherDocument_CannotBeDecrypted_R_13()
    {
        var cipher = Cipher();
        var stored = cipher.Encrypt(_plain, _context);

        var act = () => cipher.Decrypt(stored, "direcciones|calleNumero|76543210fedcba9876543210");

        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void TamperedValue_FailsAuthentication_R_13()
    {
        var cipher = Cipher();
        var stored = cipher.Encrypt(_plain, _context);
        var tampered = stored[..^2] + (stored[^2] == 'A' ? 'B' : 'A') + stored[^1];

        var act = () => cipher.Decrypt(tampered, _context);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void RotatedKey_StillDecryptsOldValuesAndEncryptsWithTheActiveVersion_R_13()
    {
        var old = Cipher("1", ("1", Key(1))).Encrypt(_plain, _context);
        var rotated = Cipher("2", ("1", Key(1)), ("2", Key(51)));

        rotated.Decrypt(old, _context).Should().Be(_plain);
        rotated.Encrypt(_plain, _context).Should().StartWith("2.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("9")]
    public void WithoutAnActiveKey_ItFailsClosedWhenUsed_R_13(string? active)
    {
        var cipher = Cipher(active);

        var act = () => cipher.Encrypt(_plain, _context);

        act.Should().Throw<InvalidOperationException>().Which.Message.Should().NotContain(_plain);
    }

    [Fact]
    public void KeyOfTheWrongLength_IsRejectedWithoutPrintingItsValue_R_13()
    {
        var weak = Convert.ToBase64String(new byte[16]);
        var cipher = Cipher("1", ("1", weak));

        var act = () => cipher.Encrypt(_plain, _context);

        act.Should().Throw<InvalidOperationException>().Which.Message.Should().NotContain(weak);
    }
}
