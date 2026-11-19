using Knock.Identity.Api.Modules.Panel.Data;
using Knock.Identity.Api.Modules.Panel.Domain;
using Knock.Identity.Api.Modules.Sesiones.Data;

namespace Knock.Identity.Api.Modules.Panel.Services;

/// <summary>Resultado de renovar (R-17 y R-18). Ningún rechazo lleva datos de la persona.</summary>
internal abstract record RenewOutcome
{
    private RenewOutcome()
    {
    }

    /// <summary>200: token nuevo y cookie nueva.</summary>
    public sealed record Renewed(PanelGrant Grant) : RenewOutcome;

    /// <summary>401 <c>invalid_session</c>. <paramref name="ClearCookie"/> es falso en la renovación concurrente tolerada (no se borra la cookie).</summary>
    public sealed record InvalidSession(bool ClearCookie) : RenewOutcome;

    /// <summary>401 <c>permissions_changed</c>: cuenta deshabilitada o <c>permisosVersion</c> distinta; la sesión ya quedó revocada.</summary>
    public sealed record PermissionsChanged : RenewOutcome;

    /// <summary>403 <c>origin_not_allowed</c>: sin <c>X-Knock-Panel: 1</c> o con otro <c>Origin</c>; no se rotó nada.</summary>
    public sealed record OriginNotAllowed : RenewOutcome;
}

