using Knock.Identity.Api.Modules.Account.Data;
using Knock.Identity.Api.Modules.Account.Domain;
using Knock.Identity.Api.Modules.Sesiones.Services;

namespace Knock.Identity.Api.Modules.Account.Services;

/// <summary>Datos de una solicitud de recuperación que no vienen del token: lo que manda la app.</summary>
internal sealed record AccessRecoveryInput(string FirebaseIdToken, string PreviousPhone, string Type)
{
    /// <summary>Sin datos: un registro imprimiría el token y el celular anterior.</summary>
    public sealed override string ToString() => nameof(AccessRecoveryInput);
}

/// <summary>Resultado de pedir la recuperación: la respuesta es la misma exista o no la cuenta (R-21).</summary>
internal abstract record AccessRecoveryOutcome
{
    private AccessRecoveryOutcome()
    {
    }

    /// <summary>202: se aceptó la solicitud (si la cuenta existe, quedó un caso en la cola del panel).</summary>
    public sealed record Accepted : AccessRecoveryOutcome;

    /// <summary>El token de Firebase no se pudo verificar (422 en <c>firebaseIdToken</c>).</summary>
    public sealed record InvalidToken : AccessRecoveryOutcome;

    /// <summary>Más de 3 intentos por hora con ese número (429 con <c>Retry-After</c>).</summary>
    public sealed record RateLimited(TimeSpan RetryAfter) : AccessRecoveryOutcome;
}

/// <summary>
/// Recuperación de acceso (spec 039, USR-05; R-21). La persona prueba que controla el número nuevo con el token de Firebase; si existe una cuenta con el
/// celular anterior y el tipo indicado, queda un caso <c>pendiente</c> para la cola del panel (spec 053). Nunca cambia ningún dato de la cuenta y la
/// respuesta no revela si existe. Ni los celulares ni el token llegan a los registros (R-26).
/// </summary>
internal sealed partial class AccessRecoveryService(
    IAccountStore store,
    IFirebaseIdTokenVerifier verifier,
    PhoneProtector phones,
    IRecoveryAttemptLimiter limiter,
    TimeProvider timeProvider,
    ILogger<AccessRecoveryService> logger)
{
    public async Task<AccessRecoveryOutcome> RequestAsync(AccessRecoveryInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        var identity = await verifier.VerifyAsync(input.FirebaseIdToken, cancellationToken);
        if (identity is null)
        {
            LogTokenRejected(logger);
            return new AccessRecoveryOutcome.InvalidToken();
        }

        var newHash = phones.Hash(identity.PhoneNumber);
        var previousHash = phones.Hash(input.PreviousPhone);
        if (!limiter.TryAcquire("nuevo:" + Convert.ToHexStringLower(newHash.Value), out var retryAfter)
            || !limiter.TryAcquire("anterior:" + Convert.ToHexStringLower(previousHash.Value), out retryAfter))
        {
            LogLimited(logger);
            return new AccessRecoveryOutcome.RateLimited(retryAfter);
        }

        var account = await store.FindByPhoneHashAsync(previousHash.Value, cancellationToken);
        if (account is null || !Matches(account, input.Type) || newHash.Value.AsSpan().SequenceEqual(previousHash.Value)
            || await store.PhoneHashExistsAsync(newHash.Value, cancellationToken))
        {
            // Sin cuenta, de otro tipo o con un número nuevo que ya tiene dueño: se responde igual y no se crea nada (R-21).
            LogNoCase(logger);
            return new AccessRecoveryOutcome.Accepted();
        }

        var created = await store.CreateRecoveryCaseIfNoneOpenAsync(
            new RecoveryCase(
                account.Id,
                phones.Protect(identity.PhoneNumber, account.Id),
                newHash,
                input.Type,
                timeProvider.GetUtcNow().UtcDateTime),
            cancellationToken);
        LogCase(logger, created);
        return new AccessRecoveryOutcome.Accepted();
    }

    // Una cuenta sirve para ser cliente y técnico: «cliente» vale para cualquiera; «tecnico» exige la marca de técnico.
    private static bool Matches(AccountSnapshot account, string type) => type == RecoveryTypes.Client || account.IsTechnician;

    // Los registros no llevan celulares, hash, token ni la existencia de la cuenta en el mensaje del 202.
    [LoggerMessage(EventId = 3904, Level = LogLevel.Warning, Message = "Access recovery refused: the Firebase token was rejected")]
    private static partial void LogTokenRejected(ILogger logger);

    [LoggerMessage(EventId = 3905, Level = LogLevel.Warning, Message = "Access recovery refused: too many attempts for the same number")]
    private static partial void LogLimited(ILogger logger);

    [LoggerMessage(EventId = 3906, Level = LogLevel.Debug, Message = "Access recovery accepted without a case")]
    private static partial void LogNoCase(ILogger logger);

    [LoggerMessage(EventId = 3907, Level = LogLevel.Information, Message = "Access recovery accepted; new case: {NewCase}")]
    private static partial void LogCase(ILogger logger, bool newCase);
}
