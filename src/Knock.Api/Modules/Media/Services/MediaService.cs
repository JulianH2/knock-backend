using Knock.Api.Modules.Media.Data;
using Knock.Api.Modules.Media.Domain;

namespace Knock.Api.Modules.Media.Services;

internal sealed record UploadTicket(Guid Id, SasLink Link);

internal sealed record ConfirmedFile(Guid Id, string State, int Width, int Height);

/// <summary>
/// Subida, confirmación y lectura de fotos privadas. La foto nunca pasa por la API: el cliente la sube a cuarentena con
/// un SAS de escritura, y al confirmar se valida por contenido, se limpia y se mueve al contenedor definitivo.
/// El dueño es siempre el <c>sub</c> del JWT; a cualquier otro se le responde como si el archivo no existiera.
/// </summary>
internal sealed class MediaService(
    IMediaFileStore store,
    MediaBlobStorage storage,
    ImageProcessor processor,
    IModerationHook moderation,
    TimeProvider timeProvider)
{
    private const string _invalidFileDetail = "The file must be a JPG or PNG image of 8 MB or less.";

    public async Task<MediaOutcome<UploadTicket>> CreateUploadAsync(
        string ownerId, string usage, string contentType, long sizeBytes, CancellationToken cancellationToken)
    {
        var parsedUsage = MediaRules.ParseUsage(usage);
        if (parsedUsage is null || !MediaRules.IsAllowedContentType(contentType) || !MediaRules.IsAllowedSize(sizeBytes))
        {
            return MediaOutcome<UploadTicket>.Unprocessable(_invalidFileDetail);
        }

        await storage.EnsureContainersAsync(cancellationToken);
        var file = MediaFile.CreatePending(ownerId, parsedUsage.Value, contentType, sizeBytes, timeProvider.GetUtcNow());
        await store.AddAsync(file, cancellationToken);
        return MediaOutcome<UploadTicket>.Ok(new UploadTicket(file.Id, storage.CreateUploadLink(file.QuarantineBlob)));
    }

    public async Task<MediaOutcome<ConfirmedFile>> ConfirmAsync(string ownerId, Guid id, CancellationToken cancellationToken)
    {
        var stored = await store.FindAsync(id, cancellationToken);
        if (stored is null || !string.Equals(stored.File.OwnerId, ownerId, StringComparison.Ordinal))
        {
            return MediaOutcome<ConfirmedFile>.NotFound();
        }

        if (stored.File.State != MediaFileState.Pending)
        {
            return MediaOutcome<ConfirmedFile>.Conflict("The file was already confirmed.");
        }

        await storage.EnsureContainersAsync(cancellationToken);
        var size = await storage.GetQuarantineSizeAsync(stored.File.QuarantineBlob, cancellationToken);
        if (size is null)
        {
            return MediaOutcome<ConfirmedFile>.Unprocessable("No file was uploaded with the upload link.");
        }

        var image = await ReadImageAsync(stored.File, size.Value, cancellationToken);
        if (image is null)
        {
            return await RejectAsync(stored, cancellationToken);
        }

        await moderation.ReviewAsync(stored.File, image, cancellationToken);
        return await AcceptAsync(stored, image, cancellationToken);
    }

    /// <summary>SAS de lectura solo para el dueño y solo de archivos listos; si no, 404 sin distinguir el motivo.</summary>
    public async Task<MediaOutcome<SasLink>> CreateOwnerReadLinkAsync(string ownerId, Guid id, bool reduced, CancellationToken cancellationToken)
    {
        var stored = await store.FindAsync(id, cancellationToken);
        return stored is not null && stored.File.IsReadyFor(ownerId)
            ? MediaOutcome<SasLink>.Ok(ReadLink(stored.File, reduced))
            : MediaOutcome<SasLink>.NotFound();
    }

    /// <summary>SAS de lectura para otro módulo que ya autorizó a quien lo pide (contrato <see cref="IMediaApi"/>).</summary>
    public async Task<SasLink?> CreateAuthorizedReadLinkAsync(Guid id, bool reduced, CancellationToken cancellationToken)
    {
        var stored = await store.FindAsync(id, cancellationToken);
        return stored?.File.State == MediaFileState.Ready ? ReadLink(stored.File, reduced) : null;
    }

    public async Task<bool> IsReadyAndOwnedByAsync(Guid id, string ownerId, CancellationToken cancellationToken)
    {
        var stored = await store.FindAsync(id, cancellationToken);
        return stored is not null && stored.File.IsReadyFor(ownerId);
    }

    private SasLink ReadLink(MediaFile file, bool reduced) =>
        storage.CreateReadLink((reduced ? file.ReducedBlob : file.OriginalBlob)
            ?? throw new InvalidOperationException("A ready media file has no blob names."));

    /// <summary>El tamaño real se revisa antes de descargar; los bytes del cliente nunca se guardan tal cual.</summary>
    private async Task<ProcessedImage?> ReadImageAsync(MediaFile file, long sizeBytes, CancellationToken cancellationToken)
    {
        if (sizeBytes > MediaRules.MaxFileBytes)
        {
            return null;
        }

        var content = await storage.DownloadQuarantineAsync(file.QuarantineBlob, cancellationToken);
        return processor.Process(content.ToMemory().Span);
    }

    private async Task<MediaOutcome<ConfirmedFile>> RejectAsync(StoredMediaFile stored, CancellationToken cancellationToken)
    {
        await storage.DeleteQuarantineAsync(stored.File.QuarantineBlob, cancellationToken);
        var rejected = stored.File.MarkRejected(timeProvider.GetUtcNow());
        return await store.TryReplaceAsync(rejected, stored.Version, cancellationToken)
            ? MediaOutcome<ConfirmedFile>.Unprocessable(_invalidFileDetail)
            : MediaOutcome<ConfirmedFile>.Conflict("The file was confirmed by another request.");
    }

    private async Task<MediaOutcome<ConfirmedFile>> AcceptAsync(StoredMediaFile stored, ProcessedImage image, CancellationToken cancellationToken)
    {
        var ready = stored.File.MarkReady(image.Format, image.Width, image.Height, timeProvider.GetUtcNow());
        await storage.UploadFileAsync(ready.OriginalBlob!, image.Original, image.Format, cancellationToken);
        await storage.UploadFileAsync(ready.ReducedBlob!, image.Reduced, image.Format, cancellationToken);
        await storage.DeleteQuarantineAsync(ready.QuarantineBlob, cancellationToken);
        return await store.TryReplaceAsync(ready, stored.Version, cancellationToken)
            ? MediaOutcome<ConfirmedFile>.Ok(new ConfirmedFile(ready.Id, ready.State.ToContractValue(), image.Width, image.Height))
            : MediaOutcome<ConfirmedFile>.Conflict("The file was confirmed by another request.");
    }
}
