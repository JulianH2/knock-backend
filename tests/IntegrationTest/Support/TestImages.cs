using System.Text;
using SkiaSharp;

namespace Knock.IntegrationTest.Support;

/// <summary>
/// Imágenes sintéticas generadas en la prueba (nunca fotos reales). El JPG lleva un bloque EXIF con marca, modelo,
/// fecha, orientación 6 (girar 90° a la derecha) y coordenadas GPS inventadas.
/// </summary>
internal static class TestImages
{
    public const string ExifMake = "KnockCam";
    public const string ExifModel = "Model-X1";
    public const string ExifDate = "2026:10:21 12:00:00";

    private static readonly byte[] _exifHeader = "Exif\0\0"u8.ToArray();

    /// <summary>JPG de <paramref name="width"/> × <paramref name="height"/>: mitad izquierda roja y derecha azul, con EXIF y GPS.</summary>
    public static byte[] JpegWithExifAndGps(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap))
        using (var red = new SKPaint { Color = SKColors.Red })
        using (var blue = new SKPaint { Color = SKColors.Blue })
        {
            canvas.DrawRect(0, 0, width / 2f, height, red);
            canvas.DrawRect(width / 2f, 0, width / 2f, height, blue);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var jpeg = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        return InsertApp1(jpeg.ToArray(), [.. _exifHeader, .. BuildTiff()]);
    }

    /// <summary>PNG gris de <paramref name="width"/> × <paramref name="height"/> (sin metadatos).</summary>
    public static byte[] Png(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.Gray);
        using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return png.ToArray();
    }

    public static byte[] PdfNamedAsJpg() => Encoding.ASCII.GetBytes("%PDF-1.7\n1 0 obj << /Type /Catalog >> endobj\ntrailer << /Root 1 0 R >>\n%%EOF\n");

    /// <summary>Bytes que empiezan con la firma JPG y miden <paramref name="sizeBytes"/> (no es una imagen válida).</summary>
    public static byte[] JpegSignaturePadding(int sizeBytes)
    {
        var content = new byte[sizeBytes];
        content[0] = 0xFF;
        content[1] = 0xD8;
        content[2] = 0xFF;
        return content;
    }

    public static bool ContainsAscii(byte[] content, string text) => ContainsBytes(content, Encoding.ASCII.GetBytes(text));

    public static bool ContainsBytes(byte[] content, byte[] sequence) => content.AsSpan().IndexOf(sequence) >= 0;

    public static SKEncodedOrigin ReadOrigin(byte[] image)
    {
        using var stream = new MemoryStream(image, writable: false);
        using var codec = SKCodec.Create(stream);
        return codec.EncodedOrigin;
    }

    /// <summary>Marcadores APPn y COM de un JPG, en orden, hasta el inicio de los datos de imagen (SOS).</summary>
    public static IReadOnlyList<byte> JpegMetadataMarkers(byte[] jpeg)
    {
        var markers = new List<byte>();
        var offset = 2;
        while (offset + 4 <= jpeg.Length && jpeg[offset] == 0xFF && jpeg[offset + 1] != 0xDA)
        {
            var marker = jpeg[offset + 1];
            if (marker is >= 0xE0 and <= 0xEF or 0xFE)
            {
                markers.Add(marker);
            }

            offset += 2 + ((jpeg[offset + 2] << 8) | jpeg[offset + 3]);
        }

        return markers;
    }

    private static byte[] InsertApp1(byte[] jpeg, byte[] payload)
    {
        var length = payload.Length + 2;
        byte[] segment = [0xFF, 0xE1, (byte)(length >> 8), (byte)length, .. payload];
        return [.. jpeg.AsSpan(0, 2), .. segment, .. jpeg.AsSpan(2)];
    }

    /// <summary>TIFF little-endian: IFD0 (Make, Model, Orientation, DateTime, puntero GPS) e IFD GPS con lat/long sintéticas.</summary>
    private static byte[] BuildTiff()
    {
        byte[] make = [.. Encoding.ASCII.GetBytes(ExifMake), 0];
        byte[] model = [.. Encoding.ASCII.GetBytes(ExifModel), 0];
        byte[] date = [.. Encoding.ASCII.GetBytes(ExifDate), 0];
        var latitude = Rationals(20, 3, 5);
        var longitude = Rationals(99, 20, 40);

        const int ifd0 = 8;
        const int ifd0Size = 2 + (5 * 12) + 4;
        var makeAt = ifd0 + ifd0Size;
        var modelAt = makeAt + make.Length;
        var dateAt = modelAt + model.Length;
        var gpsAt = dateAt + date.Length;
        var latitudeAt = gpsAt + 2 + (4 * 12) + 4;
        var longitudeAt = latitudeAt + latitude.Length;

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write("II"u8);
        writer.Write((ushort)42);
        writer.Write((uint)ifd0);
        writer.Write((ushort)5);
        Entry(writer, 0x010F, 2, make.Length, makeAt);
        Entry(writer, 0x0110, 2, model.Length, modelAt);
        Entry(writer, 0x0112, 3, 1, 6);
        Entry(writer, 0x0132, 2, date.Length, dateAt);
        Entry(writer, 0x8825, 4, 1, gpsAt);
        writer.Write(0u);
        writer.Write(make);
        writer.Write(model);
        writer.Write(date);
        writer.Write((ushort)4);
        Entry(writer, 0x0001, 2, 2, 'N');
        Entry(writer, 0x0002, 5, 3, latitudeAt);
        Entry(writer, 0x0003, 2, 2, 'W');
        Entry(writer, 0x0004, 5, 3, longitudeAt);
        writer.Write(0u);
        writer.Write(latitude);
        writer.Write(longitude);
        writer.Flush();
        return stream.ToArray();
    }

    /// <summary>Entrada de IFD (12 bytes); un valor que cabe en 4 bytes va en línea (SHORT o ASCII de un carácter).</summary>
    private static void Entry(BinaryWriter writer, ushort tag, ushort type, int count, int valueOrOffset)
    {
        writer.Write(tag);
        writer.Write(type);
        writer.Write((uint)count);
        writer.Write((uint)valueOrOffset);
    }

    private static byte[] Rationals(params uint[] values)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        foreach (var value in values)
        {
            writer.Write(value);
            writer.Write(1u);
        }

        writer.Flush();
        return stream.ToArray();
    }
}
