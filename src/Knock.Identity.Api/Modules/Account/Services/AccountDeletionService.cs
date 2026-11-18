using Knock.Identity.Api.Common.Errors;
using Knock.Identity.Api.Modules.Account.Data;
using Knock.Identity.Api.Modules.Account.Domain;
using Knock.Identity.Api.Modules.Sesiones.Services;
using Knock.Identity.Api.Modules.Usuarios.Services;

namespace Knock.Identity.Api.Modules.Account.Services;

/// <summary>Lo que manda la app al pedir la eliminación. El motivo se valida pero no se guarda ni se registra: la spec no le da campo (§6) y R-26 lo prohíbe en los registros.</summary>
internal sealed record DeleteAccountInput(string? FirebaseIdToken)
{
    /// <summary>Sin datos: un registro imprimiría el token.</summary>
    public sealed override string ToString() => nameof(DeleteAccountInput);
}

/// <summary>Resultado de pedir la eliminación.</summary>
internal abstract record DeletionOutcome
{
    private DeletionOutcome()
    {
    }

    /// <summary>202: la cuenta quedó programada y vence la gracia en <paramref name="DeleteAt"/>.</summary>
    public sealed record Scheduled(DateTime DeleteAt) : DeletionOutcome;

    /// <summary>La sesión tiene más de 24 horas y falta el token de Firebase (422 en <c>firebaseIdToken</c>).</summary>
    public sealed record ReauthRequired : DeletionOutcome;

    /// <summary>El token de Firebase no se pudo verificar o no es de esta cuenta (422 en <c>firebaseIdToken</c>).</summary>
    public sealed record ReauthRejected : DeletionOutcome;
}

