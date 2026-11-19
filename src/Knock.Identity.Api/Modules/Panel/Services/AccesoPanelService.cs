using Knock.Identity.Api.Modules.Panel.Data;
using Knock.Identity.Api.Modules.Panel.Domain;
using Knock.Identity.Api.Modules.Sesiones.Data;
using Knock.Identity.Api.Modules.Sesiones.Domain;
using Knock.Identity.Api.Modules.Sesiones.Services;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Knock.Identity.Api.Modules.Panel.Services;

/// <summary>Resultado del primer paso (R-04 a R-07). Los rechazos no llevan datos: el controller los traduce a 401, 403 o 429.</summary>
internal abstract record PhoneOutcome
{
    private PhoneOutcome()
    {
    }

    /// <summary>200: el desafío de un solo uso y cuándo vence (UTC).</summary>
    public sealed record Challenge(string ChallengeId, DateTime ExpiresAt) : PhoneOutcome
    {
        public override string ToString() => nameof(Challenge);
    }

    /// <summary>401 <c>invalid_credentials</c>: el <c>idToken</c> de Firebase no se pudo verificar.</summary>
    public sealed record InvalidCredentials : PhoneOutcome;

    /// <summary>403 <c>no_panel_access</c>: sin cuenta del panel, deshabilitada, cuenta de la app suspendida o sin TOTP. El mismo cuerpo en todos los casos.</summary>
    public sealed record NoPanelAccess : PhoneOutcome;

    /// <summary>429 <c>locked</c>: la cuenta está bloqueada; <paramref name="RetryAfter"/> es lo que falta para el fin del bloqueo.</summary>
    public sealed record Locked(TimeSpan RetryAfter) : PhoneOutcome;

    /// <summary>429 <c>limit_exceeded</c>: la IP llegó a 10 fallos de TOTP en 15 minutos.</summary>
    public sealed record LimitExceeded(TimeSpan RetryAfter) : PhoneOutcome;
}

/// <summary>Resultado del segundo paso (R-08 a R-12).</summary>
internal abstract record TotpOutcome
{
    private TotpOutcome()
    {
    }

    /// <summary>200: la sesión del panel abierta.</summary>
    public sealed record Granted(PanelGrant Grant) : TotpOutcome;

    /// <summary>401 <c>challenge_expired</c>: desafío inexistente, vencido o ya usado.</summary>
    public sealed record ChallengeExpired : TotpOutcome;

    /// <summary>401 <c>invalid_code</c>: código incorrecto, fuera de la ventana o repetido.</summary>
    public sealed record InvalidCode : TotpOutcome;

    /// <summary>403 <c>no_panel_access</c>: la cuenta dejó de poder entrar entre los dos pasos.</summary>
    public sealed record NoPanelAccess : TotpOutcome;

    /// <summary>429 <c>locked</c>.</summary>
    public sealed record Locked(TimeSpan RetryAfter) : TotpOutcome;

    /// <summary>429 <c>limit_exceeded</c>.</summary>
    public sealed record LimitExceeded(TimeSpan RetryAfter) : TotpOutcome;
}

