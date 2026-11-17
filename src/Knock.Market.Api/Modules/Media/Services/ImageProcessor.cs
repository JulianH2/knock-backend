using Knock.Market.Api.Modules.Media.Data;
using Knock.Market.Api.Modules.Media.Domain;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace Knock.Market.Api.Modules.Media.Services;

/// <summary>
/// Validates by real content and cleans the photo with SkiaSharp: JPEG/PNG/WebP signature, dimensions checked before decoding,
/// full decode, EXIF orientation applied to pixels and re-encoding (which drops EXIF, GPS, XMP and IPTC).
/// Only <see cref="MediaOptions.MaxConcurrentProcessing"/> photos are processed at once per replica; each peak is bounded
/// by <see cref="MediaRules.MaxPixels"/>.
/// </summary>
internal sealed class ImageProcessor(IOptions<MediaOptions> options) : IDisposable
{
    private const int _jpegQuality = 85;

    private readonly SemaphoreSlim _slots = new(options.Value.MaxConcurrentProcessing, options.Value.MaxConcurrentProcessing);

    /// <summary>Processes when a slot is free; rejects invalid JPEG/PNG/WebP or dimensions outside the limits.</summary>
    public Task<MediaOutcome<ProcessedImage>> ProcessAsync(ArraySegment<byte> content, CancellationToken cancellationToken) =>
        ProcessAsync(content, MediaUsage.Request, cancellationToken);

    public Task<MediaOutcome<ProcessedImage>> ProcessAsync(ArraySegment<byte> content, MediaUsage usage, CancellationToken cancellationToken) =>
        RunLimitedAsync(() => Process(content, usage), cancellationToken);

    /// <summary>Runs <paramref name="work"/> without exceeding the configured slots; callers wait asynchronously.</summary>
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

    private MediaOutcome<ProcessedImage> Process(ArraySegment<byte> content, MediaUsage usage)
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

        using var reduced = Reduce(oriented, usage == MediaUsage.Profile ? options.Value.ProfileReducedMaxSide : options.Value.ReducedMaxSide);
        return MediaOutcome<ProcessedImage>.Ok(new ProcessedImage(
            ImageFormat.Jpeg, Encode(oriented), Encode(reduced), oriented.Width, oriented.Height));
    }

    private static ImageFormat? ToFormat(SKEncodedImageFormat format) => format switch
    {
        SKEncodedImageFormat.Jpeg => ImageFormat.Jpeg,
        SKEncodedImageFormat.Png => ImageFormat.Png,
        SKEncodedImageFormat.Webp => ImageFormat.WebP,
        _ => null,
    };

    /// <summary>Decodes every pixel; a truncated or corrupt image (non-Success result) is rejected.</summary>
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

    /// <summary>Decodes and applies orientation; the decoded copy is freed as soon as the rotated one exists.</summary>
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

    /// <summary>Copies pixels with EXIF orientation applied, so the re-encoded image looks the same without EXIF.</summary>
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

    /// <summary>Transformation for each EXIF orientation (1-8) over a <c>w</c> x <c>h</c> image.</summary>
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

    /// <summary>Encodes directly from the bitmap pixels (no <c>SKImage.FromBitmap</c>, which copies a mutable bitmap).</summary>
    private static byte[] Encode(SKBitmap bitmap)
    {
        using var opaque = new SKBitmap(new SKImageInfo(bitmap.Width, bitmap.Height, SKColorType.Rgba8888, SKAlphaType.Opaque));
        using var canvas = new SKCanvas(opaque);
        canvas.Clear(SKColors.White);
        canvas.DrawBitmap(bitmap, 0, 0, SKSamplingOptions.Default, null);
        using var encoded = opaque.Encode(SKEncodedImageFormat.Jpeg, _jpegQuality);
        return encoded?.ToArray() ?? throw new InvalidOperationException("SkiaSharp could not encode the image.");
    }
}