/// <summary>
/// Eliminación de la cuenta con 7 días de gracia (spec 039, USR-10 y USR-11; R-25). Al programarla se cancelan de inmediato las solicitudes abiertas y las
/// cotizaciones pendientes en Mercado y la cuenta pasa a <c>eliminacion_programada</c> con <c>eliminarEn</c>; el borrado lo ejecuta la tarea
/// <c>EliminarCuenta</c> de la spec 054 al vencer la gracia (todavía no implementada). Cancelar durante la gracia devuelve la cuenta a <c>activa</c>.
/// Ningún dato personal, motivo ni token llega a los registros (R-26).
/// </summary>
internal sealed partial class AccountDeletionService(
    IAccountStore store,
    IMarketAccountClient market,
    IFirebaseIdTokenVerifier verifier,
    PhoneProtector phones,
    ProfileCacheInvalidator profileCache,
    TimeProvider timeProvider,
    ILogger<AccountDeletionService> logger)
{
    /// <summary>Días de gracia antes de borrar (R-15).</summary>
    public static readonly TimeSpan GracePeriod = TimeSpan.FromDays(7);

    /// <summary>A partir de cuánto la sesión se considera antigua y exige re-autenticar con Firebase (R-25).</summary>
    public static readonly TimeSpan ReauthAfter = TimeSpan.FromHours(24);

    /// <exception cref="DomainException">409 <c>deletion_not_allowed</c> (ya programada, cuenta no activa o visita en curso); 404 si la cuenta ya no existe; 503 si Mercado no responde.</exception>
    public async Task<DeletionOutcome> ScheduleAsync(string sub, string currentSessionId, DeleteAccountInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        var account = await store.FindBySubAsync(sub, cancellationToken)
            ?? throw new DomainException(DomainErrorKind.NotFound, "The account of the token does not exist.");
        if (account.State != AccountStates.Active)
        {
            LogRefused(logger);
            throw NotAllowed();
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (await ReauthenticationAsync(account, currentSessionId, input, now, cancellationToken) is { } failed)
        {
            return failed;
        }

        // Antes de tocar nada: una visita en curso bloquea la eliminación (R-25, D-07).
        if ((await market.GetPendingAsync(sub, cancellationToken)).VisitInProgress)
        {
            LogRefused(logger);
            throw NotAllowed();
        }

        await market.CancelOpenAsync(sub, cancellationToken);
        var deleteAt = now + GracePeriod;
        if (!await store.ScheduleDeletionAsync(account.Id, deleteAt, now, cancellationToken))
        {
            // Otra petición la programó entre la lectura y la escritura: la regla de estado impide el duplicado (R-30).
            LogRefused(logger);
            throw NotAllowed();
        }

        await ForgetProfileAsync(sub, cancellationToken);
        LogScheduled(logger);
        return new DeletionOutcome.Scheduled(deleteAt);
    }

    /// <exception cref="DomainException">409 <c>no_deletion_scheduled</c> si no hay una eliminación programada en gracia; 404 si la cuenta ya no existe.</exception>
    public async Task CancelAsync(string sub, CancellationToken cancellationToken)
    {
        var account = await store.FindBySubAsync(sub, cancellationToken)
            ?? throw new DomainException(DomainErrorKind.NotFound, "The account of the token does not exist.");
        if (!await store.CancelDeletionAsync(account.Id, timeProvider.GetUtcNow().UtcDateTime, cancellationToken))
        {
            LogNothingToCancel(logger);
            throw new DomainException(DomainErrorKind.Conflict, "There is no scheduled deletion to cancel.", AccountErrorCodes.NoDeletionScheduled);
        }

        await ForgetProfileAsync(sub, cancellationToken);
        LogCancelled(logger);
    }

    /// <summary>Con una sesión de más de 24 horas exige el token de Firebase del número de la cuenta; <c>null</c> si no hace falta o ya se probó.</summary>
    private async Task<DeletionOutcome?> ReauthenticationAsync(
        AccountSnapshot account, string currentSessionId, DeleteAccountInput input, DateTime now, CancellationToken cancellationToken)
    {
        var started = await store.FindSessionStartAsync(currentSessionId, account.Id, cancellationToken);
        if (started is { } start && now - start <= ReauthAfter)
        {
            return null;
        }

        if (string.IsNullOrEmpty(input.FirebaseIdToken))
        {
            return new DeletionOutcome.ReauthRequired();
        }

        var identity = await verifier.VerifyAsync(input.FirebaseIdToken, cancellationToken);
        if (identity is null
            || UserIdDerivation.Derive(identity.Uid).ToString("D") != account.Sub
            || !phones.Hash(identity.PhoneNumber).Value.AsSpan().SequenceEqual(account.PhoneHash))
        {
            LogReauthRejected(logger);
            return new DeletionOutcome.ReauthRejected();
        }

        return null;
    }

    // La caché del perfil lleva el estado de la cuenta: se borra, pero un fallo de Redis no deshace una escritura ya confirmada.
    private async Task ForgetProfileAsync(string sub, CancellationToken cancellationToken)
    {
        if (!await profileCache.TryForgetAsync(sub, cancellationToken))
        {
            LogCacheNotCleared(logger);
        }
    }

    private static DomainException NotAllowed() =>
        new(DomainErrorKind.Conflict, "The account deletion is not allowed now.", AccountErrorCodes.DeletionNotAllowed);

    [LoggerMessage(EventId = 3911, Level = LogLevel.Information, Message = "Account deletion scheduled")]
    private static partial void LogScheduled(ILogger logger);

    [LoggerMessage(EventId = 3912, Level = LogLevel.Information, Message = "Account deletion refused: already scheduled, not active or a visit in progress")]
    private static partial void LogRefused(ILogger logger);

    [LoggerMessage(EventId = 3913, Level = LogLevel.Warning, Message = "Account deletion refused: re-authentication rejected")]
    private static partial void LogReauthRejected(ILogger logger);

    [LoggerMessage(EventId = 3914, Level = LogLevel.Information, Message = "Account deletion cancelled")]
    private static partial void LogCancelled(ILogger logger);

    [LoggerMessage(EventId = 3915, Level = LogLevel.Information, Message = "Account deletion cancel refused: nothing scheduled")]
    private static partial void LogNothingToCancel(ILogger logger);

    [LoggerMessage(EventId = 3916, Level = LogLevel.Warning, Message = "Profile cache could not be cleared after an account state change")]
    private static partial void LogCacheNotCleared(ILogger logger);
}
