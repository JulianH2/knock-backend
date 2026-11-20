using Knock.Identity.Api.Modules.Panel.Data;
using Knock.Identity.Api.Modules.Panel.Domain;
using QRCoder;

namespace Knock.Identity.Api.Modules.Panel.Services;

/// <summary>Resultado de consumir el enlace de activación (R-29): el secreto en forma de dirección y de QR, o «inválido» (410) con el mismo cuerpo para cualquier enlace inservible.</summary>
internal abstract record ActivationOutcome
{
    private ActivationOutcome()
    {
    }

    /// <summary>200: el secreto se muestra una sola vez.</summary>
    public sealed record Activated(string OtpauthUri, string QrSvg) : ActivationOutcome
    {
        public override string ToString() => nameof(Activated);
    }

    /// <summary>410 <c>activation_invalid</c>: desconocido, vencido, ya consumido o de una cuenta que no está en <c>pendiente</c>.</summary>
    public sealed record Invalid : ActivationOutcome;
}

/// <summary>
/// Activación del TOTP con el enlace de un solo uso (spec 033, R-29). El token solo existe en el enlace y en la memoria de esta petición: de él se guarda
/// únicamente la huella SHA-256. El consumo es una transacción que marca la invitación y pasa la cuenta a <c>configurado</c> con el secreto de 160 bits
/// cifrado; de dos consumos simultáneos gana uno, y un enlace nunca reemplaza un TOTP ya configurado. El token, el secreto y la dirección
/// <c>otpauth://</c> no llegan a los registros, a la telemetría ni a la auditoría.
/// </summary>
internal sealed class ActivacionPanelService(
    IPanelActivacionesStore invitations,
    IPanelCuentasStore accounts,
    IPanelCuentaCache cache,
    TotpService totp,
    AuditoriaPanel audit,
    PanelSettings settings,
    TimeProvider timeProvider)
{
    public async Task<ActivationOutcome> ActivateAsync(string token, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(token);
        if (!settings.IsConfigured)
        {
            throw PanelInfra.Unavailable();
        }

        return await PanelInfra.GuardAsync<ActivationOutcome>(async () =>
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var fingerprint = ActivationFingerprint.Compute(token);
            var live = await invitations.FindLiveAsync(fingerprint, now, cancellationToken);
            if (live is null)
            {
                return new ActivationOutcome.Invalid();
            }

            var account = await accounts.FindBySubAsync(live.Sub, cancellationToken);
            if (account is null)
            {
                return new ActivationOutcome.Invalid();
            }

            var secret = TotpService.GenerateSecret();
            var protectedSecret = totp.Protect(secret, live.Sub);

            // La auditoría va antes del consumo: sin rastro no hay activación (R-24).
            await audit.WriteAsync(PanelAuditValues.TipoAdministracion, PanelAuditValues.TotpActivado, PanelAuditValues.ResultadoPermitido, sub: live.Sub, cancellationToken: cancellationToken);
            var result = await invitations.ConsumeAsync(fingerprint, live.CuentaId, new ProtectedSecretForStore(protectedSecret.Ciphertext, protectedSecret.KeyVersion), now, cancellationToken);
            if (result != ActivacionResultado.Consumida)
            {
                return new ActivationOutcome.Invalid();
            }

            await cache.DeleteAsync(live.Sub, cancellationToken);
            var uri = TotpService.BuildOtpauthUri(secret, account.Nombre);
            return new ActivationOutcome.Activated(uri, RenderQrSvg(uri));
        });
    }

    internal static string RenderQrSvg(string content)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
        using var svg = new SvgQRCode(data);
        return svg.GetGraphic(4);
    }
}
