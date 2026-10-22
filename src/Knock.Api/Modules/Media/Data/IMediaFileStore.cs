using Knock.Api.Modules.Media.Domain;

namespace Knock.Api.Modules.Media.Data;

/// <summary>Registro guardado junto con su versión (para la concurrencia optimista).</summary>
internal sealed record StoredMediaFile(MediaFile File, string Version);

/// <summary>
/// Persistencia del registro de cada archivo. Interna al módulo: ningún otro módulo ni servicio lee estos datos
/// (ADR-001 regla 1); los demás usan <see cref="IMediaApi"/>.
/// <para>
/// Destino (TT-BD-06, Azure DocumentDB, ADR-002): colección <c>archivos</c> en la base lógica <c>market</c>, documento
/// <c>{ _id, propietario_id, uso, estado, content_type, tamano_bytes, blob_original, blob_reducido, ancho, alto,
/// creado_en, confirmado_en, version }</c> con <c>estado ∈ {pendiente, listo, rechazado}</c>, índices
/// <c>{ propietario_id: 1 }</c> y <c>{ estado: 1 }</c>, <c>_id</c> UUID v7, fechas en UTC y <c>maxPoolSize</c> fijo en la
/// conexión. Hasta entonces la implementación es <see cref="BlobMediaFileStore"/> (temporal) y no se crea esquema.
/// </para>
/// </summary>
internal interface IMediaFileStore
{
    /// <exception cref="InvalidOperationException">Ya existe un registro con ese id.</exception>
    Task AddAsync(MediaFile file, CancellationToken cancellationToken);

    Task<StoredMediaFile?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Reemplaza el registro solo si sigue en <paramref name="expectedVersion"/>; si otro lo cambió antes, devuelve <see langword="false"/>.</summary>
    Task<bool> TryReplaceAsync(MediaFile file, string expectedVersion, CancellationToken cancellationToken);
}