/// <summary>
/// Renovación y cierre de la sesión del panel (spec 033, R-17, R-18 y R-19). La rotación es una sola actualización condicionada a la huella vigente (de dos
/// renovaciones simultáneas solo una gana). La huella inmediatamente anterior, dentro de 10 segundos, es una renovación concurrente tolerada (401 sin
/// revocar); cualquier otra huella usada, o la anterior pasada la gracia, es un token robado: se revocan todas las sesiones de la cuenta y se cancelan sus
/// <c>sid</c> en Redis. Solo se manejan huellas, nunca el token en claro.
/// </summary>
internal sealed class SesionPanelCicloService(
    IPanelSesionesStore sessions,
    IPanelCuentasStore accounts,
    ISesionCancelada cancelled,
    PanelTokenIssuer issuer,
    AuditoriaPanel audit,
    PanelSettings settings,
    TimeProvider timeProvider)
{
    public async Task<RenewOutcome> RenewAsync(string? refreshToken, string? marker, string? origin, CancellationToken cancellationToken)
    {
        if (!settings.IsConfigured)
        {
            throw PanelInfra.Unavailable();
        }

        return await PanelInfra.GuardAsync<RenewOutcome>(async () =>
        {
            if (marker != "1" || !settings.OriginMatches(origin))
            {
                await audit.WriteAsync(PanelAuditValues.TipoSesion, PanelAuditValues.Renovacion, PanelAuditValues.ResultadoDenegado, cancellationToken: cancellationToken);
                return new RenewOutcome.OriginNotAllowed();
            }

            if (string.IsNullOrEmpty(refreshToken))
            {
                return new RenewOutcome.InvalidSession(true);
            }

            var now = timeProvider.GetUtcNow().UtcDateTime;
            var fingerprint = PanelRefreshToken.Fingerprint(refreshToken);
            var session = await sessions.FindByFingerprintAsync(fingerprint, cancellationToken);
            if (session is null)
            {
                return await UnknownOrReusedAsync(fingerprint, now, cancellationToken);
            }

            if (PanelSesionReglas.CannotRenew(session, now))
            {
                await audit.WriteAsync(PanelAuditValues.TipoSesion, PanelAuditValues.Renovacion, PanelAuditValues.ResultadoFallido, session.Sub, session.Id, cancellationToken: cancellationToken);
                return new RenewOutcome.InvalidSession(true);
            }

            // Antes de renovar se vuelve a leer la cuenta (R-18).
            var account = await accounts.FindBySubAsync(session.Sub, cancellationToken);
            if (account is null || !account.EstaActiva || account.PermisosVersion != session.PermisosVersion)
            {
                await sessions.RevokeAsync(session.Id, cancellationToken);
                await cancelled.CancelAsync(session.Id, PanelSesionReglas.AccessTokenLifetime, cancellationToken);
                await audit.WriteAsync(PanelAuditValues.TipoSesion, PanelAuditValues.PermisosCambiados, PanelAuditValues.ResultadoDenegado, session.Sub, session.Id, cancellationToken: cancellationToken);
                return new RenewOutcome.PermissionsChanged();
            }

            var newRefresh = PanelRefreshToken.Generate();
            var rotated = await sessions.RotateAsync(
                session.Id,
                fingerprint,
                PanelRefreshToken.Fingerprint(newRefresh),
                now,
                PanelSesionReglas.RenewalExpiry(now, session.VenceEn),
                cancellationToken);
            if (!rotated)
            {
                // Otra renovación con la misma cookie ganó la rotación: esta pierde sin revocar nada ni borrar la cookie.
                await audit.WriteAsync(PanelAuditValues.TipoSesion, PanelAuditValues.RenovacionConcurrente, PanelAuditValues.ResultadoDenegado, session.Sub, session.Id, cancellationToken: cancellationToken);
                return new RenewOutcome.InvalidSession(false);
            }

            var access = issuer.Issue(account.Sub, session.Id, account.PermisosVersion, PanelSesionReglas.AccessExpiry(now, session.VenceEn));
            await audit.WriteAsync(PanelAuditValues.TipoSesion, PanelAuditValues.Renovacion, PanelAuditValues.ResultadoPermitido, session.Sub, session.Id, cancellationToken: cancellationToken);
            return new RenewOutcome.Renewed(new PanelGrant(access.Token, access.ExpiresAt, account.Permisos, account.Nombre, newRefresh));
        });
    }

    /// <summary>Cierra la sesión del <c>sid</c> (R-19): la revoca, cancela el <c>sid</c> en Redis por lo que le quede al token y audita.</summary>
    public async Task CloseAsync(string sid, string sub, TimeSpan remaining, CancellationToken cancellationToken)
    {
        if (!settings.IsConfigured)
        {
            throw PanelInfra.Unavailable();
        }

        await PanelInfra.GuardAsync(async () =>
        {
            await sessions.RevokeAsync(sid, cancellationToken);
            await cancelled.CancelAsync(sid, remaining > TimeSpan.Zero ? remaining : TimeSpan.FromSeconds(1), cancellationToken);
            await audit.WriteAsync(PanelAuditValues.TipoSesion, PanelAuditValues.Cierre, PanelAuditValues.ResultadoPermitido, sub, sid, cancellationToken: cancellationToken);
            return true;
        });
    }

    private async Task<RenewOutcome> UnknownOrReusedAsync(string fingerprint, DateTime now, CancellationToken cancellationToken)
    {
        var used = await sessions.FindByUsedFingerprintAsync(fingerprint, cancellationToken);
        if (used is null)
        {
            await audit.WriteAsync(PanelAuditValues.TipoSesion, PanelAuditValues.Renovacion, PanelAuditValues.ResultadoFallido, cancellationToken: cancellationToken);
            return new RenewOutcome.InvalidSession(true);
        }

        if (PanelReuseClassifier.Classify(used, fingerprint, now) == PanelReuse.Concurrent)
        {
            await audit.WriteAsync(PanelAuditValues.TipoSesion, PanelAuditValues.RenovacionConcurrente, PanelAuditValues.ResultadoDenegado, used.Sub, used.Id, cancellationToken: cancellationToken);
            return new RenewOutcome.InvalidSession(false);
        }

        foreach (var sid in await sessions.RevokeAllAsync(used.Sub, cancellationToken))
        {
            await cancelled.CancelAsync(sid, PanelSesionReglas.AccessTokenLifetime, cancellationToken);
        }

        await audit.WriteAsync(PanelAuditValues.TipoSesion, PanelAuditValues.ReusoDetectado, PanelAuditValues.ResultadoDenegado, used.Sub, used.Id, cancellationToken: cancellationToken);
        return new RenewOutcome.InvalidSession(true);
    }
}
