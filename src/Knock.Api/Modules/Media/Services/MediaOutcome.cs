using System.Globalization;
using Knock.Api.Modules.Media.Domain;

namespace Knock.Api.Modules.Media.Services;

/// <summary>Resultado de una operación del módulo; el controller lo traduce a HTTP sin lógica propia.</summary>
internal enum MediaOutcomeStatus
{
    Success,

    /// <summary>No existe o no es de quien llama: no se distingue para no filtrar existencia (404).</summary>
    NotFound,

    /// <summary>El archivo ya no está pendiente (409).</summary>
    Conflict,

    /// <summary>El archivo o lo declarado no es válido (422).</summary>
    Unprocessable,
}

internal sealed record MediaOutcome<T>(MediaOutcomeStatus Status, T? Value = default, string? Detail = null)
{
    public static MediaOutcome<T> Ok(T value) => new(MediaOutcomeStatus.Success, value);

    public static MediaOutcome<T> NotFound() => new(MediaOutcomeStatus.NotFound);

    public static MediaOutcome<T> Conflict(string detail) => new(MediaOutcomeStatus.Conflict, Detail: detail);

    public static MediaOutcome<T> Unprocessable(string detail) => new(MediaOutcomeStatus.Unprocessable, Detail: detail);
}

/// <summary>Texto del <c>detail</c> de cada 422 del módulo: cada motivo de rechazo tiene el suyo y ninguno repite datos del archivo.</summary>
internal static class MediaRejection
{
    public const string InvalidFile = "The file must be a JPG or PNG image of 8 MB or less.";

    public const string NotUploaded = "No file was uploaded with the upload link.";

    public const string ChangedDuringConfirmation = "The uploaded file changed while it was being confirmed. Upload it again.";

    public static readonly string DimensionsTooLarge = string.Create(
        CultureInfo.InvariantCulture,
        $"The image must be at most {MediaRules.MaxSidePixels} pixels per side and {MediaRules.MaxPixels / 1_000_000} megapixels in total.");
}
