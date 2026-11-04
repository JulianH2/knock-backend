using System.Globalization;
using System.Security.Claims;
using Knock.Api.Common.Security;
using Knock.Api.Modules.Identity.Data;
using Knock.Api.Modules.Identity.Domain;

namespace Knock.Api.Modules.Identity.Services;

/// <summary>Claims del access token que acompañó al cierre de sesión (opcional): solo lo que hace falta para revocar su <c>jti</c>.</summary>
internal sealed record AccessTokenClaims(string Sub, string Jti, DateTimeOffset ExpiresAt)
{
    /// <summary>Lee <c>sub</c>, <c>jti</c> y <c>exp</c> de un usuario autenticado; <c>null</c> si no hay un access token válido.</summary>
    public static AccessTokenClaims? From(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var sub = principal.FindFirst("sub")?.Value;
        var jti = principal.FindFirst("jti")?.Value;
        return sub is not null
            && jti is not null
            && long.TryParse(principal.FindFirst("exp")?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds)
                ? new AccessTokenClaims(sub, jti, DateTimeOffset.FromUnixTimeSeconds(seconds))
                : null;
    }
}

/// <summary>
/// Cierre de sesión de este dispositivo (spec HU-004): revoca toda la familia del refresh token, exista o no (idempotente y sin revelar si existía).
/// Si llega un access token de otra persona que la dueña del refresh, no revoca nada (caso 07); si es de la misma y hay lista de revocados, también
/// revoca su <c>jti</c>. Ni los tokens, ni las huellas, ni el <c>sub</c>, ni el <c>jti</c> llegan a los registros.
/// </summary>
internal sealed partial class LogoutService(
    IRefreshSessionStore store,
    IAccessTokenRevocations revocations,
    TimeProvider timeProvider,
    ILogger<LogoutService> logger)
{
    public async Task LogoutAsync(string refreshToken, AccessTokenClaims? access, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var session = await store.FindByFingerprintAsync(RefreshTokenFingerprint.Compute(refreshToken), cancellationToken);
        if (session is null)
        {
            return;
        }

        if (access is not null)
        {
            var owner = await store.FindByIdAsync(session.UserId, cancellationToken);
            if (owner is null || !string.Equals(owner.Sub, access.Sub, StringComparison.Ordinal))
            {
                LogOwnerMismatch(logger);
                return;
            }
        }

        await store.RevokeFamilyAsync(session.FamilyId, RevocationReasons.Logout, now.UtcDateTime, cancellationToken);
        LogLoggedOut(logger);
        var timeToLive = access is null ? TimeSpan.Zero : access.ExpiresAt - now;
        if (revocations.Enabled && access is not null && timeToLive > TimeSpan.Zero)
        {
            await revocations.RevokeAsync(access.Jti, timeToLive, cancellationToken);
        }
    }

    [LoggerMessage(EventId = 2020, Level = LogLevel.Information, Message = "Session closed")]
    private static partial void LogLoggedOut(ILogger logger);

    [LoggerMessage(EventId = 2021, Level = LogLevel.Warning, Message = "Session close ignored: access token and refresh token belong to different accounts")]
    private static partial void LogOwnerMismatch(ILogger logger);
}
