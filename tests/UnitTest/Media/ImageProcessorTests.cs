using AwesomeAssertions;
using Knock.Market.Api.Modules.Media.Data;
using Knock.Market.Api.Modules.Media.Domain;
using Knock.Market.Api.Modules.Media.Services;
using Microsoft.Extensions.Options;
using SkiaSharp;
using Xunit;

namespace Knock.UnitTest.Media;

/// <summary>OI-2: ImageProcessor cleans EXIF GPS, reduces deterministically and rejects fake signatures.</summary>
public sealed class ImageProcessorTests
{
    [Theory]
    [InlineData("perfil", 512, 384)]
    [InlineData("solicitud", 1600, 1200)]
    [InlineData("trabajo", 1600, 1200)]
    public async Task Process_ReducesByUsage(string usage, int width, int height)
    {
        using var processor = new ImageProcessor(Options.Create(new MediaOptions()));
        var result = await processor.ProcessAsync(new ArraySegment<byte>(Jpeg(2000, 1500)), MediaRules.ParseUsage(usage)!.Value, CancellationToken.None);
        result.Status.Should().Be(MediaOutcomeStatus.Success);
        using var codec = SKCodec.Create(new MemoryStream(result.Value!.Reduced));
        codec.Info.Width.Should().Be(width);
        codec.Info.Height.Should().Be(height);
    }
    [Theory]
    [InlineData(SKEncodedImageFormat.Png)]
    [InlineData(SKEncodedImageFormat.Webp)]
    public async Task Process_TransparentImage_ProducesWhiteJpegWithoutMetadata_CA_013_01(SKEncodedImageFormat format)
    {
        using var processor = CreateProcessor();
        using var bitmap = new SKBitmap(new SKImageInfo(32, 32, SKColorType.Rgba8888, SKAlphaType.Premul));
        bitmap.Erase(SKColors.Transparent);
        using var encoded = bitmap.Encode(format, 100);
        var result = await processor.ProcessAsync(new ArraySegment<byte>(encoded.ToArray()), CancellationToken.None);
        result.Status.Should().Be(MediaOutcomeStatus.Success);
        foreach (var bytes in new[] { result.Value!.Original, result.Value.Reduced })
        {
            using var codec = SKCodec.Create(new MemoryStream(bytes));
            codec.EncodedFormat.Should().Be(SKEncodedImageFormat.Jpeg);
            HasApp1Segment(bytes).Should().BeFalse();
            using var decoded = SKBitmap.Decode(bytes);
            decoded.GetPixel(0, 0).Red.Should().BeGreaterThan((byte)245);
            decoded.GetPixel(0, 0).Green.Should().BeGreaterThan((byte)245);
            decoded.GetPixel(0, 0).Blue.Should().BeGreaterThan((byte)245);
        }
    }
    [Fact]
    public async Task Process_StripsGpsCoordinates_CA_013_01()
    {
        using var processor = CreateProcessor();
        byte[] withGps = JpegWithGps(300, 200);

        var outcome = await processor.ProcessAsync(new ArraySegment<byte>(withGps), CancellationToken.None);

        outcome.Status.Should().Be(MediaOutcomeStatus.Success);
        outcome.Value.Should().NotBeNull();
        var processed = outcome.Value;
        processed.Original.Should().NotContain("Exif"u8.ToArray(), "re-encoded bytes carry no EXIF segment");
        HasApp1Segment(processed.Original).Should().BeFalse("re-encoded bytes carry no APP1 metadata segment");
        HasApp1Segment(processed.Reduced).Should().BeFalse("reduced bytes carry no APP1 metadata segment");
    }

