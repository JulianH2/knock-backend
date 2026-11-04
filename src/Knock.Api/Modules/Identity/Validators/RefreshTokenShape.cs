using System.Buffers.Text;
using Knock.Api.Modules.Identity.Services;

namespace Knock.Api.Modules.Identity.Validators;

/// <summary>Forma del refresh token (HU-001 decisión 3): Base64Url de exactamente 32 bytes, que son 43 caracteres sin relleno.</summary>
internal static class RefreshTokenShape
{
    public const int EncodedLength = 43;

    public static bool IsValid(string? value)
    {
        // IsValid no lanza con caracteres fuera del alfabeto Base64Url (TryDecodeFromChars sí).
        if (value is not { Length: EncodedLength } || !Base64Url.IsValid(value))
        {
            return false;
        }

        Span<byte> buffer = stackalloc byte[RefreshTokenFingerprint.TokenBytes + 4];
        return Base64Url.TryDecodeFromChars(value, buffer, out var written) && written == RefreshTokenFingerprint.TokenBytes;
    }
}