/// <summary>
/// Acceso al panel en dos pasos (spec 033, R-04 a R-12 y R-28). Primer paso: valida el <c>idToken</c> con el verificador de Firebase de la spec 015, deriva el
/// <c>sub</c> igual que ella y abre un desafío de un solo uso. Segundo paso: valida el TOTP (ventana de ±1 paso, paso mayor, tiempo constante), cuenta los fallos
/// por cuenta en MongoDB y por IP en Redis, consume el desafío de forma atómica y abre la sesión. Cada paso escribe su auditoría antes de conceder nada: si no
/// se puede escribir, el acceso no se concede. Ni el celular, ni el <c>idToken</c>, ni los códigos, ni el <c>desafioId</c> llegan a los registros.
/// </summary>
internal sealed class AccesoPanelService(
    IFirebaseIdTokenVerifier verifier,
    IPanelCuentasStore accounts,
    ISesionesStore appAccounts,
    IPanelDesafios challenges,
    TotpService totp,
    SesionPanelService sessions,
    AuditoriaPanel audit,
    PanelSettings settings,
    TimeProvider timeProvider)
{
    public const int ChallengeIdBytes = 16;

    public async Task<PhoneOutcome> VerifyPhoneAsync(string idToken, string clientIp, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return await PanelInfra.GuardAsync<PhoneOutcome>(async () =>
        {
            if (await IpLimitAsync(clientIp, cancellationToken) is { } limited)
            {
                return new PhoneOutcome.LimitExceeded(limited);
            }

            var identity = await verifier.VerifyAsync(idToken, cancellationToken);
            if (identity is null)
            {
                await audit.WriteAsync(PanelAuditValues.TipoAcceso, PanelAuditValues.CelularRechazado, PanelAuditValues.ResultadoFallido, cancellationToken: cancellationToken);
                return new PhoneOutcome.InvalidCredentials();
            }

            var sub = UserIdDerivation.Derive(identity.Uid).ToString("D");
            var account = await accounts.FindBySubAsync(sub, cancellationToken);
            if (!await CanEnterAsync(account, sub, cancellationToken))
            {
                await audit.WriteAsync(PanelAuditValues.TipoAcceso, PanelAuditValues.CelularRechazado, PanelAuditValues.ResultadoDenegado, sub: sub, cancellationToken: cancellationToken);
                return new PhoneOutcome.NoPanelAccess();
            }

            var now = timeProvider.GetUtcNow().UtcDateTime;
            if (account!.EstaBloqueada(now))
            {
                await audit.WriteAsync(PanelAuditValues.TipoAcceso, PanelAuditValues.Bloqueo, PanelAuditValues.ResultadoDenegado, sub: sub, cancellationToken: cancellationToken);
                return new PhoneOutcome.Locked(account.BloqueadoHasta!.Value - now);
            }

            // La auditoría va antes del desafío: si no se puede escribir, no queda desafío y el acceso no avanza (R-24).
            await audit.WriteAsync(PanelAuditValues.TipoAcceso, PanelAuditValues.CelularVerificado, PanelAuditValues.ResultadoPermitido, sub: sub, cancellationToken: cancellationToken);
            var challengeId = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(ChallengeIdBytes));
            await challenges.CreateAsync(challengeId, sub, PanelSesionReglas.ChallengeLifetime, cancellationToken);
            return new PhoneOutcome.Challenge(challengeId, now + PanelSesionReglas.ChallengeLifetime);
        });
    }

    public async Task<TotpOutcome> VerifyTotpAsync(string challengeId, string code, string clientIp, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        return await PanelInfra.GuardAsync<TotpOutcome>(async () =>
        {
            if (await IpLimitAsync(clientIp, cancellationToken) is { } limited)
            {
                return new TotpOutcome.LimitExceeded(limited);
            }

            var sub = await challenges.PeekAsync(challengeId, cancellationToken);
            if (sub is null)
            {
                await audit.WriteAsync(PanelAuditValues.TipoAcceso, PanelAuditValues.TotpFallido, PanelAuditValues.ResultadoFallido, cancellationToken: cancellationToken);
                return new TotpOutcome.ChallengeExpired();
            }

            // Se lee la cuenta de nuevo: un cambio de roles entre los dos pasos queda reflejado en la sesión que se emite (§11).
            var account = await accounts.FindBySubAsync(sub, cancellationToken);
            if (!await CanEnterAsync(account, sub, cancellationToken))
            {
                await challenges.ConsumeAsync(challengeId, cancellationToken);
                await audit.WriteAsync(PanelAuditValues.TipoAcceso, PanelAuditValues.TotpFallido, PanelAuditValues.ResultadoDenegado, sub: sub, cancellationToken: cancellationToken);
                return new TotpOutcome.NoPanelAccess();
            }

            var now = timeProvider.GetUtcNow().UtcDateTime;
            if (account!.EstaBloqueada(now))
            {
                await audit.WriteAsync(PanelAuditValues.TipoAcceso, PanelAuditValues.Bloqueo, PanelAuditValues.ResultadoDenegado, sub: sub, cancellationToken: cancellationToken);
                return new TotpOutcome.Locked(account.BloqueadoHasta!.Value - now);
            }

            var secret = totp.Unprotect(account.TotpSecretoCifrado!, account.TotpVersionLlave, sub);
            var step = TotpService.Verify(secret, code, timeProvider.GetUtcNow(), account.TotpUltimoPaso);
            if (step is null)
            {
                return await FailAsync(sub, clientIp, now, cancellationToken);
            }

            // El desafío se consume de forma atómica: de dos peticiones con el mismo desafío y el mismo código solo una pasa de aquí (R-12).
            if (await challenges.ConsumeAsync(challengeId, cancellationToken) is null)
            {
                return new TotpOutcome.ChallengeExpired();
            }

            // Y el paso se acepta con una actualización condicionada a que sea mayor: protege contra la repetición del mismo código (R-09).
            if (!await accounts.TryAcceptStepAsync(sub, step.Value, cancellationToken))
            {
                return await FailAsync(sub, clientIp, now, cancellationToken);
            }

            await accounts.ResetFailuresAsync(sub, cancellationToken);
            await audit.WriteAsync(PanelAuditValues.TipoAcceso, PanelAuditValues.AccesoConcedido, PanelAuditValues.ResultadoPermitido, sub: sub, cancellationToken: cancellationToken);
            return new TotpOutcome.Granted(await sessions.OpenAsync(account, cancellationToken));
        });
    }

    private async Task<TotpOutcome> FailAsync(string sub, string clientIp, DateTime now, CancellationToken cancellationToken)
    {
        // El conteo por cuenta es persistente y atómico (MongoDB, R-11); el de la IP vive en Redis. Un desafío nuevo no reinicia ninguno.
        var after = await accounts.RegisterFailureAsync(sub, now, cancellationToken);
        await challenges.RegisterIpFailureAsync(clientIp, PanelSesionReglas.FailureWindow, cancellationToken);
        await audit.WriteAsync(PanelAuditValues.TipoAcceso, PanelAuditValues.TotpFallido, PanelAuditValues.ResultadoFallido, sub: sub, cancellationToken: cancellationToken);
        if (after is not null && after.EstaBloqueada(now))
        {
            await audit.WriteAsync(PanelAuditValues.TipoAcceso, PanelAuditValues.Bloqueo, PanelAuditValues.ResultadoFallido, sub: sub, cancellationToken: cancellationToken);
        }

        return new TotpOutcome.InvalidCode();
    }

    /// <summary>Con 10 fallos de la IP en la ventana responde 429; la espera que se anuncia es la ventana completa (Redis solo cuenta, no devuelve el resto).</summary>
    private async Task<TimeSpan?> IpLimitAsync(string clientIp, CancellationToken cancellationToken) =>
        await challenges.IpFailuresAsync(clientIp, cancellationToken) >= PanelSesionReglas.MaxIpFailures ? PanelSesionReglas.FailureWindow : null;

    /// <summary>Cuenta del panel activa, con TOTP, y cuenta de la app existente y no suspendida (R-06).</summary>
    private async Task<bool> CanEnterAsync(CuentaPanel? account, string sub, CancellationToken cancellationToken)
    {
        if (account is null || !account.EstaActiva || !account.TieneTotp)
        {
            return false;
        }

        var appAccount = await appAccounts.FindBySubAsync(sub, cancellationToken);
        return appAccount is not null && appAccount.State != AccountState.Suspended;
    }

    private void EnsureEnabled()
    {
        if (!settings.IsConfigured)
        {
            throw PanelInfra.Unavailable();
        }
    }
}
