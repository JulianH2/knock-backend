using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Knock.Market.Api.Modules.Media.Data;
using Knock.Market.Api.Modules.Media.Domain;
using Microsoft.Extensions.Options;

namespace Knock.Market.Api.Modules.Media.Services;

/// <summary>
/// Azure Blob access for quarantine and final containers. Client bytes are only ever read from quarantine;
/// the final container holds re-encoded images. SAS generation is OI-3.
/// </summary>
internal sealed class AzureMediaBlobStore(BlobServiceClient blobs, IOptions<MediaOptions> options) : IMediaBlobStore
{
    private readonly BlobContainerClient _quarantine = blobs.GetBlobContainerClient(options.Value.QuarantineContainer);
    private readonly BlobContainerClient _files = blobs.GetBlobContainerClient(options.Value.FilesContainer);
    private volatile bool _containersReady;

    public async Task EnsureContainersAsync(CancellationToken cancellationToken)
    {
        if (_containersReady)
        {
            return;
        }

        await _quarantine.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
        await _files.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
        _containersReady = true;
    }

    public async Task<UploadedBlob?> GetUploadedAsync(string quarantineBlob, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(quarantineBlob);
        await EnsureContainersAsync(cancellationToken);
        try
        {
            var properties = await _quarantine.GetBlobClient(quarantineBlob).GetPropertiesAsync(cancellationToken: cancellationToken);
            return new UploadedBlob(properties.Value.ContentLength, properties.Value.ETag.ToString());
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<byte[]?> DownloadQuarantineAsync(string quarantineBlob, UploadedBlob expected, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(quarantineBlob);
        ArgumentNullException.ThrowIfNull(expected);
        await EnsureContainersAsync(cancellationToken);
        var limit = MediaRules.MaxFileBytes + 1;
        var download = new BlobDownloadOptions
        {
            Range = new HttpRange(0, limit),
            Conditions = new BlobRequestConditions { IfMatch = new ETag(expected.ETag) },
        };
        try
        {
            var result = await _quarantine.GetBlobClient(quarantineBlob).DownloadStreamingAsync(download, cancellationToken);
            using var streaming = result.Value;
            return await ReadAtMostAsync(streaming.Content, (int)Math.Min(expected.SizeBytes + 1, limit), cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status is 404 or 412 or 416)
        {
            return null;
        }
    }

    public async Task UploadFileAsync(string fileBlob, byte[] content, ImageFormat format, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileBlob);
        ArgumentNullException.ThrowIfNull(content);
        await EnsureContainersAsync(cancellationToken);
        await _files.GetBlobClient(fileBlob).UploadAsync(
            BinaryData.FromBytes(content),
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = "image/jpeg" } },
            cancellationToken);
    }

    public async Task DeleteQuarantineAsync(string quarantineBlob, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(quarantineBlob);
        await _quarantine.GetBlobClient(quarantineBlob).DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    /// <summary>Reads up to <paramref name="capacity"/> bytes; more content than that fills the buffer and the caller sees it.</summary>
    private static async Task<byte[]> ReadAtMostAsync(Stream content, int capacity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        var buffer = new byte[capacity];
        var total = 0;
        int read;
        while (total < capacity && (read = await content.ReadAsync(buffer.AsMemory(total), cancellationToken)) > 0)
        {
            total += read;
        }

        return buffer[..total];
    }
}
