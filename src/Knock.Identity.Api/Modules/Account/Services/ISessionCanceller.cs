namespace Knock.Identity.Api.Modules.Account.Services;

/// <summary>Cancela sesiones en la lista de Redis que lee el gateway (spec 016); la implementación vive en la capa de datos del módulo.</summary>
internal interface ISessionCanceller
{
    Task CancelAsync(string sessionId, TimeSpan lifetime, CancellationToken cancellationToken);
}
