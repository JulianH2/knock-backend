using Knock.Api.Modules.Media.Data;
using Knock.Api.Modules.Media.Domain;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace Knock.Api.Modules.Media.Services;

/// <summary>Imagen ya limpia: original recodificado y versión reducida, ambos sin EXIF ni otros metadatos.</summary>
internal sealed record ProcessedImage(ImageFormat Format, byte[] Original, byte[] Reduced, int Width, int Height);

/// <summary>
/// Valida por contenido real y limpia la foto con SkiaSharp: firma JPG/PNG, dimensiones acotadas antes de decodificar,
/// decodificación completa, orientación EXIF aplicada a los píxeles y recodificación (que descarta EXIF, GPS, XMP e IPTC).
/// Solo <see cref="MediaOptions.MaxConcurrentProcessing"/> fotos se procesan a la vez por réplica; el pico de memoria
/// de cada una está calculado en <see cref="MediaRules.MaxPixels"/>.
/// </summary>
internal sealed class ImageProcessor(IOptions<MediaOptions> options) : IDisposable
{
    private const int _jpegQuality = 85;

    private readonly SemaphoreSlim _slots = new(options.Value.MaxConcurrentProcessing, options.Value.MaxConcurrentProcessing);

    /// <summary>Procesa cuando hay turno; responde 422 (con un motivo distinto por dimensiones) si no es una imagen JPG/PNG válida dentro de los límites.</summary>
    public Task<MediaOutcome<ProcessedImage>> ProcessAsync(ArraySegment<byte> content, CancellationToken cancellationToken) =>
        RunLimitedAsync(() => Process(content), cancellationToken);

    /// <summary>Ejecuta <paramref name="work"/> sin pasar de los turnos configurados; los demás esperan sin bloquear hilos.</summary>
    internal async Task<T> RunLimitedAsync<T>(Func<T> work, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(work);
        await _slots.WaitAsync(cancellationToken);
        try
        {
            return work();
        }
        finally
        {
            _slots.Release();
        }
    }

    public void Dispose() => _slots.Dispose();

    private MediaOutcome<ProcessedImage> Process(ArraySegment<byte> content)
    {
        var format = MediaRules.DetectFormat(content);
        if (format is null)
        {
            return MediaOutcome<ProcessedImage>.Unprocessable(MediaRejection.InvalidFile);
        }

        using var stream = new MemoryStream(content.Array!, content.Offset, content.Count, writable: false);
        using var codec = SKCodec.Create(stream, out var result);
        if (result != SKCodecResult.Success || ToFormat(codec.EncodedFormat) != format)
        {
            return MediaOutcome<ProcessedImage>.Unprocessable(MediaRejection.InvalidFile);
        }

        if (!MediaRules.AreAllowedDimensions(codec.Info.Width, codec.Info.Height))
        {
            return MediaOutcome<ProcessedImage>.Unprocessable(MediaRejection.DimensionsTooLarge);
        }

        using var oriented = DecodeUpright(codec);
        if (oriented is null)
        {
            return MediaOutcome<ProcessedImage>.Unprocessable(MediaRejection.InvalidFile);
        }

        using var reduced = Reduce(oriented, options.Value.ReducedMaxSide);
        return MediaOutcome<ProcessedImage>.Ok(new ProcessedImage(
            format.Value, Encode(oriented, format.Value), Encode(reduced, format.Value), oriented.Width, oriented.Height));
    }

    private static ImageFormat? ToFormat(SKEncodedImageFormat format) => format switch
    {
        SKEncodedImageFormat.Jpeg => ImageFormat.Jpeg,
        SKEncodedImageFormat.Png => ImageFormat.Png,
        _ => null,
    };

    /// <summary>Decodifica todos los píxeles; una imagen truncada o corrupta (resultado distinto de Success) se rechaza.</summary>
    private static SKBitmap? DecodeFully(SKCodec codec)
    {
        var alpha = codec.Info.AlphaType == SKAlphaType.Opaque ? SKAlphaType.Opaque : SKAlphaType.Premul;
        var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKImageInfo.PlatformColorType, alpha);
        var bitmap = new SKBitmap(info);
        if (codec.GetPixels(info, bitmap.GetPixels()) == SKCodecResult.Success)
        {
            return bitmap;
        }

        bitmap.Dispose();
        return null;
    }

    /// <summary>Decodifica y aplica la orientación; la copia decodificada se libera en cuanto existe la girada (máx. dos mapas a la vez).</summary>
    private static SKBitmap? DecodeUpright(SKCodec codec)
    {
        var decoded = DecodeFully(codec);
        if (decoded is null || codec.EncodedOrigin == SKEncodedOrigin.TopLeft)
        {
            return decoded;
        }

        using (decoded)
        {
            return ApplyOrigin(decoded, codec.EncodedOrigin);
        }
    }

    /// <summary>Copia los píxeles con la orientación EXIF aplicada, para que la imagen recodificada se vea igual sin EXIF.</summary>
    private static SKBitmap ApplyOrigin(SKBitmap source, SKEncodedOrigin origin)
    {
        var swapsSides = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var width = swapsSides ? source.Height : source.Width;
        var height = swapsSides ? source.Width : source.Height;
        var target = new SKBitmap(new SKImageInfo(width, height, source.Info.ColorType, source.Info.AlphaType));
        using var canvas = new SKCanvas(target);
        var matrix = OriginMatrix(origin, source.Width, source.Height);
        canvas.SetMatrix(in matrix);
        canvas.DrawBitmap(source, 0, 0, SKSamplingOptions.Default, null);
        return target;
    }

    /// <summary>Transformación de cada orientación EXIF (1–8) sobre una imagen de <paramref name="w"/> × <paramref name="h"/>.</summary>
    private static SKMatrix OriginMatrix(SKEncodedOrigin origin, float w, float h) => origin switch
    {
        SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, w, 0, 1, 0, 0, 0, 1),
        SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, w, 0, -1, h, 0, 0, 1),
        SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, h, 0, 0, 1),
        SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightTop => new SKMatrix(0, -1, h, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, h, -1, 0, w, 0, 0, 1),
        SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, w, 0, 0, 1),
        _ => SKMatrix.Identity,
    };

    private static SKBitmap Reduce(SKBitmap source, int maxSide)
    {
        var scale = Math.Min(1d, (double)maxSide / Math.Max(source.Width, source.Height));
        var size = new SKSizeI(Math.Max(1, (int)Math.Round(source.Width * scale)), Math.Max(1, (int)Math.Round(source.Height * scale)));
        return source.Resize(size, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear))
            ?? throw new InvalidOperationException("SkiaSharp could not resize the image.");
    }

    /// <summary>Codifica directo desde los píxeles del mapa (sin <c>SKImage.FromBitmap</c>, que copia un mapa mutable).</summary>
    private static byte[] Encode(SKBitmap bitmap, ImageFormat format)
    {
        using var encoded = format == ImageFormat.Png
            ? bitmap.Encode(SKEncodedImageFormat.Png, 100)
            : bitmap.Encode(SKEncodedImageFormat.Jpeg, _jpegQuality);
        return encoded?.ToArray() ?? throw new InvalidOperationException("SkiaSharp could not encode the image.");
    }
}
