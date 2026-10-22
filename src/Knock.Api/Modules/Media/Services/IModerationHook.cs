using Knock.Api.Modules.Media.Domain;

namespace Knock.Api.Modules.Media.Services;

/// <summary>
/// Punto de enganche de moderación (lineamiento 16b) en <c>POST /archivos/{id}/confirmar</c>: se llama después de
/// validar y limpiar el contenido y antes de moverlo al contenedor definitivo. TT-IA-02 decide proveedor, costo y la
/// respuesta de la API ante un rechazo; hasta entonces la implementación no hace nada.
/// </summary>
internal interface IModerationHook
{
    Task ReviewAsync(MediaFile file, ProcessedImage image, CancellationToken cancellationToken);
}

/// <summary>Implementación vacía hasta TT-IA-02: aprueba cualquier imagen sin enviarla a ningún servicio.</summary>
internal sealed class NoModerationHook : IModerationHook
{
    public Task ReviewAsync(MediaFile file, ProcessedImage image, CancellationToken cancellationToken) => Task.CompletedTask;
}