    [Theory]
    [InlineData(2, 80, 48, 68, 8)]
    [InlineData(3, 80, 48, 68, 39)]
    [InlineData(4, 80, 48, 11, 39)]
    [InlineData(5, 48, 80, 8, 11)]
    [InlineData(6, 48, 80, 39, 11)]
    [InlineData(7, 48, 80, 39, 68)]
    [InlineData(8, 48, 80, 8, 68)]
    public async Task Process_ExifOrientation_AppliesToPixelsAndStripsMetadata_CA_013_01(
        ushort orientation, int width, int height, int markerX, int markerY)
    {
        using var bitmap = new SKBitmap(80, 48);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Blue);
        using var paint = new SKPaint { Color = SKColors.Red };
        canvas.DrawRect(4, 2, 16, 14, paint);
        using var jpeg = bitmap.Encode(SKEncodedImageFormat.Jpeg, 100);
        byte[] source = jpeg.ToArray();
        using var input = new MemoryStream();
        await input.WriteAsync(source.AsMemory(0, 2));
        await input.WriteAsync(BuildExifApp1(orientation));
        await input.WriteAsync(source.AsMemory(2));
        using var sourceCodec = SKCodec.Create(new MemoryStream(input.ToArray()));
        ((int)sourceCodec.EncodedOrigin).Should().Be(orientation);

        using var processor = CreateProcessor();
        var outcome = await processor.ProcessAsync(new ArraySegment<byte>(input.ToArray()), CancellationToken.None);

