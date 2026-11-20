using System.Security.Cryptography;
using System.Text;
using Knock.Identity.Api.Common.Errors;
using Knock.Identity.Api.Modules.Panel.Data;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Knock.Identity.Api.Modules.Panel.Services;

/// <summary>Sesión recién emitida: el token de acceso con sus datos y el token de renovación, que solo viaja en la cookie (R-12, R-16).</summary>
internal sealed record PanelGrant(string AccessToken, DateTime ExpiraEn, IReadOnlyList<string> Permisos, string Nombre, string RefreshToken)
{
    public override string ToString() => nameof(PanelGrant);
}

/// <summary>Token de acceso del panel ya firmado y su vencimiento.</summary>
internal sealed record IssuedPanelToken(string Token, DateTime ExpiresAt)
{
    public override string ToString() => nameof(IssuedPanelToken);
}

/// <summary>
/// Emite el JWT del panel (spec 033, R-13): HMAC-SHA256 con la llave propia del panel, destinatario <c>knock-panel</c>, el emisor de la app y los claims
/// <c>sub</c>, <c>sid</c>, <c>pv</c> (la <c>permisosVersion</c> vigente al emitirlo) y <c>jti</c>. No lleva los permisos.
/// </summary>
internal sealed class PanelTokenIssuer(PanelSettings settings, TimeProvider timeProvider)
{
    public const string SubClaim = "sub";
    public const string SessionClaim = "sid";
    public const string VersionClaim = "pv";

    public IssuedPanelToken Issue(string sub, string sessionId, int permissionsVersion, DateTime expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sub);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Claims = new Dictionary<string, object>
            {
                [SubClaim] = sub,
                [SessionClaim] = sessionId,
                [VersionClaim] = permissionsVersion,
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N"),
            },
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(settings.SigningKey, SecurityAlgorithms.HmacSha256),
        };
        return new IssuedPanelToken(new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }
}

/// <summary>Token de renovación (R-16): 256 bits aleatorios; en la base solo se guarda la huella SHA-256 (R-15). Nunca aparece en un cuerpo de respuesta.</summary>
internal static class PanelRefreshToken
{
    public const int Bytes = 32;

    public static string Generate() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(Bytes));

    /// <summary>Huella del token en hexadecimal (64 caracteres). Cualquier cadena tiene huella: un token mal formado simplemente no coincide con ninguna sesión.</summary>
    public static string Fingerprint(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}

/// <summary>
/// Traduce las caídas de dependencias (MongoDB, Redis, Firebase) en 503 <c>unavailable</c> sin conceder acceso (R-28). Solo toma los tipos de falla de
/// infraestructura: un error de programación sigue siendo 500. El mensaje original no sale (puede traer la cadena de conexión).
/// </summary>
internal static class PanelInfra
{
    public static async Task<T> GuardAsync<T>(Func<Task<T>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        try
        {
            return await operation();
        }
        catch (Exception exception) when (IsInfrastructure(exception))
        {
            throw Unavailable();
        }
    }

    public static DomainException Unavailable() =>
        new(DomainErrorKind.ServiceUnavailable, "A dependency of the panel is not available.", Domain.PanelErrorCodes.UnavailableCode);

    internal static bool IsInfrastructure(Exception exception) =>
        PanelStoreFailures.IsStoreFailure(exception)
        || exception is TimeoutException or HttpRequestException or IOException or FirebaseAdmin.Auth.FirebaseAuthException;
}
