using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Knock.Api.Modules.Identity.Services;

/// <summary>
/// Huella del refresh token (decisión 3 del líder en HU-001): SHA-256 de los 32 bytes aleatorios del token, que viaja como Base64Url.
/// En la base solo se guarda la huella, nunca el token; HU-003 la usará para buscar el token que se presente. El token en claro solo
/// existe en la respuesta al cliente.
/// </summary>
internal static class RefreshTokenFingerprint
{
    public const int TokenBytes = 32;

    public static byte[] Compute(string refreshToken)
    {
        byte[] raw;
        try
        {
            raw = Base64UrlEncoder.DecodeBytes(refreshToken ?? string.Empty);
        }
        catch (FormatException)
        {
            throw new ArgumentException("The refresh token is not a valid Base64Url value.", nameof(refreshToken));
        }

        if (raw.Length != TokenBytes)
        {
            throw new ArgumentException($"The refresh token must decode to exactly {TokenBytes} bytes.", nameof(refreshToken));
        }

        return SHA256.HashData(raw);
    }
}
