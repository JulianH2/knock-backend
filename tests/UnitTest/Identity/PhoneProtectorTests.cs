using System.Security.Cryptography;
using System.Text;
using AwesomeAssertions;
using Knock.Api.Modules.Identity.Services;
using Xunit;

namespace Knock.UnitTest.Identity;

public class PhoneProtectorTests
{
    private const string _phone = "+525512345678";
    private const string _otherPhone = "+525598765432";
    private const string _userId = "0123456789abcdef01234567";
    private const string _otherUserId = "76543210fedcba9876543210";

    // Valores esperados calculados con una implementación independiente (Python hmac y hashlib) a partir de las llaves ficticias de
    // PhoneProtectionTestKeys (bytes 101..132 y 201..232).
    private const string _hmacV1Expected = "a9340e5bc0d0c7d2c69c8004d1c6628b5ef176fb00cfd913d8217d466e8c2e8d";
    private const string _hmacV2Expected = "e2a0dcb55dbf6beedb8802a4546b37f960813910c6d02f8071cc023c5a58ef7b";
    private const string _hmacOtherPhoneExpected = "4d0f6e8bb04356d333449edfffa6151c5e6c3186dc184022998982fd71a91376";
    private const string _plainSha256OfPhone = "91e296c972c986bfd3469ba3cb87a2c742b990be55289589c683d5ef0c0bc1c3";

    /// <summary>Descifrado independiente de la prueba (no usa la clase bajo prueba): nonce ‖ texto cifrado ‖ etiqueta.</summary>
    private static string Decrypt(byte[] protectedValue, byte[] key, string userId)
    {
        var nonce = protectedValue.AsSpan(0, 12);
        var ciphertext = protectedValue.AsSpan(12, protectedValue.Length - 12 - 16);
        var tag = protectedValue.AsSpan(protectedValue.Length - 16, 16);
        var plaintext = new byte[ciphertext.Length];
        using var aes = new AesGcm(key, 16);
        aes.Decrypt(nonce, ciphertext, tag, plaintext, Encoding.UTF8.GetBytes($"usuarios|celular|{userId}"));
        return Encoding.UTF8.GetString(plaintext);
    }

    [Fact]
    public void PhoneIsEncryptedWithAesGcmNonceAadAndKeyVersion_TC_HU_001_13()
    {
        var protector = new PhoneProtector(PhoneProtectionTestKeys.Keys());

        var result = protector.Protect(_phone, _userId);

        result.KeyVersion.Should().Be(1);
        result.Ciphertext.Length.Should().Be(12 + Encoding.UTF8.GetByteCount(_phone) + 16, "nonce de 12 bytes, texto cifrado y etiqueta de 16");
        Decrypt(result.Ciphertext, PhoneProtectionTestKeys.Bytes(PhoneProtectionTestKeys.EncryptionStart1), _userId).Should().Be(_phone);
    }

    [Fact]
    public void ProtectedValueNeverContainsThePlainPhone_TC_HU_001_13()
    {
        var protector = new PhoneProtector(PhoneProtectionTestKeys.Keys());
        var phoneBytes = Encoding.UTF8.GetBytes(_phone);

        var value = protector.Protect(_phone, _userId).Ciphertext;

        value.AsSpan().IndexOf(phoneBytes).Should().Be(-1, "el celular no aparece en claro");
        Convert.ToBase64String(value).Should().NotContain(Convert.ToBase64String(phoneBytes));
        Convert.ToHexStringLower(value).Should().NotContain(Convert.ToHexStringLower(phoneBytes));
        Convert.ToHexStringLower(value).Should().NotContain("5512345678");
    }

    [Fact]
    public void EncryptingTwiceGivesDifferentResultsBecauseTheNonceIsRandom_TC_HU_001_13()
    {
        var protector = new PhoneProtector(PhoneProtectionTestKeys.Keys());

        var results = Enumerable.Range(0, 20).Select(_ => Convert.ToHexStringLower(protector.Protect(_phone, _userId).Ciphertext)).ToList();

        results.Distinct().Should().HaveCount(20);
        results.Select(r => r[..24]).Distinct().Should().HaveCount(20, "los 12 bytes del nonce nunca se repiten");
    }

    [Fact]
    public void TamperedValueOrAnotherUsersIdFailsAuthentication_TC_HU_001_13()
    {
        var key = PhoneProtectionTestKeys.Bytes(PhoneProtectionTestKeys.EncryptionStart1);
        var value = new PhoneProtector(PhoneProtectionTestKeys.Keys()).Protect(_phone, _userId).Ciphertext;

        Decrypt(value, key, _userId).Should().Be(_phone);
        for (var position = 0; position < value.Length; position++)
        {
            var tampered = (byte[])value.Clone();
            tampered[position] ^= 0x01;
            var act = () => Decrypt(tampered, key, _userId);

            act.Should().Throw<CryptographicException>($"alterar el byte {position} invalida la etiqueta");
        }

        var wrongOwner = () => Decrypt(value, key, _otherUserId);
        wrongOwner.Should().Throw<CryptographicException>("el dato asociado liga el valor al _id del usuario");
    }

