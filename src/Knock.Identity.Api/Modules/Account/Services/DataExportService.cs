using Knock.Identity.Api.Common.Errors;
using Knock.Identity.Api.Modules.Account.Data;
using Knock.Identity.Api.Modules.Account.Domain;

namespace Knock.Identity.Api.Modules.Account.Services;

/// <summary>
/// Exportación de los datos propios (spec 039, USR-06; R-22). Aquí solo se registra la petición: una en curso por persona, y se programa la tarea
/// <c>PrepararExportacion</c> (spec 054), que arma el ZIP en el contenedor privado y avisa con un enlace de 24 horas. Nada de la exportación llega a los
/// registros (R-26).
/// </summary>
internal sealed partial class DataExportService(
    IAccountStore store,
    IAccountOutbox outbox,
    TimeProvider timeProvider,
    ILogger<DataExportService> logger)
{
    /// <exception cref="DomainException">409 <c>export_in_progress</c> si ya hay una en curso; 404 si la cuenta del token ya no existe.</exception>
    public async Task RequestAsync(string sub, CancellationToken cancellationToken)
    {
        var account = await store.FindBySubAsync(sub, cancellationToken)
            ?? throw new DomainException(DomainErrorKind.NotFound, "The account of the token does not exist.");
        if (await store.TryStartExportAsync(account.Id, timeProvider.GetUtcNow().UtcDateTime, cancellationToken) == ExportStartOutcome.AlreadyInProgress)
        {
            LogInProgress(logger);
            throw new DomainException(DomainErrorKind.Conflict, "An export is already in progress.", AccountErrorCodes.ExportInProgress);
        }

        try
        {
            await outbox.EnqueueExportAsync(account.Id, cancellationToken);
        }
        catch
        {
            // Sin tarea no habría quien termine la exportación y la marca bloquearía a la persona: se quita y el error sigue su camino.
            await store.ClearExportAsync(account.Id, CancellationToken.None);
            throw;
        }

        LogRequested(logger);
    }

    [LoggerMessage(EventId = 3908, Level = LogLevel.Information, Message = "Data export requested")]
    private static partial void LogRequested(ILogger logger);

    [LoggerMessage(EventId = 3909, Level = LogLevel.Information, Message = "Data export refused: one is already in progress")]
    private static partial void LogInProgress(ILogger logger);
}
