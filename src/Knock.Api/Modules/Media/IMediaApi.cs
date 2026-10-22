namespace Knock.Api.Modules.Media;

/// <summary>
/// Contrato público del módulo Media: la única vía para que otros módulos o servicios usen las fotos privadas
/// (pasa a <c>Knock.Contracts</c> al separar servicios, TT-ARQ-03). Media no conoce las reglas de otros módulos:
/// quien llama decide antes si el solicitante puede ver el archivo.
/// </summary>
public interface IMediaApi
{
    string ModuleName { get; }

    /// <summary>Si el archivo existe, está listo y su dueño es <paramref name="ownerId"/> (el <c>sub</c> del JWT).</summary>
    Task<bool> IsReadyAndOwnedByAsync(Guid fileId, string ownerId, CancellationToken cancellationToken);

    /// <summary>
    /// SAS de lectura de 5 min sobre un solo blob (original o reducido), solo después de que quien llama autorizó al
    /// solicitante con su propia regla. Devuelve <see langword="null"/> si el archivo no existe o no está listo.
    /// </summary>
    Task<(Uri Url, DateTimeOffset ExpiresAt)?> CreateReadLinkAsync(Guid fileId, bool reduced, CancellationToken cancellationToken);
}
