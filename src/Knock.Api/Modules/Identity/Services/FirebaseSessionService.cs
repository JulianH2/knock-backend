using Knock.Api.Common.Errors;
using Knock.Api.Modules.Identity.Data;
using Knock.Api.Modules.Identity.Domain;
using MongoDB.Bson;

namespace Knock.Api.Modules.Identity.Services;

/// <summary>Datos de un canje que no vienen del token: lo que manda la app y lo que sale de la petición.</summary>
internal sealed record ExchangeRequest(string FirebaseIdToken, string DeviceId, string CorrelationId, string TraceId);

/// <summary>Resultado de un canje: la sesión y si la cuenta se creó en este canje (201) o ya existía (200).</summary>
internal sealed record ExchangeResult(IssuedSession Session, bool IsNew, bool ProfileComplete);

/// <summary>
/// Canjea un ID token de Firebase por la sesión propia (spec HU-001): deriva el <c>sub</c>, busca la cuenta por <c>sub</c> y por el HMAC del celular,
/// crea la cuenta completa en una transacción si no existe y guarda solo la huella del refresh token. Ni el celular ni el <c>uid</c> ni los tokens
/// llegan a los registros.
/// </summary>
internal sealed partial class FirebaseSessionService(
    IFirebaseIdTokenVerifier verifier,
    SessionTokenIssuer issuer,
    PhoneProtector phones,
    IIdentityStore store,
    TimeProvider timeProvider,
    ILogger<FirebaseSessionService> logger)
{
    public const string AccountSuspendedCode = "account_suspended";
    public const string AccountConflictCode = "account_conflict";

    /// <returns>El resultado, o <c>null</c> si el token se rechaza (inválido, vencido, sin teléfono +52 o de otro proyecto).</returns>
    /// <exception cref="DomainException">403 <c>account_suspended</c> o 409 <c>account_conflict</c>.</exception>
    public async Task<ExchangeResult?> ExchangeAsync(ExchangeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var identity = await verifier.VerifyAsync(request.FirebaseIdToken, cancellationToken);
        if (identity is null)
        {
            LogRejected(logger);
            return null;
        }

        var sub = UserIdDerivation.Derive(identity.Uid);
        var phoneHash = phones.Hash(identity.PhoneNumber);
        var bySub = await store.FindBySubAsync(sub.ToString("D"), cancellationToken);
        var byPhone = await store.FindByPhoneHashAsync(phoneHash.Value, cancellationToken);

        if (bySub is null && byPhone is null)
        {
            return await CreateAsync(request, identity, sub, phoneHash, cancellationToken);
        }

        if (bySub is null || byPhone is null || bySub.Id != byPhone.Id)
        {
            throw Conflict();
        }

        return await ExistingAsync(request, bySub, sub, cancellationToken);
    }

    private async Task<ExchangeResult> ExistingAsync(ExchangeRequest request, UserAccount account, Guid sub, CancellationToken cancellationToken)
    {
        if (account.State == UserState.Suspended)
        {
            LogSuspended(logger);
            throw new DomainException(DomainErrorKind.Forbidden, "The account is suspended.", AccountSuspendedCode);
        }

        var session = issuer.Issue(sub);
        await store.AddRefreshTokenAsync(NewRefresh(account.Id, session, request.DeviceId), cancellationToken);
        LogIssued(logger);
        return new ExchangeResult(session, IsNew: false, account.ProfileComplete);
    }

    private async Task<ExchangeResult> CreateAsync(
        ExchangeRequest request, FirebaseIdentity identity, Guid sub, PhoneHash phoneHash, CancellationToken cancellationToken)
    {
        var userId = ObjectId.GenerateNewId().ToString();
        var session = issuer.Issue(sub);
        var protectedPhone = phones.Protect(identity.PhoneNumber, userId);
        var account = new NewAccount(
            userId,
            sub.ToString("D"),
            protectedPhone.Ciphertext,
            protectedPhone.KeyVersion,
            phoneHash.Value,
            phoneHash.KeyVersion,
            request.DeviceId,
            request.CorrelationId,
            request.TraceId,
            timeProvider.GetUtcNow().UtcDateTime,
            NewRefresh(userId, session, request.DeviceId));

        if (await store.CreateAccountAsync(account, cancellationToken) == AccountCreation.Created)
        {
            LogCreated(logger);
            return new ExchangeResult(session, IsNew: true, ProfileComplete: false);
        }

        // Otro canje ganó la carrera: no se guardó nada de este; se vuelve a leer y se trata como cuenta existente (o como conflicto).
        var winner = await store.FindBySubAsync(account.Sub, cancellationToken);
        var winnerByPhone = await store.FindByPhoneHashAsync(phoneHash.Value, cancellationToken);
        if (winner is null || winnerByPhone is null || winner.Id != winnerByPhone.Id)
        {
            throw Conflict();
        }

        return await ExistingAsync(request, winner, sub, cancellationToken);
    }

    // Cada canje abre una familia de sesión nueva.
    private RefreshTokenRecord NewRefresh(string userId, IssuedSession session, string deviceId) =>
        new(
            userId,
            ObjectId.GenerateNewId().ToString(),
            RefreshTokenFingerprint.Compute(session.RefreshToken),
            deviceId,
            timeProvider.GetUtcNow().UtcDateTime);

    private DomainException Conflict()
    {
        LogConflict(logger);
        return new DomainException(DomainErrorKind.Conflict, "The phone or the identity is already linked to another account.", AccountConflictCode);
    }

    // Los registros no llevan ID token, teléfono, uid, sub ni tokens de la sesión.
    [LoggerMessage(EventId = 2001, Level = LogLevel.Warning, Message = "Firebase session exchange rejected")]
    private static partial void LogRejected(ILogger logger);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Information, Message = "Firebase session exchange succeeded")]
    private static partial void LogIssued(ILogger logger);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Information, Message = "Account created by Firebase session exchange")]
    private static partial void LogCreated(ILogger logger);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Warning, Message = "Firebase session exchange refused: account suspended")]
    private static partial void LogSuspended(ILogger logger);

    [LoggerMessage(EventId = 2005, Level = LogLevel.Warning, Message = "Firebase session exchange refused: phone or identity linked to another account")]
    private static partial void LogConflict(ILogger logger);
}
