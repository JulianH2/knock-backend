using Knock.Api.Modules.Identity.Services;

namespace Knock.UnitTest.Identity;

/// <summary>
/// Llaves ficticias de pruebas, construidas en ejecución (sin literales Base64 en el repositorio). Cifrado: bytes <c>start + i</c>; HMAC: bytes
/// <c>start + i</c> con otro inicio, para que las dos familias nunca coincidan.
/// </summary>
internal static class PhoneProtectionTestKeys
{
    public const int EncryptionStart1 = 1;
    public const int EncryptionStart2 = 51;
    public const int HmacStart1 = 101;
    public const int HmacStart2 = 201;

    public static byte[] Bytes(int start, int length = 32) => Enumerable.Range(0, length).Select(i => (byte)((start + i) & 0xFF)).ToArray();

    public static string Base64(int start, int length = 32) => Convert.ToBase64String(Bytes(start, length));

    /// <summary>Configuración válida con una versión de cada llave (1); <paramref name="configure"/> permite romperla.</summary>
    public static PhoneProtectionOptions Options(Action<PhoneProtectionOptions>? configure = null)
    {
        var options = new PhoneProtectionOptions { EncryptionKeyVersion = 1, HmacKeyVersion = 1 };
        options.EncryptionKeys[1] = Base64(EncryptionStart1);
        options.HmacKeys[1] = Base64(HmacStart1);
        configure?.Invoke(options);
        return options;
    }

    public static PhoneProtectionKeys Keys(Action<PhoneProtectionOptions>? configure = null)
    {
        var keys = PhoneProtectionKeys.TryCreate(Options(configure), out var errors);
        return keys ?? throw new InvalidOperationException("La configuración de prueba es inválida: " + string.Join("; ", errors));
    }
}
