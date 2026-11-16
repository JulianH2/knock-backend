using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Knock.Market.Api.Modules.Media.Domain;
using Microsoft.Extensions.Options;

namespace Knock.Market.Api.Modules.Media.Data;

/// <summary>
/// Temporary <see cref="IMediaFileStore"/> until TT-BD-06: one JSON document per file in the private records
/// container, with the ETag as the version. Stateless (survives restarts and multiple replicas); same field names as
/// the <c>archivos</c> collection. Removed when the DocumentDB implementation exists.
/// </summary>
internal sealed class BlobMediaFileStore(BlobServiceClient blobs, IOptions<MediaOptions> options) : IMediaFileStore
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly BlobContainerClient _records = blobs.GetBlobContainerClient(options.Value.RecordsContainer);
    private volatile bool _containerReady;

    public async Task AddAsync(MediaFile file, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        await EnsureContainerAsync(cancellationToken);
        var conditions = new BlobRequestConditions { IfNoneMatch = ETag.All };
        try
        {
            await _records.GetBlobClient(BlobName(file.Id)).UploadAsync(Serialize(file), Upload(conditions), cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status == 409 || ex.Status == 412)
        {
            throw new InvalidOperationException("A media file with the same id already exists.", ex);
        }
    }

    public async Task<StoredMediaFile?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        await EnsureContainerAsync(cancellationToken);
        try
        {
            var result = await _records.GetBlobClient(BlobName(id)).DownloadContentAsync(cancellationToken);
            var record = result.Value.Content.ToObjectFromJson<MediaFileRecord>(_json)
                ?? throw new InvalidOperationException("The media file record is empty.");
            return new StoredMediaFile(record.ToDomain(), result.Value.Details.ETag.ToString());
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<bool> TryReplaceAsync(MediaFile file, string expectedVersion, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        var conditions = new BlobRequestConditions { IfMatch = new ETag(expectedVersion) };
        try
        {
            await _records.GetBlobClient(BlobName(file.Id)).UploadAsync(Serialize(file), Upload(conditions), cancellationToken);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 412)
        {
            return false;
        }
    }

    private static string BlobName(Guid id) => $"{id:N}.json";

    private static BinaryData Serialize(MediaFile file) => BinaryData.FromObjectAsJson(MediaFileRecord.FromDomain(file), _json);

    private static BlobUploadOptions Upload(BlobRequestConditions conditions) => new()
    {
        Conditions = conditions,
        HttpHeaders = new BlobHttpHeaders { ContentType = "application/json" },
    };

    private async Task EnsureContainerAsync(CancellationToken cancellationToken)
    {
        if (!_containerReady)
        {
            await _records.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
            _containerReady = true;
        }
    }

    /// <summary>Persisted shape: same fields (snake_case, Spanish) as the <c>archivos</c> collection.</summary>
    private sealed record MediaFileRecord(
        Guid Id,
        string BlobKey,
        string PropietarioId,
        string Uso,
        string Estado,
        string ContentType,
        long TamanoBytes,
        string? BlobOriginal,
        string? BlobReducido,
        int? Ancho,
        int? Alto,
        DateTimeOffset CreadoEn,
        DateTimeOffset? ConfirmadoEn)
    {
        public static MediaFileRecord FromDomain(MediaFile file) => new(
            file.Id, file.BlobKey, file.OwnerId, file.Usage.ToContractValue(), file.State.ToContractValue(), file.DeclaredContentType,
            file.DeclaredSizeBytes, file.OriginalBlob, file.ReducedBlob, file.Width, file.Height, file.CreatedAt, file.ConfirmedAt);

        public MediaFile ToDomain() =>
            new(Id, BlobKey, PropietarioId, ParseUsage(Uso), ParseState(Estado), ContentType, TamanoBytes, CreadoEn)
            {
                OriginalBlob = BlobOriginal,
                ReducedBlob = BlobReducido,
                Width = Ancho,
                Height = Alto,
                ConfirmedAt = ConfirmadoEn,
            };

        private static MediaUsage ParseUsage(string value) =>
            MediaRules.ParseUsage(value) ?? throw new InvalidOperationException("Unknown media usage in the stored record.");

        private static MediaFileState ParseState(string value) => value switch
        {
            "pendiente" => MediaFileState.Pending,
            "listo" => MediaFileState.Ready,
            "rechazado" => MediaFileState.Rejected,
            _ => throw new InvalidOperationException("Unknown media state in the stored record."),
        };
    }
}