    [Fact]
    public void NewUsersUseTheCurrentKeyVersionAndOldValuesStayReadableWithTheirOwn_TC_HU_001_13()
    {
        var oldKeys = PhoneProtectionTestKeys.Keys();
        var rotated = PhoneProtectionTestKeys.Keys(options =>
        {
            options.EncryptionKeys[2] = PhoneProtectionTestKeys.Base64(PhoneProtectionTestKeys.EncryptionStart2);
            options.EncryptionKeyVersion = 2;
        });

        var oldValue = new PhoneProtector(oldKeys).Protect(_phone, _userId);
        var newValue = new PhoneProtector(rotated).Protect(_phone, _otherUserId);

        oldValue.KeyVersion.Should().Be(1);
        newValue.KeyVersion.Should().Be(2, "los usuarios nuevos usan la versión vigente");
        Decrypt(newValue.Ciphertext, PhoneProtectionTestKeys.Bytes(PhoneProtectionTestKeys.EncryptionStart2), _otherUserId).Should().Be(_phone);
        Decrypt(oldValue.Ciphertext, PhoneProtectionTestKeys.Bytes(PhoneProtectionTestKeys.EncryptionStart1), _userId).Should().Be(_phone,
            "el valor viejo sigue descifrable con su llave");
    }

    [Theory]
    [InlineData("")]
    [InlineData("5512345678")]
    [InlineData("+15555550199")]
    [InlineData("+52 5512345678")]
    public void InvalidPhoneIsRejectedWithoutEchoingIt_TC_HU_001_13(string phone)
    {
        var protector = new PhoneProtector(PhoneProtectionTestKeys.Keys());

        var protect = () => protector.Protect(phone, _userId);
        var hash = () => protector.Hash(phone);

        foreach (var exception in new[] { protect.Should().Throw<ArgumentException>().Which, hash.Should().Throw<ArgumentException>().Which })
        {
            if (phone.Length > 0)
            {
                exception.Message.Should().NotContain(phone);
            }
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("0123456789ABCDEF01234567")]
    [InlineData("0123456789abcdef0123456")]
    [InlineData("0123456789abcdef012345678")]
    [InlineData("0123456789abcdef0123456g")]
    public void UserIdMustBe24LowercaseHexDigits_TC_HU_001_13(string userId)
    {
        var protect = () => new PhoneProtector(PhoneProtectionTestKeys.Keys()).Protect(_phone, userId);

        protect.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void PhoneHashIsHmacSha256WithSeparateKeys_TC_HU_001_14()
    {
        var protector = new PhoneProtector(PhoneProtectionTestKeys.Keys());

        var hash = protector.Hash(_phone);

        Convert.ToHexStringLower(hash.Value).Should().Be(_hmacV1Expected, "HMAC-SHA256 del E.164 con la llave HMAC de la versión vigente");
        hash.KeyVersion.Should().Be(1);
        Convert.ToHexStringLower(protector.Hash(_phone).Value).Should().Be(_hmacV1Expected, "es estable");
        Convert.ToHexStringLower(protector.Hash(_otherPhone).Value).Should().Be(_hmacOtherPhoneExpected, "cambia con otro celular");
        Convert.ToHexStringLower(hash.Value).Should().NotBe(_plainSha256OfPhone, "no es el SHA-256 simple del celular");
    }

    [Fact]
    public void PhoneHashChangesWithTheHmacKeyAndNeverUsesTheEncryptionKey_TC_HU_001_14()
    {
        var rotated = PhoneProtectionTestKeys.Keys(options =>
        {
            options.HmacKeys[2] = PhoneProtectionTestKeys.Base64(PhoneProtectionTestKeys.HmacStart2);
            options.HmacKeyVersion = 2;
        });

        var hash = new PhoneProtector(rotated).Hash(_phone);

        hash.KeyVersion.Should().Be(2);
        Convert.ToHexStringLower(hash.Value).Should().Be(_hmacV2Expected, "otra llave HMAC da otro hash");
        Convert.ToHexStringLower(hash.Value).Should().NotBe(_hmacV1Expected);
        var withEncryptionKey = HMACSHA256.HashData(PhoneProtectionTestKeys.Bytes(PhoneProtectionTestKeys.EncryptionStart1), Encoding.UTF8.GetBytes(_phone));
        Convert.ToHexStringLower(new PhoneProtector(PhoneProtectionTestKeys.Keys()).Hash(_phone).Value).Should()
            .NotBe(Convert.ToHexStringLower(withEncryptionKey), "la llave de cifrado y la de HMAC son distintas");
    }
}