        outcome.Status.Should().Be(MediaOutcomeStatus.Success);
        outcome.Value!.Width.Should().Be(width);
        outcome.Value.Height.Should().Be(height);
        foreach (var bytes in new[] { outcome.Value.Original, outcome.Value.Reduced })
        {
            HasApp1Segment(bytes).Should().BeFalse();
            using var decoded = SKBitmap.Decode(bytes);
            decoded.Width.Should().Be(width);
            decoded.Height.Should().Be(height);
            var marker = decoded.GetPixel(markerX, markerY);
            marker.Red.Should().BeGreaterThan((byte)200);
            marker.Blue.Should().BeLessThan((byte)50);
        }
    }

    [Fact]
    public async Task Process_ReducesLongestSideToConfiguredLimit_OI2_01()
    {
        using var processor = CreateProcessor();
        byte[] source = Jpeg(2000, 1500);

        var outcome = await processor.ProcessAsync(new ArraySegment<byte>(source), CancellationToken.None);

        outcome.Status.Should().Be(MediaOutcomeStatus.Success);
        outcome.Value.Should().NotBeNull();
        var processed = outcome.Value;
        processed.Width.Should().Be(2000);
        processed.Height.Should().Be(1500);
        using var codec = SKCodec.Create(new MemoryStream(processed.Reduced));
        codec.Info.Width.Should().Be(1024);
        codec.Info.Height.Should().Be(768);
    }

    [Theory]
    [InlineData("JVBERi0=")] // %PDF magic with a .jpg-like declaration
    [InlineData("/9j/Zm9v")] // JPEG SOI magic but truncated garbage
    public async Task Process_RejectsNonImageSignature_OI2_01(string base64)
    {
        using var processor = CreateProcessor();
        byte[] content = Convert.FromBase64String(base64);

        var outcome = await processor.ProcessAsync(new ArraySegment<byte>(content), CancellationToken.None);

        outcome.Status.Should().Be(MediaOutcomeStatus.Unprocessable);
        outcome.Detail.Should().Be(MediaRejection.InvalidFile);
    }

    private static ImageProcessor CreateProcessor() =>
        new(Options.Create(new MediaOptions { ReducedMaxSide = 1024, MaxConcurrentProcessing = 2 }));

    private static byte[] Jpeg(int width, int height)
    {
        using var bitmap = new SKBitmap(new SKImageInfo(width, height));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.CornflowerBlue);
        using var data = bitmap.Encode(SKEncodedImageFormat.Jpeg, 90);
        return data!.ToArray();
    }

    /// <summary>Builds a JPEG carrying an APP1 EXIF segment whose GPS IFD encodes a distinctive marker.</summary>
    private static byte[] JpegWithGps(int width, int height)
    {
        byte[] jpeg = Jpeg(width, height);
        byte[] app1 = BuildExifApp1();
        // JPEG = SOI(2) + APP1 segment + rest. SkiaSharp emits SOI then APP0; insert APP1 right after SOI.
        using var output = new MemoryStream();
        output.Write(jpeg, 0, 2);
        output.Write(app1, 0, app1.Length);
        output.Write(jpeg, 2, jpeg.Length - 2);
        return output.ToArray();
    }

    private static bool HasApp1Segment(byte[] jpeg)
    {
        for (int i = 2; i + 1 < jpeg.Length; i++)
        {
            if (jpeg[i] == 0xFF && jpeg[i + 1] == 0xE1)
            {
                return true;
            }
        }

        return false;
    }

    private static byte[] BuildExifApp1(ushort? orientation = null)
    {
        using var tiff = new MemoryStream();
        using var writer = new BinaryWriter(tiff, System.Text.Encoding.ASCII, leaveOpen: true);
        // TIFF header (little-endian, magic 42, IFD0 at offset 8).
        writer.Write((byte)'I');
        writer.Write((byte)'I');
        writer.Write((short)42);
        writer.Write(8u);
        // IFD0: optional orientation followed by the GPS pointer.
        int extra = orientation.HasValue ? 12 : 0;
        writer.Write((ushort)(orientation.HasValue ? 2 : 1));
        if (orientation.HasValue)
        {
            writer.Write((ushort)0x0112); // Orientation, SHORT, count 1
            writer.Write((ushort)3);
            writer.Write(1u);
            writer.Write(orientation.Value);
            writer.Write((ushort)0);
        }
        writer.Write((ushort)0x8825);
        writer.Write((ushort)4);
        writer.Write(1u);
        writer.Write((uint)(26 + extra));
        writer.Write(0u); // next IFD: none
        // GPS IFD: four entries.
        writer.Write((ushort)4);
        WriteAsciiEntry(writer, 0x0001, "N"); // GPSLatitudeRef
        WriteRationalEntry(writer, 0x0002, 80 + extra, 19, 25, 56); // GPSLatitude: 19°25'56/100"
        WriteAsciiEntry(writer, 0x0003, "W"); // GPSLongitudeRef
        WriteRationalEntry(writer, 0x0004, 104 + extra, 99, 7, 46); // GPSLongitude: 99°07'46/100"
        writer.Write(0u);
        // Latitude rationals.
        WriteRational(writer, 19, 1);
        WriteRational(writer, 25, 1);
        WriteRational(writer, 56, 100);
        // Longitude rationals.
        WriteRational(writer, 99, 1);
        WriteRational(writer, 7, 1);
        WriteRational(writer, 46, 100);
        writer.Flush();

        byte[] tiffBytes = tiff.ToArray();
        using var segment = new MemoryStream();
        int length = 2 + 6 + tiffBytes.Length;
        segment.WriteByte(0xFF);
        segment.WriteByte(0xE1);
        segment.WriteByte((byte)(length >> 8));
        segment.WriteByte((byte)length);
        byte[] exif = "Exif\0\0"u8.ToArray();
        segment.Write(exif, 0, exif.Length);
        segment.Write(tiffBytes, 0, tiffBytes.Length);
        return segment.ToArray();
    }

    private static void WriteAsciiEntry(BinaryWriter writer, int tag, string value)
    {
        writer.Write((ushort)tag);
        writer.Write((ushort)2); // ASCII
        writer.Write(2u); // count: char + NUL
        writer.Write((byte)value[0]);
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((byte)0);
    }

    private static void WriteRationalEntry(BinaryWriter writer, int tag, int offset, int degrees, int minutes, int secondsHundredths)
    {
        writer.Write((ushort)tag);
        writer.Write((ushort)5); // RATIONAL
        writer.Write(3u);
        writer.Write((uint)offset);
        _ = (degrees, minutes, secondsHundredths);
    }

    private static void WriteRational(BinaryWriter writer, int numerator, int denominator)
    {
        writer.Write((uint)numerator);
        writer.Write((uint)denominator);
    }
}
