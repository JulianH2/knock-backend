using System.Collections.Concurrent;
using Knock.Market.Api.Modules.Media.Domain;

namespace Knock.Market.Api.Modules.Media.Data;

/// <summary>
/// In-memory <see cref="IMediaFileStore"/> for unit tests. Never registered in production.
/// </summary>
internal sealed class InMemoryMediaFileStore : IMediaFileStore
{
    private readonly ConcurrentDictionary<Guid, StoredMediaFile> _files = new();

    public Task AddAsync(MediaFile file, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (!_files.TryAdd(file.Id, new StoredMediaFile(file, "1")))
        {
            throw new InvalidOperationException("A media file with the same id already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<StoredMediaFile?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_files.TryGetValue(id, out var stored) ? stored : null);

    public Task<bool> TryReplaceAsync(MediaFile file, string expectedVersion, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        var stored = new StoredMediaFile(file, (int.Parse(expectedVersion, System.Globalization.CultureInfo.InvariantCulture) + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
        return Task.FromResult(_files.TryGetValue(file.Id, out var current)
            && current.Version == expectedVersion
            && _files.TryUpdate(file.Id, stored, current));
    }
}
