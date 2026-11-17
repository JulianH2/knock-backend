using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Knock.Market.Api.Common.Crypto;

/// <summary>
/// Llaves del cifrado de campo (spec 020, R-06): <c>Keys</c> mapea la versión de la llave a 32 bytes en base64 y <c>ActiveVersion</c> es la que cifra; las demás
/// siguen sirviendo para descifrar. Las llaves llegan por variables de entorno o Key Vault; el repositorio no trae ninguna.
/// </summary>
internal sealed class FieldEncryptionOptions
{
    public const string SectionName = "Security:FieldEncryption";

    public string? ActiveVersion { get; set; }

    public Dictionary<string, string> Keys { get; set; } = [];
}

/// <summary>
/// Cifrado de campo AES-256-GCM con llave versionada. El valor guardado es <c>{versión}.{nonce‖etiqueta‖cifrado en base64url}</c>; el <c>context</c> (por ejemplo
/// <c>solicitudes|direccion|{id}</c>) viaja como dato asociado, así que un valor copiado a otro documento no se puede descifrar. Si no hay llave configurada falla al usarse, no al arrancar.
/// </summary>
internal sealed class FieldCipher(IOptions<FieldEncryptionOptions> options)
{
    private const int _nonceSize = 12;
    private const int _tagSize = 16;
    private const char _separator = '.';

    public string Encrypt(string plaintext, string context)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        var settings = options.Value;
        var version = settings.ActiveVersion;
        if (string.IsNullOrEmpty(version) || !settings.Keys.ContainsKey(version))
        {
            throw new InvalidOperationException("The field encryption key is not configured.");
        }

        var data = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(_nonceSize);
        var cipher = new byte[data.Length];
        var tag = new byte[_tagSize];
        using (var aes = new AesGcm(KeyFor(version), _tagSize))
        {
            aes.Encrypt(nonce, data, cipher, tag, Encoding.UTF8.GetBytes(context));
        }

        return version + _separator + WebEncoders.Base64UrlEncode([.. nonce, .. tag, .. cipher]);
    }

    public string Decrypt(string stored, string context)
    {
        ArgumentNullException.ThrowIfNull(stored);
        var separator = stored.IndexOf(_separator, StringComparison.Ordinal);
        if (separator <= 0)
        {
            throw new CryptographicException("The stored value has no key version.");
        }

        var raw = WebEncoders.Base64UrlDecode(stored[(separator + 1)..]);
        if (raw.Length < _nonceSize + _tagSize)
        {
            throw new CryptographicException("The stored value is too short.");
        }

        var nonce = raw.AsSpan(0, _nonceSize);
        var tag = raw.AsSpan(_nonceSize, _tagSize);
        var cipher = raw.AsSpan(_nonceSize + _tagSize);
        var plain = new byte[cipher.Length];
        using (var aes = new AesGcm(KeyFor(stored[..separator]), _tagSize))
        {
            aes.Decrypt(nonce, cipher, tag, plain, Encoding.UTF8.GetBytes(context));
        }

        return Encoding.UTF8.GetString(plain);
    }

    private byte[] KeyFor(string version)
    {
        if (!options.Value.Keys.TryGetValue(version, out var encoded))
        {
            throw new CryptographicException("The field encryption key version is unknown.");
        }

        var key = Convert.FromBase64String(encoded);
        return key.Length == 32 ? key : throw new InvalidOperationException("The field encryption key must be 32 bytes.");
    }
}
