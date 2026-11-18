using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Knock.IntegrationTest.Support;

/// <summary>Fábrica de tokens de servicio de Identidad hacia Mercado (spec 041, R-17) con una clave sintética al azar por ejecución. Nunca se escribe en disco.</summary>
internal static class InternalServiceTokens
{
    public static readonly byte[] Key = RandomNumberGenerator.GetBytes(32);

    public static string KeyBase64 => Convert.ToBase64String(Key);

    public static string Issue(
        string usuarioId,
        string elementoId,
        string[]? permisos = null,
        string issuer = "knock-identity",
        string audience = "knock-market",
        byte[]? key = null,
        TimeSpan? lifetime = null,
        DateTime? issuedAt = null,
        bool withJti = true)
    {
        var now = issuedAt ?? DateTime.UtcNow;
        var claims = new Dictionary<string, object>
        {
            ["usuarioId"] = usuarioId,
            ["elementoId"] = elementoId,
            ["permisos"] = permisos ?? ["historial"],
        };
        if (withJti)
        {
            claims["jti"] = Guid.NewGuid().ToString("N");
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Claims = claims,
            IssuedAt = now,
            NotBefore = now.AddSeconds(-1),
            Expires = now + (lifetime ?? TimeSpan.FromMinutes(1)),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key ?? Key), SecurityAlgorithms.HmacSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
