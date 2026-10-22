namespace Knock.Api.Modules.Media.Domain;

/// <summary>Límites y reglas puras del módulo (sin E/S): tipos permitidos, tamaño, dimensiones y firma del contenido.</summary>
internal static class MediaRules
{
    /// <summary>Tamaño máximo de la foto, declarado y real: 8 MB.</summary>
    public const long MaxFileBytes = 8L * 1024 * 1024;

    /// <summary>Límite de píxeles antes de decodificar (evita «bombas de descompresión»).</summary>
    public const long MaxPixels = 40_000_000;

    /// <summary>Lado máximo, en píxeles, antes de decodificar.</summary>
    public const int MaxSidePixels = 12_000;

    /// <summary>Vida de cualquier SAS que emite el módulo (escritura o lectura).</summary>
    public static readonly TimeSpan SasLifetime = TimeSpan.FromMinutes(5);

    private static readonly byte[] _jpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] _pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static bool IsAllowedContentType(string contentType) =>
        contentType is "image/jpeg" or "image/png";

    public static bool IsAllowedSize(long sizeBytes) => sizeBytes is > 0 and <= MaxFileBytes;

    public static bool AreAllowedDimensions(int width, int height) =>
        width is > 0 and <= MaxSidePixels && height is > 0 and <= MaxSidePixels && (long)width * height <= MaxPixels;

    /// <summary>Formato por firma (magic bytes); <see langword="null"/> si no es JPG ni PNG.</summary>
    public static ImageFormat? DetectFormat(ReadOnlySpan<byte> content)
    {
        if (content.StartsWith(_jpegSignature))
        {
            return ImageFormat.Jpeg;
        }

        return content.StartsWith(_pngSignature) ? ImageFormat.Png : null;
    }

    public static MediaUsage? ParseUsage(string value) => value switch
    {
        "perfil" => MediaUsage.Profile,
        "solicitud" => MediaUsage.Request,
        "trabajo" => MediaUsage.Job,
        _ => null,
    };

    public static string ToContractValue(this MediaUsage usage) => usage switch
    {
        MediaUsage.Profile => "perfil",
        MediaUsage.Request => "solicitud",
        _ => "trabajo",
    };

    public static string ToContractValue(this MediaFileState state) => state switch
    {
        MediaFileState.Pending => "pendiente",
        MediaFileState.Ready => "listo",
        _ => "rechazado",
    };
}
