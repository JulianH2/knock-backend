using System.Security.Cryptography;
using System.Text;
using Knock.Api.Modules.Identity.Domain;

namespace Knock.Api.Modules.Identity.Services;

/// <summary>Celular cifrado: <c>nonce (12) ‖ texto cifrado ‖ etiqueta (16)</c> y la versión de la llave AES con que se produjo.</summary>
internal sealed record ProtectedPhone(byte[] Ciphertext, int KeyVersion);

/// <summary>Hash del celular para buscarlo por igualdad (HMAC-SHA256) y la versión de la llave HMAC con que se produjo.</summary>
internal sealed record PhoneHash(byte[] Value, int KeyVersion);

/// <summary>
/// Protege el celular (decisión 6 del líder en HU-001): AES-256-GCM para poder mostrarlo después a quien corresponda y HMAC-SHA256 con una
/// llave distinta para buscar la cuenta sin descifrar. El dato asociado de GCM liga el texto cifrado al <c>_id</c> del usuario: copiarlo a otro
/// usuario hace fallar la autenticación. No existe <c>Unprotect</c> a propósito: se escribe cuando una historia lo necesite (la prueba descifra
/// con su propia implementación). Ni los mensajes de error ni nada de esta clase incluyen el celular ni las llaves.
/// </summary>
internal sealed class PhoneProtector(PhoneProtectionKeys keys)
{
    private const int _nonceBytes = 12;
    private const int _tagBytes = 16;
    private const int _userIdHexLength = 24;

    /// <summary>
    /// Cifra el celular. <paramref name="userIdHex"/> es el <c>_id</c> (ObjectId, 24 dígitos hexadecimales en minúsculas) del usuario: debe
    /// generarse <b>antes</b> de cifrar. El nonce sale de <see cref="RandomNumberGenerator"/> en cada cifrado (nunca fijo ni de contador).
    /// </summary>
    public ProtectedPhone Protect(string phone, string userIdHex)
    {
        EnsureValidPhone(phone);
        if (!IsLowerHex(userIdHex, _userIdHexLength))
        {
            throw new ArgumentException("The user id must be 24 lowercase hexadecimal digits.", nameof(userIdHex));
        }

        var plaintext = Encoding.UTF8.GetBytes(phone);
        var nonce = RandomNumberGenerator.GetBytes(_nonceBytes);
        var output = new byte[_nonceBytes + plaintext.Length + _tagBytes];
        nonce.CopyTo(output, 0);

        using var aes = new AesGcm(keys.CurrentEncryptionKey, _tagBytes);
        aes.Encrypt(
            nonce,
            plaintext,
            output.AsSpan(_nonceBytes, plaintext.Length),
            output.AsSpan(_nonceBytes + plaintext.Length, _tagBytes),
            AssociatedData(userIdHex));
        return new ProtectedPhone(output, keys.EncryptionVersion);
    }

    /// <summary>HMAC-SHA256 del E.164 con la llave vigente: estable, distinto por celular y por llave, y no es el SHA-256 simple del celular.</summary>
    public PhoneHash Hash(string phone)
    {
        EnsureValidPhone(phone);
        return new PhoneHash(HMACSHA256.HashData(keys.CurrentHmacKey, Encoding.UTF8.GetBytes(phone)), keys.HmacVersion);
    }

    /// <summary>Dato asociado de GCM: <c>usuarios|celular|&lt;_id en hexadecimal&gt;</c>.</summary>
    internal static byte[] AssociatedData(string userIdHex) => Encoding.UTF8.GetBytes($"usuarios|celular|{userIdHex}");

    private static void EnsureValidPhone(string phone)
    {
        if (!PhoneNumberPolicy.IsValid(phone))
        {
            throw new ArgumentException("The phone number is not a valid +52 E.164 number.", nameof(phone));
        }
    }

    private static bool IsLowerHex(string? value, int length)
    {
        if (value is null || value.Length != length)
        {
            return false;
        }

        return value.All(c => c is (>= '0' and <= '9') or (>= 'a' and <= 'f'));
    }
}
