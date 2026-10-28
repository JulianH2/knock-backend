using AwesomeAssertions;
using Knock.Api.Modules.Identity.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Knock.UnitTest.Identity;

public class PhoneProtectionOptionsTests
{
    private const string _encryption = "Identity:PhoneProtection:EncryptionKeys";
    private const string _hmac = "Identity:PhoneProtection:HmacKeys";

    // Los tipos de opciones son internos: la teoría recibe el índice del escenario para no exponerlos en una firma pública.
    private static readonly (string Scenario, Action<PhoneProtectionOptions> Break, string ExpectedName)[] _invalidConfigurations =
    [
        ("falta la llave de cifrado", o => o.EncryptionKeys.Clear(), _encryption),
        ("falta la llave HMAC", o => o.HmacKeys.Clear(), _hmac),
        ("cifrado no es Base64", o => o.EncryptionKeys[1] = "esto no es base64 !!", _encryption + ":1"),
        ("cifrado vacío", o => o.EncryptionKeys[1] = "   ", _encryption + ":1"),
        ("cifrado de 31 bytes", o => o.EncryptionKeys[1] = PhoneProtectionTestKeys.Base64(PhoneProtectionTestKeys.EncryptionStart1, 31), _encryption + ":1"),
        ("cifrado de 33 bytes", o => o.EncryptionKeys[1] = PhoneProtectionTestKeys.Base64(PhoneProtectionTestKeys.EncryptionStart1, 33), _encryption + ":1"),
        ("HMAC no es Base64", o => o.HmacKeys[1] = "esto no es base64 !!", _hmac + ":1"),
        ("HMAC de 31 bytes", o => o.HmacKeys[1] = PhoneProtectionTestKeys.Base64(PhoneProtectionTestKeys.HmacStart1, 31), _hmac + ":1"),
        ("falta la versión vigente de cifrado", o => o.EncryptionKeyVersion = 0, "Identity:PhoneProtection:EncryptionKeyVersion"),
        ("versión de cifrado sin llave", o => o.EncryptionKeyVersion = 7, "Identity:PhoneProtection:EncryptionKeyVersion"),
        ("falta la versión vigente de HMAC", o => o.HmacKeyVersion = 0, "Identity:PhoneProtection:HmacKeyVersion"),
        ("versión de HMAC sin llave", o => o.HmacKeyVersion = 9, "Identity:PhoneProtection:HmacKeyVersion"),
        ("cifrado y HMAC coinciden", o => o.HmacKeys[1] = PhoneProtectionTestKeys.Base64(PhoneProtectionTestKeys.EncryptionStart1), "must be different keys"),
    ];

    public static TheoryData<int> InvalidConfigurationIndexes => new(Enumerable.Range(0, _invalidConfigurations.Length));

    [Theory]
    [MemberData(nameof(InvalidConfigurationIndexes))]
    public void InvalidKeyConfigurationFailsStartupNamingTheKey_TC_HU_001_15(int index)
    {
        var (scenario, breakIt, expectedName) = _invalidConfigurations[index];
        var options = PhoneProtectionTestKeys.Options(breakIt);

        var result = new PhoneProtectionOptionsValidator().Validate(null, options);

        result.Failed.Should().BeTrue(scenario);
        string.Join(" | ", result.Failures!).Should().Contain(expectedName, "el mensaje nombra la clave de configuración");
        PhoneProtectionKeys.TryCreate(options, out _).Should().BeNull(scenario);
    }

    [Fact]
    public void ErrorMessagesNeverIncludeTheKeyValues_TC_HU_001_15()
    {
        var secretLike = PhoneProtectionTestKeys.Base64(PhoneProtectionTestKeys.EncryptionStart1, 31);
        var options = PhoneProtectionTestKeys.Options(o =>
        {
            o.EncryptionKeys[1] = secretLike;
            o.HmacKeys[1] = PhoneProtectionTestKeys.Base64(PhoneProtectionTestKeys.HmacStart1, 16);
            o.HmacKeys[2] = "no-es-base64-!";
        });

        var result = new PhoneProtectionOptionsValidator().Validate(null, options);

        result.Failed.Should().BeTrue();
        var text = string.Join(" | ", result.Failures!);
        text.Should().NotContain(secretLike).And.NotContain(options.HmacKeys[1]).And.NotContain(options.HmacKeys[2]);
        text.Should().Contain(_encryption + ":1").And.Contain(_hmac + ":1").And.Contain(_hmac + ":2");
    }

    [Fact]
    public void ValidConfigurationIsAcceptedWithOneOrSeveralVersions_TC_HU_001_15()
    {
        var single = new PhoneProtectionOptionsValidator().Validate(null, PhoneProtectionTestKeys.Options());
        var several = PhoneProtectionTestKeys.Options(o =>
        {
            o.EncryptionKeys[2] = PhoneProtectionTestKeys.Base64(PhoneProtectionTestKeys.EncryptionStart2);
            o.HmacKeys[2] = PhoneProtectionTestKeys.Base64(PhoneProtectionTestKeys.HmacStart2, 48);
            o.EncryptionKeyVersion = 2;
            o.HmacKeyVersion = 2;
        });

        var keys = PhoneProtectionKeys.TryCreate(several, out var errors);

        single.Succeeded.Should().BeTrue();
        errors.Should().BeEmpty();
        keys.Should().NotBeNull();
        keys.EncryptionVersion.Should().Be(2);
        keys.HmacVersion.Should().Be(2);
        keys.CurrentHmacKey.Length.Should().Be(48, "la llave HMAC puede medir más de 32 bytes");
        keys.Encryption.Keys.Should().BeEquivalentTo([1, 2]);
    }

    [Fact]
    public void ConfigurationBindsFromEnvironmentStyleKeys_TC_HU_001_15()
    {
        var values = new Dictionary<string, string?>
        {
            ["Identity:PhoneProtection:EncryptionKeys:1"] = PhoneProtectionTestKeys.Base64(PhoneProtectionTestKeys.EncryptionStart1),
            ["Identity:PhoneProtection:HmacKeys:1"] = PhoneProtectionTestKeys.Base64(PhoneProtectionTestKeys.HmacStart1),
            ["Identity:PhoneProtection:EncryptionKeyVersion"] = "1",
            ["Identity:PhoneProtection:HmacKeyVersion"] = "1",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        var options = configuration.GetSection(PhoneProtectionOptions.SectionName).Get<PhoneProtectionOptions>();

        options.Should().NotBeNull();
        PhoneProtectionKeys.TryCreate(options, out var errors).Should().NotBeNull();
        errors.Should().BeEmpty();
    }
}
