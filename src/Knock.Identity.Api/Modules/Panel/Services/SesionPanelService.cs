using Knock.Identity.Api.Modules.Panel.Data;
using Knock.Identity.Api.Modules.Panel.Domain;

namespace Knock.Identity.Api.Modules.Panel.Services;

/// <summary>
/// Sesiones del panel (spec 033, R-12, R-15 y R-16): abre la sesión tras el segundo paso con una vida máxima de 8 horas, el token de acceso de 15 minutos y el
/// token de renovación de 256 bits (solo su huella se guarda). Una cuenta tiene como máximo 3 sesiones abiertas: al abrir la cuarta el almacén revoca la más
/// antigua y aquí se cancela su <c>sid</c> en Redis para que el gateway rechace su token.
/// </summary>
internal sealed partial class SesionPanelService(
    IPanelSesionesStore sessions,
    IPanelSidCanceller cancelled,
    PanelTokenIssuer issuer,
    TimeProvider timeProvider,
    ILogger<SesionPanelService> logger)
{
    /// <summary>Abre una sesión nueva para la cuenta con la <c>permisosVersion</c> vigente.</summary>
    public async Task<PanelGrant> OpenAsync(CuentaPanel account, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(account);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var sid = Guid.CreateVersion7().ToString("N");
        var refreshToken = PanelRefreshToken.Generate();
        var sessionExpiry = PanelSesionReglas.SessionExpiry(now);
        var revoked = await sessions.CreateAsync(
            new NuevaSesionPanel(
                sid,
                account.Sub,
                PanelRefreshToken.Fingerprint(refreshToken),
                account.PermisosVersion,
                now,
                PanelSesionReglas.RenewalExpiry(now, sessionExpiry),
                sessionExpiry),
            cancellationToken);
        foreach (var oldSid in revoked)
        {
            await cancelled.CancelAsync(oldSid, PanelSesionReglas.AccessTokenLifetime, cancellationToken);
        }

        var access = issuer.Issue(account.Sub, sid, account.PermisosVersion, PanelSesionReglas.AccessExpiry(now, sessionExpiry));
        LogOpened(logger, revoked.Count);
        return new PanelGrant(access.Token, access.ExpiresAt, account.Permisos, account.Nombre, refreshToken);
    }

    // Los registros no llevan sub, sid, tokens ni huellas.
    [LoggerMessage(EventId = 3401, Level = LogLevel.Information, Message = "Panel session opened ({Revoked} older sessions revoked)")]
    private static partial void LogOpened(ILogger logger, int revoked);
}
