namespace Knock.Api.Modules.Identity.Services;

/// <summary>Canjea un ID token de Firebase por la sesión propia. Sin persistencia: el <c>sub</c> se deriva del <c>uid</c>.</summary>
internal sealed partial class FirebaseSessionService(
    IFirebaseIdTokenVerifier verifier,
    SessionTokenIssuer issuer,
    ILogger<FirebaseSessionService> logger)
{
    /// <returns>La sesión, o <c>null</c> si el token se rechaza (inválido, vencido, sin teléfono o de otro proyecto).</returns>
    public async Task<IssuedSession?> ExchangeAsync(string firebaseIdToken, CancellationToken cancellationToken)
    {
        var identity = await verifier.VerifyAsync(firebaseIdToken, cancellationToken);
        if (identity is null)
        {
            LogRejected(logger);
            return null;
        }

        var session = issuer.Issue(UserIdDerivation.Derive(identity.Uid));
        LogIssued(logger);
        return session;
    }

    // Los registros no llevan ID token, teléfono, uid ni tokens de la sesión.
    [LoggerMessage(EventId = 2001, Level = LogLevel.Warning, Message = "Firebase session exchange rejected")]
    private static partial void LogRejected(ILogger logger);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Information, Message = "Firebase session exchange succeeded")]
    private static partial void LogIssued(ILogger logger);
}
