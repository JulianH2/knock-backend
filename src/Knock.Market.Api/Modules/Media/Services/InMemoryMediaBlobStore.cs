using System.Collections.Concurrent;
using Knock.Market.Api.Modules.Media.Domain;

namespace Knock.Market.Api.Modules.Media.Services;

/// <summary>In-memory <see cref="IMediaBlobStore"/> for unit tests. Never registered in production.</summary>
internal sealed class InMemoryMediaBlobStore : IMediaBlobStore
{
    private readonly ConcurrentDictionary<string, (byte[] Content, string ETag)> _blobs = new();
    private int _version;

    public Task EnsureContainersAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void UploadRaw(string blob, byte[] content)
    {
        ArgumentException.ThrowIfNullOrEmpty(blob);
        ArgumentNullException.ThrowIfNull(content);
        _blobs[blob] = ((byte[])content.Clone(), NextETag());
    }

    public bool ContainsQuarantine(string blob) => _blobs.ContainsKey(blob);

    public byte[]? GetBlob(string blob) =>
        _blobs.TryGetValue(blob, out var entry) ? (byte[])entry.Content.Clone() : null;

    public void ClearQuarantine() => _blobs.Clear();

    public Task<UploadedBlob?> GetUploadedAsync(string quarantineBlob, CancellationToken cancellationToken) =>
        Task.FromResult(_blobs.TryGetValue(quarantineBlob, out var entry)
            ? new UploadedBlob(entry.Content.Length, entry.ETag)
            : null);

    public Task<byte[]?> DownloadQuarantineAsync(string quarantineBlob, UploadedBlob expected, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(expected);
        if (!_blobs.TryGetValue(quarantineBlob, out var entry) || entry.ETag != expected.ETag)
        {
            return Task.FromResult<byte[]?>(null);
        }

        return Task.FromResult<byte[]?>((byte[])entry.Content.Clone());
    }

    public Task UploadFileAsync(string fileBlob, byte[] content, ImageFormat format, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileBlob);
        ArgumentNullException.ThrowIfNull(content);
        _ = format;
        _blobs[fileBlob] = ((byte[])content.Clone(), NextETag());
        return Task.CompletedTask;
    }

    public Task DeleteQuarantineAsync(string quarantineBlob, CancellationToken cancellationToken)
    {
        _blobs.TryRemove(quarantineBlob, out _);
        return Task.CompletedTask;
    }

    private string NextETag() => Interlocked.Increment(ref _version).ToString(System.Globalization.CultureInfo.InvariantCulture);
}
