using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Knock.Identity.Api.Common.Crypto;

/// <summary>
/// Llaves del cifrado de campo de Identidad (spec 040, §8): <c>Keys</c> mapea la versión de la llave a 32 bytes en base64 y <c>ActiveVersion</c> es la que
/// cifra; las demás solo descifran. Misma sección y mismo formato que el cifrado de campo de Mercado (spec 020). Las llaves llegan por variable de entorno
/// o Key Vault; el repositorio no trae ninguna.
/// </summary>
internal sealed class FieldEncryptionOptions
{
    public const string SectionName = "Security:FieldEncryption";

    public string? ActiveVersion { get; set; }

    public Dictionary<string, string> Keys { get; } = [];
}

/// <summary>
/// AES-256-GCM con llave versionada. El valor guardado es <c>{versión}.{base64url(nonce ‖ etiqueta ‖ cifrado)}</c>. El <c>context</c> (por ejemplo
/// <c>direcciones|calleNumero|{id}</c>) viaja como dato asociado: un valor copiado a otro documento o campo no se descifra. Sin llave configurada falla al
/// usarse (nunca guarda en claro) y ningún mensaje de error incluye el texto ni las llaves.
/// </summary>
internal sealed class FieldCipher(IOptions<FieldEncryptionOptions> options)
{
    private const int _nonceBytes = 12;
    private const int _tagBytes = 16;
    private const int _keyBytes = 32;
    private const char _separator = '.';

    public string Encrypt(string plaintext, string context)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        ArgumentException.ThrowIfNullOrEmpty(context);
        var version = options.Value.ActiveVersion;
        if (string.IsNullOrEmpty(version) || !options.Value.Keys.ContainsKey(version))
        {
            throw new InvalidOperationException("The field encryption key is not configured.");
        }

        var data = Encoding.UTF8.GetBytes(plaintext);
        var packed = new byte[_nonceBytes + _tagBytes + data.Length];
        RandomNumberGenerator.Fill(packed.AsSpan(0, _nonceBytes));
        using var aes = new AesGcm(KeyOf(version), _tagBytes);
        aes.Encrypt(
            packed.AsSpan(0, _nonceBytes),
            data,
            packed.AsSpan(_nonceBytes + _tagBytes),
            packed.AsSpan(_nonceBytes, _tagBytes),
            Encoding.UTF8.GetBytes(context));
        return string.Concat(version, _separator.ToString(), WebEncoders.Base64UrlEncode(packed));
    }

    public string Decrypt(string stored, string context)
    {
        ArgumentNullException.ThrowIfNull(stored);
        ArgumentException.ThrowIfNullOrEmpty(context);
        var split = stored.IndexOf(_separator, StringComparison.Ordinal);
        if (split <= 0)
        {
            throw new CryptographicException("The stored value has no key version.");
        }

        var packed = WebEncoders.Base64UrlDecode(stored[(split + 1)..]);
        if (packed.Length < _nonceBytes + _tagBytes)
        {
            throw new CryptographicException("The stored value is too short.");
        }

        var plain = new byte[packed.Length - _nonceBytes - _tagBytes];
        using var aes = new AesGcm(KeyOf(stored[..split]), _tagBytes);
        aes.Decrypt(
            packed.AsSpan(0, _nonceBytes),
            packed.AsSpan(_nonceBytes + _tagBytes),
            packed.AsSpan(_nonceBytes, _tagBytes),
            plain,
            Encoding.UTF8.GetBytes(context));
        return Encoding.UTF8.GetString(plain);
    }

    private byte[] KeyOf(string version)
    {
        if (!options.Value.Keys.TryGetValue(version, out var encoded))
        {
            throw new CryptographicException("The field encryption key version is unknown.");
        }

        var key = new byte[_keyBytes + 1];
        if (!Convert.TryFromBase64String(encoded, key, out var written) || written != _keyBytes)
        {
            throw new InvalidOperationException("The field encryption key must be Base64 of 32 bytes.");
        }

        return key[.._keyBytes];
    }
}
