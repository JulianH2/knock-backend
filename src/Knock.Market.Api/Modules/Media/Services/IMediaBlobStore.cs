using Knock.Market.Api.Modules.Media.Domain;

namespace Knock.Market.Api.Modules.Media.Services;

/// <summary>Real size and version (ETag) of the blob the client uploaded to quarantine, read before downloading.</summary>
internal sealed record UploadedBlob(long SizeBytes, string ETag);

/// <summary>
/// Access to the private photo containers. Only quarantine and final blobs; SAS generation is OI-3.
/// </summary>
internal interface IMediaBlobStore
{
    /// <summary>Creates the containers if missing, always private (no anonymous access).</summary>
    Task EnsureContainersAsync(CancellationToken cancellationToken);

    /// <summary>Real size and ETag of the uploaded quarantine blob, or <see langword="null"/> if the client uploaded nothing.</summary>
    Task<UploadedBlob?> GetUploadedAsync(string quarantineBlob, CancellationToken cancellationToken);

    /// <summary>
    /// Downloads the blob only if still the consulted version (ETag) and never more than 8 MB + 1 byte.
    /// Returns <see langword="null"/> if the blob changed or disappeared after the consultation.
    /// </summary>
    Task<byte[]?> DownloadQuarantineAsync(string quarantineBlob, UploadedBlob expected, CancellationToken cancellationToken);

    /// <summary>Uploads the re-encoded file to the final container. Client bytes are never stored as-is.</summary>
    Task UploadFileAsync(string fileBlob, byte[] content, ImageFormat format, CancellationToken cancellationToken);

    /// <summary>Removes the quarantine blob (confirmed, rejected, or expired upload).</summary>
    Task DeleteQuarantineAsync(string quarantineBlob, CancellationToken cancellationToken);
}
