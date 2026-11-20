using System.Security.Cryptography;
using Knock.Identity.Api.Modules.Panel.Data;
using Knock.Identity.Api.Modules.Panel.Domain;
using Knock.Identity.Api.Modules.Sesiones;
using Knock.Identity.Api.Modules.Sesiones.Services;
using Microsoft.IdentityModel.Tokens;
using OtpNet;

namespace Knock.Identity.Api.Modules.Panel.Services;

/// <summary>Por qué falló un comando de la herramienta interna. Ninguno lleva datos de la persona.</summary>
internal enum PanelAdminError
{
    InvalidPhone,
    AppAccountNotFound,
    InvalidName,
    InvalidRoles,
    AccountAlreadyExists,
    AccountNotFound,
    TestSecretNotAllowed,
    InvalidTestSecret,
}

/// <summary>Resultado de un comando: error o éxito, con el token de activación si el comando crea una invitación (se imprime una sola vez en la consola).</summary>
internal sealed record PanelAdminOutcome(PanelAdminError? Error, string? ActivationToken = null)
{
    public bool Succeeded => Error is null;

    public override string ToString() => nameof(PanelAdminOutcome);
}

/// <summary>Datos de <c>panel alta</c>. El celular solo se usa para buscar la cuenta de la app por su HMAC: nunca se guarda en claro.</summary>
internal sealed record PanelAltaRequest(string Phone, string Name, IReadOnlyList<string> Roles, string? TestSecretBase32, bool IsDevelopment)
{
    public override string ToString() => nameof(PanelAltaRequest);
}

