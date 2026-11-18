namespace Knock.Identity.Api.Modules.Account.Services;

/// <summary>
/// Salida de eventos de la cuenta hacia el buzón de salida y las tareas programadas (specs 037 y 054). Esos dos mecanismos todavía no están
/// implementados (spec 039, §8): mientras no existan, <see cref="PendingAccountOutbox"/> solo deja constancia sin datos. Cuando existan, esta
/// interfaz se implementa con la escritura en el buzón, dentro de la transacción del cambio.
/// </summary>
internal interface IAccountOutbox
{
    /// <summary>Deja el aviso <c>sesion_revocada { sid }</c> (spec 037, R-13) para que el despachador borre los dispositivos de esa sesión.</summary>
    Task EnqueueSessionRevokedAsync(string sessionId, CancellationToken cancellationToken);

    /// <summary>Programa la tarea <c>PrepararExportacion</c> (spec 054) de la persona: reúne sus datos, arma el ZIP y la avisa con un enlace de 24 horas.</summary>
    Task EnqueueExportAsync(string userId, CancellationToken cancellationToken);
}

/// <summary>Sustituto mientras no existe el buzón de salida (spec 054): registra que el evento quedó pendiente, sin ids ni datos de la persona.</summary>
internal sealed partial class PendingAccountOutbox(ILogger<PendingAccountOutbox> logger) : IAccountOutbox
{
    public Task EnqueueSessionRevokedAsync(string sessionId, CancellationToken cancellationToken)
    {
        LogPending(logger, "sesion_revocada");
        return Task.CompletedTask;
    }

    public Task EnqueueExportAsync(string userId, CancellationToken cancellationToken)
    {
        LogPending(logger, "PrepararExportacion");
        return Task.CompletedTask;
    }

    [LoggerMessage(EventId = 3990, Level = LogLevel.Warning, Message = "Account event {EventType} not delivered: the outbox (spec 054) is not implemented yet")]
    private static partial void LogPending(ILogger logger, string eventType);
}
