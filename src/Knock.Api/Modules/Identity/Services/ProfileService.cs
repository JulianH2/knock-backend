using Knock.Api.Common.Errors;
using Knock.Api.Modules.Identity.Data;
using Knock.Api.Modules.Identity.Domain;

namespace Knock.Api.Modules.Identity.Services;

/// <summary>
/// Perfil de la persona con sesión (spec HU-002): lee y completa el nombre de <b>su propia</b> cuenta; la identidad es siempre el <c>sub</c> del JWT.
/// Ni el nombre ni el <c>sub</c> llegan a los registros.
/// </summary>
internal sealed partial class ProfileService(IProfileStore store, ILogger<ProfileService> logger)
{
    /// <returns>El perfil, o <c>null</c> si la cuenta del token ya no existe.</returns>
    public Task<UserProfile?> GetAsync(string sub, CancellationToken cancellationToken) => store.FindProfileAsync(sub, cancellationToken);

    /// <param name="sub">Identificador de la cuenta, tomado del JWT.</param>
    /// <param name="name">Nombre ya validado y normalizado (<see cref="PersonName.TryNormalize"/>).</param>
    /// <param name="cancellationToken">Cancelación de la petición.</param>
    /// <returns>El perfil guardado, o <c>null</c> si la cuenta del token ya no existe.</returns>
    /// <exception cref="DomainException">403 <c>account_suspended</c> si la cuenta no está activa; no se escribe nada.</exception>
    public async Task<UserProfile?> SaveNameAsync(string sub, string name, CancellationToken cancellationToken)
    {
        var profile = await store.SaveNameAsync(sub, name, cancellationToken);
        if (profile is { State: not UserState.Active })
        {
            LogSuspended(logger);
            throw new DomainException(DomainErrorKind.Forbidden, "The account is suspended.", FirebaseSessionService.AccountSuspendedCode);
        }

        if (profile is not null)
        {
            LogSaved(logger);
        }

        return profile;
    }

    // Los registros no llevan el nombre, el sub ni ningún token.
    [LoggerMessage(EventId = 2201, Level = LogLevel.Information, Message = "Profile name saved")]
    private static partial void LogSaved(ILogger logger);

    [LoggerMessage(EventId = 2202, Level = LogLevel.Warning, Message = "Profile update refused: account suspended")]
    private static partial void LogSuspended(ILogger logger);
}
