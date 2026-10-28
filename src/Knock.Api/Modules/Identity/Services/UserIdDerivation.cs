using System.Security.Cryptography;
using System.Text;

namespace Knock.Api.Modules.Identity.Services;

/// <summary>
/// Deriva el <c>sub</c> del JWT a partir del <c>uid</c> de Firebase como UUID v8 con SHA-256 (RFC 9562 §5.8): los primeros 16 bytes del
/// SHA-256 de <c>Namespace</c> (orden de red) seguido del <c>uid</c> en UTF-8, con el dígito de versión 8 y la variante RFC.
/// </summary>
internal static class UserIdDerivation
{
    /// <summary>
    /// Namespace fijo de Knock (decidido por el revisor de KNOCK-21, generado una sola vez).
    /// NO CAMBIA NUNCA: cambiarlo cambia el <c>sub</c> de todos los usuarios. No es un secreto.
    /// </summary>
    public static readonly Guid Namespace = new("bcfea58b-95dd-4c0e-80d1-baf839f2e36e");

    public static Guid Derive(string uid)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uid);

        var name = Encoding.UTF8.GetBytes(uid);
        var input = new byte[16 + name.Length];
        Namespace.TryWriteBytes(input, bigEndian: true, out _);
        name.CopyTo(input, 16);

        var hash = SHA256.HashData(input);
        hash[6] = (byte)((hash[6] & 0x0F) | 0x80);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        return new Guid(hash.AsSpan(0, 16), bigEndian: true);
    }
}
