using System.Globalization;

namespace Knock.Market.Api.Modules.Media.Domain;

/// <summary>Resultado de una operación del módulo.</summary>
internal enum MediaOutcomeStatus { Success, NotFound, Conflict, Unprocessable, Unavailable }
internal sealed record MediaOutcome<T>(MediaOutcomeStatus Status, T? Value = default, string? Detail = null)
{
    public static MediaOutcome<T> Ok(T value) => new(MediaOutcomeStatus.Success, value);
    public static MediaOutcome<T> NotFound() => new(MediaOutcomeStatus.NotFound);
    public static MediaOutcome<T> Conflict(string detail) => new(MediaOutcomeStatus.Conflict, Detail: detail);
    public static MediaOutcome<T> Unprocessable(string detail) => new(MediaOutcomeStatus.Unprocessable, Detail: detail);
    public static MediaOutcome<T> Unavailable() => new(MediaOutcomeStatus.Unavailable);
}
internal static class MediaRejection
{
    public const string InvalidFile = "The file must be a JPEG, PNG or WebP image of 5 MB or less.";
    public const string ModerationRejected = "The content was rejected by moderation.";
    public const string NotUploaded = "No file was uploaded with the upload link.";
    public const string ChangedDuringConfirmation = "The uploaded file changed while it was being confirmed. Upload it again.";
    public static readonly string DimensionsTooLarge = string.Create(System.Globalization.CultureInfo.InvariantCulture,
        $"The image must be at most {MediaRules.MaxSidePixels} pixels per side and {MediaRules.MaxPixels / 1_000_000} megapixels in total.");
}
internal sealed record ProcessedImage(ImageFormat Format, byte[] Original, byte[] Reduced, int Width, int Height);
