using Knock.Market.Api.Modules.Media.Domain;

namespace Knock.Market.Api.Modules.Media.Data;

/// <summary>Stored record with its version (for optimistic concurrency).</summary>
internal sealed record StoredMediaFile(MediaFile File, string Version);

/// <summary>
/// Persistence of each file record. Internal to the module; other modules use <c>IMediaApi</c>.
/// </summary>
internal interface IMediaFileStore
{
    /// <exception cref="InvalidOperationException">A record with the same id already exists.</exception>
    Task AddAsync(MediaFile file, CancellationToken cancellationToken);

    Task<StoredMediaFile?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Replaces the record only if still at <paramref name="expectedVersion"/>; false if another request changed it first.</summary>
    Task<bool> TryReplaceAsync(MediaFile file, string expectedVersion, CancellationToken cancellationToken);
}
