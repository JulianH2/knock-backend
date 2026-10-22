namespace Knock.Api.Modules.Media.Domain;

/// <summary>Uso previsto de la foto; decide qué módulo la reutiliza (perfil, solicitud o trabajo).</summary>
internal enum MediaUsage
{
    Profile,
    Request,
    Job,
}

/// <summary>Estados del archivo: <c>pendiente</c> → <c>listo</c> o <c>rechazado</c>; los dos últimos son finales.</summary>
internal enum MediaFileState
{
    Pending,
    Ready,
    Rejected,
}

/// <summary>Formato real de la imagen, detectado por contenido (nunca por extensión ni <c>Content-Type</c>).</summary>
internal enum ImageFormat
{
    Jpeg,
    Png,
}

/// <summary>
/// Registro de un archivo privado. El dueño es el <c>sub</c> del JWT; los nombres de blob los genera el servidor
/// a partir del id (UUID v7), nunca con el nombre que envía el cliente.
/// </summary>
internal sealed record MediaFile(
    Guid Id,
    string OwnerId,
    MediaUsage Usage,
    MediaFileState State,
    string DeclaredContentType,
    long DeclaredSizeBytes,
    DateTimeOffset CreatedAt)
{
    public string? OriginalBlob { get; init; }

    public string? ReducedBlob { get; init; }

    public int? Width { get; init; }

    public int? Height { get; init; }

    public DateTimeOffset? ConfirmedAt { get; init; }

    /// <summary>Blob de cuarentena donde el cliente sube los bytes con el SAS de escritura.</summary>
    public string QuarantineBlob => Id.ToString("N");

    public bool IsReadyFor(string ownerId) => State == MediaFileState.Ready && string.Equals(OwnerId, ownerId, StringComparison.Ordinal);

    public static MediaFile CreatePending(string ownerId, MediaUsage usage, string contentType, long sizeBytes, DateTimeOffset now) =>
        new(Guid.CreateVersion7(now), ownerId, usage, MediaFileState.Pending, contentType, sizeBytes, now);

    /// <exception cref="InvalidOperationException">El archivo ya no está pendiente.</exception>
    public MediaFile MarkReady(ImageFormat format, int width, int height, DateTimeOffset now)
    {
        EnsurePending();
        var extension = format == ImageFormat.Png ? "png" : "jpg";
        return this with
        {
            State = MediaFileState.Ready,
            OriginalBlob = $"{Id:N}/original.{extension}",
            ReducedBlob = $"{Id:N}/reducida.{extension}",
            Width = width,
            Height = height,
            ConfirmedAt = now,
        };
    }

    /// <exception cref="InvalidOperationException">El archivo ya no está pendiente.</exception>
    public MediaFile MarkRejected(DateTimeOffset now)
    {
        EnsurePending();
        return this with { State = MediaFileState.Rejected, ConfirmedAt = now };
    }

    private void EnsurePending()
    {
        if (State != MediaFileState.Pending)
        {
            throw new InvalidOperationException($"Media file is {State}; only pending files change state.");
        }
    }
}
