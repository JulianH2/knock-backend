using System.Security.Cryptography;

namespace Knock.Market.Api.Modules.Media.Domain;

/// <summary>Límites y reglas puras del módulo (sin E/S): tipos permitidos, tamaño y firma del contenido.</summary>
internal static class MediaRules
{
    /// <summary>Tamaño máximo de la foto, declarado y real: 5 242 880 bytes (5 MB; spec 032 y enmienda de la 013).</summary>
    public const long MaxFileBytes = 5_242_880;
    /// <summary>Límite de píxeles antes de decodificar: 40 MP (spec 032).</summary>
    public const long MaxPixels = 40_000_000;
    /// <summary>Lado máximo, en píxeles, antes de decodificar: 8 000 (spec 032).</summary>
    public const int MaxSidePixels = 8_000;
    /// <summary>Bits aleatorios del nombre de blob (spec 032): 128, en 32 caracteres hexadecimales.</summary>
    public const int BlobKeyHexLength = 32;
    /// <summary>Vida de cualquier SAS que emite el módulo.</summary>
    public static readonly TimeSpan SasLifetime = TimeSpan.FromMinutes(5);

    private static readonly byte[] _jpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] _pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] _riffSignature = "RIFF"u8.ToArray();
    private static readonly byte[] _webpTag = "WEBP"u8.ToArray();

    public static bool IsAllowedContentType(string contentType) =>
        contentType is "image/jpeg" or "image/png" or "image/webp";

    public static bool IsAllowedSize(long sizeBytes) => sizeBytes is > 0 and <= MaxFileBytes;

    public static bool AreAllowedDimensions(int width, int height) =>
        width is > 0 and <= MaxSidePixels && height is > 0 and <= MaxSidePixels && (long)width * height <= MaxPixels;

    public static ImageFormat? DetectFormat(ReadOnlySpan<byte> content)
    {
        if (content.StartsWith(_jpegSignature)) return ImageFormat.Jpeg;
        if (content.StartsWith(_pngSignature)) return ImageFormat.Png;
        // WebP: «RIFF», 4 bytes de tamaño y «WEBP».
        return content.Length >= 12 && content.StartsWith(_riffSignature) && content.Slice(8, 4).SequenceEqual(_webpTag) ? ImageFormat.WebP : null;
    }

    /// <summary>Nombre de blob impredecible: 128 bits de <see cref="RandomNumberGenerator"/>, no derivado del id ni de la hora (spec 032).</summary>
    public static string NewBlobKey() => RandomNumberGenerator.GetHexString(BlobKeyHexLength, lowercase: true);

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
