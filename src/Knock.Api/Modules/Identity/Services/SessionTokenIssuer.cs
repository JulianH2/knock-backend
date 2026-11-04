using System.Security.Cryptography;
using Knock.Api.Common.Security;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Knock.Api.Modules.Identity.Services;

/// <summary>Sesión emitida: JWT de acceso y refresh token (valor aleatorio; todavía no se guarda ni se puede canjear).</summary>
internal sealed record IssuedSession(string AccessToken, string RefreshToken);

/// <summary>Emite el JWT propio (HS256, 15 min, claims <c>sub</c> y <c>jti</c>, sin teléfono ni <c>role</c>) y el refresh token.</summary>
internal sealed class SessionTokenIssuer(JwtSettings settings, TimeProvider timeProvider)
{
    public static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);

    private const int _refreshTokenBytes = 32;

    public IssuedSession Issue(Guid userId) => new(IssueAccessToken(userId), NewRefreshToken());

    /// <summary>JWT de acceso nuevo (<c>jti</c> propio) para una cuenta; la renovación lo emite aparte de su refresh token.</summary>
    public string IssueAccessToken(Guid userId)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Claims = new Dictionary<string, object>
            {
                ["sub"] = userId.ToString("D"),
                ["jti"] = Guid.NewGuid().ToString("D"),
            },
            IssuedAt = now,
            NotBefore = now,
            Expires = now + AccessTokenLifetime,
            SigningCredentials = new SigningCredentials(settings.SigningKey, SecurityAlgorithms.HmacSha256),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    /// <summary>Refresh token nuevo: 32 bytes aleatorios en Base64Url.</summary>
    public static string NewRefreshToken() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(_refreshTokenBytes));
}
