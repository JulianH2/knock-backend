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
    PhoneProtector phones,
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

    internal static DomainException PhoneTaken() =>
        new(DomainErrorKind.Conflict, "The phone number already belongs to an account.", AccountErrorCodes.PhoneAlreadyRegistered);

    // Los registros no llevan celular, hash ni token.
    [LoggerMessage(EventId = 3901, Level = LogLevel.Information, Message = "Phone change availability check refused: number already registered")]
    private static partial void LogPhoneTaken(ILogger logger);
}
