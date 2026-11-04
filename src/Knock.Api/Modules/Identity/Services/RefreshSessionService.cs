using Knock.Api.Common.Errors;
using Knock.Api.Modules.Identity.Data;
using Knock.Api.Modules.Identity.Domain;

namespace Knock.Api.Modules.Identity.Services;

/// <summary>Lo que manda la app al renovar: el refresh token vigente y el <c>deviceId</c> del canje.</summary>
internal sealed record RefreshCommand(string RefreshToken, string DeviceId);

/// <summary>Sesión renovada: access token y refresh token nuevos y si el perfil ya está completo.</summary>
internal sealed record RefreshResult(IssuedSession Session, bool ProfileComplete);

/// <summary>
/// Renovación de la sesión con rotación (spec HU-003): cada refresh token sirve una sola vez; presentar uno ya reemplazado revoca toda su familia.
/// Todos los rechazos 401 se parecen (devuelven <c>null</c>) y ocurren antes de leer la cuenta. Ni los tokens, ni las huellas, ni el
/// <c>deviceId</c>, ni el <c>sub</c> llegan a los registros.
/// </summary>
internal sealed partial class RefreshSessionService(
    SessionTokenIssuer issuer,
    IRefreshSessionStore store,
    TimeProvider timeProvider,
    ILogger<RefreshSessionService> logger)
{
    /// <returns>La sesión nueva, o <c>null</c> si el refresh se rechaza (desconocido, vencido, reemplazado, revocado u otro dispositivo).</returns>
    /// <exception cref="DomainException">403 <c>account_suspended</c>.</exception>
    public async Task<RefreshResult?> RenewAsync(RefreshCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var nextRefreshToken = SessionTokenIssuer.NewRefreshToken();
        var rotation = await store.RotateAsync(
            RefreshTokenFingerprint.Compute(command.RefreshToken), RefreshTokenFingerprint.Compute(nextRefreshToken), now, cancellationToken);

        if (rotation.Status == RotationStatus.Reused)
        {
            LogReused(logger);
            return null;
        }

        if (rotation.Parent is not { } parent)
        {
            LogRejected(logger);
            return null;
        }

        if (!string.Equals(parent.DeviceId, command.DeviceId, StringComparison.Ordinal))
        {
            await store.RevokeFamilyAsync(parent.FamilyId, RevocationReasons.Device, now, cancellationToken);
            LogOtherDevice(logger);
            return null;
        }

        var account = await store.FindByIdAsync(parent.UserId, cancellationToken);
        if (account is null)
        {
            // La cuenta ya no existe: la sesión no puede seguir viva.
            await store.RevokeFamilyAsync(parent.FamilyId, RevocationReasons.Suspension, now, cancellationToken);
            LogRejected(logger);
            return null;
        }

        if (account.State == UserState.Suspended)
        {
            await store.RevokeFamilyAsync(parent.FamilyId, RevocationReasons.Suspension, now, cancellationToken);
            LogSuspended(logger);
            throw new DomainException(DomainErrorKind.Forbidden, "The account is suspended.", FirebaseSessionService.AccountSuspendedCode);
        }

        var session = new IssuedSession(issuer.IssueAccessToken(Guid.Parse(account.Sub)), nextRefreshToken);
        LogRenewed(logger);
        return new RefreshResult(session, account.ProfileComplete);
    }

    [LoggerMessage(EventId = 2010, Level = LogLevel.Information, Message = "Session renewed")]
    private static partial void LogRenewed(ILogger logger);

    [LoggerMessage(EventId = 2011, Level = LogLevel.Warning, Message = "Session renewal rejected: refresh token unknown, expired or account missing")]
    private static partial void LogRejected(ILogger logger);

    [LoggerMessage(EventId = 2012, Level = LogLevel.Warning, Message = "Session renewal rejected: refresh token reuse, family revoked")]
    private static partial void LogReused(ILogger logger);

    [LoggerMessage(EventId = 2013, Level = LogLevel.Warning, Message = "Session renewal rejected: other device, family revoked")]
    private static partial void LogOtherDevice(ILogger logger);

    [LoggerMessage(EventId = 2014, Level = LogLevel.Warning, Message = "Session renewal refused: account suspended, family revoked")]
    private static partial void LogSuspended(ILogger logger);
}