/// <summary>
/// Operaciones de la herramienta interna (spec 033, R-25 y R-26): alta, cambio de roles, deshabilitar, habilitar y reinicio del TOTP. Es la única forma de
/// crear o cambiar cuentas del panel: no hay endpoint. Cada comando que cambia algo aumenta <c>permisosVersion</c> (lo hace el almacén), borra la caché de la
/// cuenta antes de terminar, revoca sus sesiones del panel, cancela sus <c>sid</c> en Redis y escribe la auditoría. Valida los datos antes de crear nada.
/// </summary>
internal sealed class PanelAdminService(
    IPanelCuentasStore accounts,
    IPanelSesionesStore sessions,
    IPanelActivacionesStore invitations,
    IPanelCuentaCache cache,
    ISesionesApi appAccounts,
    IPanelSidCanceller cancelled,
    TotpService totp,
    AuditoriaPanel audit,
    TimeProvider timeProvider)
{
    public const int MaxNameLength = 60;
    public const int ActivationTokenBytes = 32;

    public async Task<PanelAdminOutcome> AltaAsync(PanelAltaRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length is 0 or > MaxNameLength || name.Any(char.IsControl))
        {
            return new PanelAdminOutcome(PanelAdminError.InvalidName);
        }

        if (!MapaDeRoles.AreValid(request.Roles))
        {
            return new PanelAdminOutcome(PanelAdminError.InvalidRoles);
        }

        byte[]? testSecret = null;
        if (request.TestSecretBase32 is not null)
        {
            if (!request.IsDevelopment)
            {
                return new PanelAdminOutcome(PanelAdminError.TestSecretNotAllowed);
            }

            testSecret = TryDecodeBase32(request.TestSecretBase32);
            if (testSecret is null)
            {
                return new PanelAdminOutcome(PanelAdminError.InvalidTestSecret);
            }
        }

        var sub = await ResolveSubAsync(request.Phone, cancellationToken);
        if (sub.Error is { } error)
        {
            return new PanelAdminOutcome(error);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var protectedSecret = testSecret is null ? null : totp.Protect(testSecret, sub.Value!);
        var created = await accounts.InsertAsync(
            new NuevaCuentaPanel(sub.Value!, name, [.. request.Roles], now, protectedSecret?.Ciphertext, protectedSecret?.KeyVersion ?? 0),
            cancellationToken);
        if (!created)
        {
            return new PanelAdminOutcome(PanelAdminError.AccountAlreadyExists);
        }

        string? token = null;
        if (protectedSecret is null)
        {
            var account = (await accounts.FindBySubAsync(sub.Value!, cancellationToken))!;
            token = await CreateInvitationAsync(account.Id, now, cancellationToken);
        }

        await AfterChangeAsync(sub.Value!, PanelAuditValues.Alta, revoke: false, cancellationToken);
        return new PanelAdminOutcome(null, token);
    }

    public async Task<PanelAdminOutcome> SetRolesAsync(string phone, IReadOnlyList<string> roles, CancellationToken cancellationToken)
    {
        if (!MapaDeRoles.AreValid(roles))
        {
            return new PanelAdminOutcome(PanelAdminError.InvalidRoles);
        }

        return await ChangeAsync(
            phone,
            (sub, now) => accounts.SetRolesAsync(sub, [.. roles], now, cancellationToken),
            PanelAuditValues.RolesCambiados,
            withInvitation: false,
            cancellationToken);
    }

    public Task<PanelAdminOutcome> DisableAsync(string phone, CancellationToken cancellationToken) =>
        ChangeAsync(phone, (sub, now) => accounts.SetEstadoAsync(sub, PanelCuentaEstado.Deshabilitada, now, cancellationToken), PanelAuditValues.Deshabilitada, false, cancellationToken);

    public Task<PanelAdminOutcome> EnableAsync(string phone, CancellationToken cancellationToken) =>
        ChangeAsync(phone, (sub, now) => accounts.SetEstadoAsync(sub, PanelCuentaEstado.Activa, now, cancellationToken), PanelAuditValues.Habilitada, false, cancellationToken);

    /// <summary>Deja la cuenta en <c>pendiente</c>, revoca sus sesiones y crea una invitación nueva: es la única forma de reemplazar un TOTP ya configurado (R-25).</summary>
    public Task<PanelAdminOutcome> ResetTotpAsync(string phone, CancellationToken cancellationToken) =>
        ChangeAsync(phone, (sub, now) => accounts.ResetTotpAsync(sub, now, cancellationToken), PanelAuditValues.TotpReiniciado, true, cancellationToken);

    private async Task<PanelAdminOutcome> ChangeAsync(
        string phone,
        Func<string, DateTime, Task<CuentaPanel?>> change,
        string auditEvent,
        bool withInvitation,
        CancellationToken cancellationToken)
    {
        var sub = await ResolveSubAsync(phone, cancellationToken);
        if (sub.Error is { } error)
        {
            return new PanelAdminOutcome(error);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var account = await change(sub.Value!, now);
        if (account is null)
        {
            return new PanelAdminOutcome(PanelAdminError.AccountNotFound);
        }

        var token = withInvitation ? await CreateInvitationAsync(account.Id, now, cancellationToken) : null;
        await AfterChangeAsync(sub.Value!, auditEvent, revoke: true, cancellationToken);
        return new PanelAdminOutcome(null, token);
    }

    /// <summary>Borra la caché antes de terminar (R-24), revoca las sesiones y cancela sus <c>sid</c> (R-25) y escribe la auditoría (R-23).</summary>
    private async Task AfterChangeAsync(string sub, string auditEvent, bool revoke, CancellationToken cancellationToken)
    {
        await cache.DeleteAsync(sub, cancellationToken);
        if (revoke)
        {
            foreach (var sid in await sessions.RevokeAllAsync(sub, cancellationToken))
            {
                await cancelled.CancelAsync(sid, PanelSesionReglas.AccessTokenLifetime, cancellationToken);
            }
        }

        await audit.WriteAsync(PanelAuditValues.TipoAdministracion, auditEvent, PanelAuditValues.ResultadoPermitido, sub: sub, cancellationToken: cancellationToken);
    }

    private async Task<string> CreateInvitationAsync(string accountId, DateTime now, CancellationToken cancellationToken)
    {
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(ActivationTokenBytes));
        await invitations.CreateAsync(accountId, ActivationFingerprint.Compute(token), now, now + PanelSesionReglas.ActivationLifetime, cancellationToken);
        return token;
    }

    private async Task<(string? Value, PanelAdminError? Error)> ResolveSubAsync(string phone, CancellationToken cancellationToken)
    {
        if (!PhoneNumbers.IsValid(phone))
        {
            return (null, PanelAdminError.InvalidPhone);
        }

        var sub = await appAccounts.FindSubByPhoneAsync(phone, cancellationToken);
        return sub is null ? (null, PanelAdminError.AppAccountNotFound) : (sub, null);
    }

    private static byte[]? TryDecodeBase32(string value)
    {
        try
        {
            var bytes = Base32Encoding.ToBytes(value.Trim().Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant());
            return bytes.Length >= 10 ? bytes : null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}

/// <summary>Huella SHA-256 del token de activación (R-29): lo único que se guarda de él.</summary>
internal static class ActivationFingerprint
{
    public static byte[] Compute(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
    }
}
