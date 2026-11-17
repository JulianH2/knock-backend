namespace Knock.Market.Api.Modules.Media.Domain;

/// <summary>Uso previsto de la foto.</summary>
internal enum MediaUsage { Profile, Request, Job }
/// <summary>Estados del archivo.</summary>
internal enum MediaFileState { Pending, Ready, Rejected }
/// <summary>Formato real detectado por firma.</summary>
internal enum ImageFormat { Jpeg, Png, WebP }

/// <summary>
/// Registro de un archivo privado. Dueño es el <c>sub</c> del JWT. El nombre de blob lo genera el servidor con 128 bits aleatorios
/// (<see cref="BlobKey"/>), independiente del <see cref="Id"/>, para que no se pueda adivinar ni revele la hora (spec 032).
/// </summary>
internal sealed record MediaFile(
    Guid Id, string BlobKey, string OwnerId, MediaUsage Usage, MediaFileState State,
    string DeclaredContentType, long DeclaredSizeBytes, DateTimeOffset CreatedAt)
{
    public string? OriginalBlob { get; init; }
    public string? ReducedBlob { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
    public DateTimeOffset? ConfirmedAt { get; init; }
    public string QuarantineBlob => BlobKey;
    public bool IsReadyFor(string ownerId) => State == MediaFileState.Ready && string.Equals(OwnerId, ownerId, StringComparison.Ordinal);
    public static MediaFile CreatePending(string ownerId, MediaUsage usage, string contentType, long sizeBytes, DateTimeOffset now) =>
        new(Guid.CreateVersion7(now), MediaRules.NewBlobKey(), ownerId, usage, MediaFileState.Pending, contentType, sizeBytes, now);
    /// <summary>Toda foto aceptada se recodifica a JPEG (spec 032), sea cual sea su formato de entrada; por eso los nombres terminan en .jpg.</summary>
    public MediaFile MarkReady(int width, int height, DateTimeOffset now)
    {
        EnsurePending();
        return this with { State = MediaFileState.Ready, OriginalBlob = $"{BlobKey}/original.jpg", ReducedBlob = $"{BlobKey}/reducida.jpg", Width = width, Height = height, ConfirmedAt = now };
    }
    public MediaFile MarkRejected(DateTimeOffset now)
    {
        EnsurePending();
        return this with { State = MediaFileState.Rejected, ConfirmedAt = now };
    }
    private void EnsurePending()
    {
        if (State != MediaFileState.Pending) throw new InvalidOperationException($"Media file is {State}; only pending files change state.");
    }
}
