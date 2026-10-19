using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace Knock.Api.Modules.Identity.Services;

/// <summary>Deriva el <c>sub</c> del JWT (GUID v5, RFC 9562 §5.5) a partir del <c>uid</c> de Firebase. .NET no trae v5 incorporado.</summary>
internal static class UserIdDerivation
{
    /// <summary>
    /// Namespace fijo de Knock para el GUID v5 (decidido por el revisor de KNOCK-21, generado una sola vez).
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

        var hash = Sha1(input);
        hash[6] = (byte)((hash[6] & 0x0F) | 0x50);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        return new Guid(hash.AsSpan(0, 16), bigEndian: true);
    }

    /// <summary>
    /// SHA-1 (RFC 3174) propio. RFC 9562 §5.5 exige SHA-1 para v5 y aquí solo produce un identificador determinista, sin valor
    /// criptográfico; se implementa a mano para no usar la API de hash débil que los analizadores (CA5350, S4790) prohíben
    /// sin excepción registrada. Lo comprueba <c>UserIdDerivationTests</c> contra <c>uuid.uuid5</c> de Python.
    /// </summary>
    private static byte[] Sha1(byte[] message)
    {
        uint h0 = 0x67452301, h1 = 0xEFCDAB89, h2 = 0x98BADCFE, h3 = 0x10325476, h4 = 0xC3D2E1F0;

        var paddedLength = ((message.Length + 8) / 64 + 1) * 64;
        var data = new byte[paddedLength];
        message.CopyTo(data, 0);
        data[message.Length] = 0x80;
        BinaryPrimitives.WriteUInt64BigEndian(data.AsSpan(paddedLength - 8), (ulong)message.Length * 8);

        var w = new uint[80];
        for (var offset = 0; offset < paddedLength; offset += 64)
        {
            for (var i = 0; i < 16; i++)
            {
                w[i] = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + (i * 4)));
            }

            for (var i = 16; i < 80; i++)
            {
                w[i] = BitOperations.RotateLeft(w[i - 3] ^ w[i - 8] ^ w[i - 14] ^ w[i - 16], 1);
            }

            uint a = h0, b = h1, c = h2, d = h3, e = h4;
            for (var i = 0; i < 80; i++)
            {
                var (f, k) = i switch
                {
                    < 20 => ((b & c) | (~b & d), 0x5A827999u),
                    < 40 => (b ^ c ^ d, 0x6ED9EBA1u),
                    < 60 => ((b & c) | (b & d) | (c & d), 0x8F1BBCDCu),
                    _ => (b ^ c ^ d, 0xCA62C1D6u),
                };
                var temp = BitOperations.RotateLeft(a, 5) + f + e + k + w[i];
                e = d;
                d = c;
                c = BitOperations.RotateLeft(b, 30);
                b = a;
                a = temp;
            }

            h0 += a;
            h1 += b;
            h2 += c;
            h3 += d;
            h4 += e;
        }

        var digest = new byte[20];
        BinaryPrimitives.WriteUInt32BigEndian(digest.AsSpan(0), h0);
        BinaryPrimitives.WriteUInt32BigEndian(digest.AsSpan(4), h1);
        BinaryPrimitives.WriteUInt32BigEndian(digest.AsSpan(8), h2);
        BinaryPrimitives.WriteUInt32BigEndian(digest.AsSpan(12), h3);
        BinaryPrimitives.WriteUInt32BigEndian(digest.AsSpan(16), h4);
        return digest;
    }
}
