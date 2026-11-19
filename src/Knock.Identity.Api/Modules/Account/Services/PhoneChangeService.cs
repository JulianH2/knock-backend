using Knock.Identity.Api.Common.Errors;
using Knock.Identity.Api.Modules.Account.Data;
using Knock.Identity.Api.Modules.Account.Domain;
using Knock.Identity.Api.Modules.Sesiones.Services;

namespace Knock.Identity.Api.Modules.Account.Services;

/// <summary>
/// Cambio de número (spec 039, USR-03 y USR-04). El SMS lo envía Firebase en el teléfono (spec 015): aquí solo se comprueba la disponibilidad
/// antes de gastarlo y, después, se valida el token del número nuevo. Ni el celular ni el token llegan a los registros (R-26).
/// </summary>
internal sealed partial class PhoneChangeService(
    IAccountStore store,
    IFirebaseIdTokenVerifier verifier,
    PhoneProtector phones,
    ISessionCanceller cancelled,
    IAccountOutbox outbox,
    TimeProvider timeProvider,
    ILogger<PhoneChangeService> logger)
{
    /// <summary>Comprueba que el número no tenga cuenta, antes de que la app gaste un SMS. El número de la propia persona también cuenta como registrado.</summary>
    /// <exception cref="DomainException">409 <c>phone_already_registered</c>.</exception>
    public async Task EnsureAvailableAsync(string newPhone, CancellationToken cancellationToken)
    {
        if (await store.PhoneHashExistsAsync(phones.Hash(newPhone).Value, cancellationToken))
        {
            LogPhoneTaken(logger);
            throw PhoneTaken();
        }
    }

    /// <summary>
    /// Confirma el cambio (USR-04): valida el token del número nuevo como el inicio de sesión (spec 015), cambia el celular protegido como en el registro, deja
    /// el aviso <c>sesion_revocada</c> en las demás sesiones, las cierra y conserva la actual.
    /// </summary>
    /// <returns>Falso si el token no se puede verificar (la ruta responde 422 en <c>firebaseIdToken</c>, sin decir por qué); verdadero si el celular cambió.</returns>
    /// <exception cref="DomainException">409 <c>phone_already_registered</c>; 404 si la cuenta del token ya no existe.</exception>
    public async Task<bool> ConfirmAsync(string sub, string currentSessionId, string firebaseIdToken, CancellationToken cancellationToken)
    {
        var identity = await verifier.VerifyAsync(firebaseIdToken, cancellationToken);
        if (identity is null)
        {
            LogTokenRejected(logger);
            return false;
        }

        var account = await store.FindBySubAsync(sub, cancellationToken)
            ?? throw new DomainException(DomainErrorKind.NotFound, "The account of the token does not exist.");
        var hash = phones.Hash(identity.PhoneNumber);
        if (hash.Value.AsSpan().SequenceEqual(account.PhoneHash) || await store.PhoneHashExistsAsync(hash.Value, cancellationToken))
        {
            LogPhoneTaken(logger);
            throw PhoneTaken();
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        // El aviso va antes de cerrar: la sesión cerrada ya no podría recibirlo (spec 037, R-13).
        foreach (var other in await store.FindOtherOpenSessionIdsAsync(account.Id, currentSessionId, now, cancellationToken))
        {
            await outbox.EnqueueSessionRevokedAsync(other, cancellationToken);
        }

        var (outcome, revoked) = await store.ChangePhoneAsync(
            account.Id, phones.Protect(identity.PhoneNumber, account.Id), hash, currentSessionId, now, cancellationToken);
        if (outcome == PhoneChangeOutcome.PhoneTaken)
        {
            LogPhoneTaken(logger);
            throw PhoneTaken();
        }

        foreach (var sessionId in revoked)
        {
            await cancelled.CancelAsync(sessionId, SessionTokenIssuer.AccessTokenLifetime, cancellationToken);
        }

        LogChanged(logger, revoked.Count);
        return true;
    }

    internal static DomainException PhoneTaken() =>
        new(DomainErrorKind.Conflict, "The phone number already belongs to an account.", AccountErrorCodes.PhoneAlreadyRegistered);

    // Los registros no llevan celular, hash ni token.
    [LoggerMessage(EventId = 3901, Level = LogLevel.Information, Message = "Phone change refused: number already registered")]
    private static partial void LogPhoneTaken(ILogger logger);

    [LoggerMessage(EventId = 3902, Level = LogLevel.Warning, Message = "Phone change refused: the Firebase token was rejected")]
    private static partial void LogTokenRejected(ILogger logger);

    [LoggerMessage(EventId = 3903, Level = LogLevel.Information, Message = "Phone changed; {ClosedSessions} other sessions closed")]
    private static partial void LogChanged(ILogger logger, int closedSessions);
}
