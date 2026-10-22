using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Knock.Api.Modules.Media.Data;
using Knock.Api.Modules.Media.Domain;
using Microsoft.Extensions.Options;

namespace Knock.Api.Modules.Media.Services;

/// <summary>Enlace SAS emitido: URL de un solo blob y su vencimiento. La URL nunca se registra en logs.</summary>
internal sealed record SasLink(Uri Url, DateTimeOffset ExpiresAt);

/// <summary>
/// Acceso a los contenedores privados de fotos. Solo emite SAS de servicio sobre un blob (nunca de cuenta ni de
/// contenedor), con permisos mínimos y vencimiento de 5 min calculado con <see cref="TimeProvider"/>.
/// </summary>
internal sealed class MediaBlobStorage(BlobServiceClient blobs, IOptions<MediaOptions> options, TimeProvider timeProvider)
{
    private readonly BlobContainerClient _quarantine = blobs.GetBlobContainerClient(options.Value.QuarantineContainer);
    private readonly BlobContainerClient _files = blobs.GetBlobContainerClient(options.Value.FilesContainer);
    private volatile bool _containersReady;

    /// <summary>Crea los contenedores si faltan, siempre privados (sin acceso anónimo).</summary>
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

    public SasLink CreateUploadLink(string quarantineBlob) =>
        CreateLink(_quarantine.GetBlobClient(quarantineBlob), BlobSasPermissions.Create | BlobSasPermissions.Write);

    public SasLink CreateReadLink(string fileBlob) =>
        CreateLink(_files.GetBlobClient(fileBlob), BlobSasPermissions.Read);

    /// <summary>Tamaño real del blob subido a cuarentena, o <see langword="null"/> si el cliente no subió nada.</summary>
    public async Task<long?> GetQuarantineSizeAsync(string quarantineBlob, CancellationToken cancellationToken)
    {
        try
        {
            var properties = await _quarantine.GetBlobClient(quarantineBlob).GetPropertiesAsync(cancellationToken: cancellationToken);
            return properties.Value.ContentLength;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<BinaryData> DownloadQuarantineAsync(string quarantineBlob, CancellationToken cancellationToken)
    {
        var result = await _quarantine.GetBlobClient(quarantineBlob).DownloadContentAsync(cancellationToken);
        return result.Value.Content;
    }

    public Task DeleteQuarantineAsync(string quarantineBlob, CancellationToken cancellationToken) =>
        _quarantine.GetBlobClient(quarantineBlob).DeleteIfExistsAsync(cancellationToken: cancellationToken);

    public Task UploadFileAsync(string fileBlob, byte[] content, ImageFormat format, CancellationToken cancellationToken) =>
        _files.GetBlobClient(fileBlob).UploadAsync(
            BinaryData.FromBytes(content),
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = format == ImageFormat.Png ? "image/png" : "image/jpeg" } },
            cancellationToken);

    private SasLink CreateLink(BlobClient blob, BlobSasPermissions permissions)
    {
        if (!blob.CanGenerateSasUri)
        {
            // Con identidad administrada (TT-AZ-02) se usa un SAS de delegación de usuario en lugar de la clave compartida.
            throw new InvalidOperationException("The Blob client cannot sign SAS links with the configured credential.");
        }

        var expiresAt = timeProvider.GetUtcNow() + MediaRules.SasLifetime;
        var sas = new BlobSasBuilder(permissions, expiresAt)
        {
            BlobContainerName = blob.BlobContainerName,
            BlobName = blob.Name,
            Resource = "b",
            Protocol = blob.Uri.Scheme == Uri.UriSchemeHttps ? SasProtocol.Https : SasProtocol.HttpsAndHttp,
        };
        return new SasLink(blob.GenerateSasUri(sas), expiresAt);
    }
}
