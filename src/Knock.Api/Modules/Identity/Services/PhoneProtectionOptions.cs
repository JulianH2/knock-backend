using Microsoft.Extensions.Options;

namespace Knock.Api.Modules.Identity.Services;

/// <summary>
/// Llaves de protección del celular (<c>Identity:PhoneProtection:*</c>). Las llaves solo llegan por variable de entorno o user-secrets
/// (Key Vault después, TT-SEC-01): nunca van en <c>appsettings*.json</c>. Sin una configuración válida la API no arranca, tampoco en Development.
/// </summary>
internal sealed class PhoneProtectionOptions
{
    public const string SectionName = "Identity:PhoneProtection";

    /// <summary>Llaves AES-256 por versión: Base64 de exactamente 32 bytes.</summary>
    public Dictionary<int, string> EncryptionKeys { get; } = [];

    /// <summary>Llaves HMAC-SHA256 por versión: Base64 de al menos 32 bytes; distintas de las de cifrado.</summary>
    public Dictionary<int, string> HmacKeys { get; } = [];

    /// <summary>Versión vigente de la llave de cifrado (la que usan los usuarios nuevos).</summary>
    public int EncryptionKeyVersion { get; set; }

    /// <summary>Versión vigente de la llave HMAC.</summary>
    public int HmacKeyVersion { get; set; }
}

/// <summary>Llaves ya decodificadas y validadas, con su versión vigente.</summary>
internal sealed class PhoneProtectionKeys
{
    public const int EncryptionKeyBytes = 32;
    public const int MinimumHmacKeyBytes = 32;

    private PhoneProtectionKeys(Dictionary<int, byte[]> encryption, Dictionary<int, byte[]> hmac, int encryptionVersion, int hmacVersion)
    {
        Encryption = encryption;
        Hmac = hmac;
        EncryptionVersion = encryptionVersion;
        HmacVersion = hmacVersion;
    }

    public IReadOnlyDictionary<int, byte[]> Encryption { get; }

    public IReadOnlyDictionary<int, byte[]> Hmac { get; }

    public int EncryptionVersion { get; }

    public int HmacVersion { get; }

    public byte[] CurrentEncryptionKey => Encryption[EncryptionVersion];

    public byte[] CurrentHmacKey => Hmac[HmacVersion];

    /// <summary>
    /// Valida la configuración y, si es correcta, devuelve las llaves decodificadas. Los mensajes nombran la clave de configuración
    /// (p. ej. <c>Identity:PhoneProtection:EncryptionKeys:1</c>) y nunca incluyen su valor.
    /// </summary>
    public static PhoneProtectionKeys? TryCreate(PhoneProtectionOptions options, out IReadOnlyList<string> errors)
    {
        ArgumentNullException.ThrowIfNull(options);
        var problems = new List<string>();
        var encryption = Decode(options.EncryptionKeys, nameof(PhoneProtectionOptions.EncryptionKeys), problems, exactBytes: EncryptionKeyBytes);
        var hmac = Decode(options.HmacKeys, nameof(PhoneProtectionOptions.HmacKeys), problems, minimumBytes: MinimumHmacKeyBytes);
        CheckVersion(options.EncryptionKeyVersion, encryption, nameof(PhoneProtectionOptions.EncryptionKeyVersion),
            nameof(PhoneProtectionOptions.EncryptionKeys), problems);
        CheckVersion(options.HmacKeyVersion, hmac, nameof(PhoneProtectionOptions.HmacKeyVersion), nameof(PhoneProtectionOptions.HmacKeys), problems);

        foreach (var (encryptionVersion, encryptionKey) in encryption)
        {
            foreach (var (hmacVersion, hmacKey) in hmac)
            {
                if (encryptionKey.AsSpan().SequenceEqual(hmacKey))
                {
                    problems.Add($"{Path(nameof(PhoneProtectionOptions.EncryptionKeys))}:{encryptionVersion} and "
                        + $"{Path(nameof(PhoneProtectionOptions.HmacKeys))}:{hmacVersion} must be different keys.");
                }
            }
        }

        errors = problems;
        return problems.Count == 0
            ? new PhoneProtectionKeys(encryption, hmac, options.EncryptionKeyVersion, options.HmacKeyVersion)
            : null;
    }

    private static string Path(string name) => $"{PhoneProtectionOptions.SectionName}:{name}";

    private static Dictionary<int, byte[]> Decode(
        Dictionary<int, string> configured, string name, List<string> problems, int? exactBytes = null, int? minimumBytes = null)
    {
        var keys = new Dictionary<int, byte[]>();
        if (configured.Count == 0)
        {
            problems.Add($"{Path(name)} is missing: set at least one key by environment variable or user-secrets.");
            return keys;
        }

        foreach (var (version, value) in configured)
        {
            var key = TryDecode(value);
            var length = key?.Length ?? 0;
            var valid = key is not null && (exactBytes is null ? length >= minimumBytes : length == exactBytes);
            if (valid)
            {
                keys[version] = key!;
            }
            else
            {
                var rule = exactBytes is { } exact ? $"exactly {exact} bytes" : $"at least {minimumBytes} bytes";
                problems.Add($"{Path(name)}:{version} must be Base64 of {rule}.");
            }
        }

        return keys;
    }

    private static byte[]? TryDecode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var buffer = new byte[value.Length];
        return Convert.TryFromBase64String(value, buffer, out var written) ? buffer[..written] : null;
    }

    private static void CheckVersion(int version, Dictionary<int, byte[]> decoded, string versionName, string keysName, List<string> problems)
    {
        if (version <= 0 || !decoded.ContainsKey(version))
        {
            problems.Add($"{Path(versionName)} must be a positive integer that matches one valid key of {Path(keysName)}.");
        }
    }
}

/// <summary>Valida <see cref="PhoneProtectionOptions"/> al arrancar (<c>ValidateOnStart</c>): la API no arranca con llaves inválidas.</summary>
internal sealed class PhoneProtectionOptionsValidator : IValidateOptions<PhoneProtectionOptions>
{
    public ValidateOptionsResult Validate(string? name, PhoneProtectionOptions options)
    {
        PhoneProtectionKeys.TryCreate(options, out var errors);
        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
