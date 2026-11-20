using System.Buffers.Binary;
using AwesomeAssertions;
using Knock.Market.Api.Modules.Media.Data;
using Knock.Market.Api.Modules.Media.Domain;
using Knock.Market.Api.Modules.Media.Services;
using SkiaSharp;
using Xunit;

namespace Knock.UnitTest.Media;

/// <summary>Spec 032, T-02: <c>ProcesadorImagen</c> con imágenes generadas en la prueba (R-07 a R-09, CA-05, CA-09 y CA-10).</summary>
public sealed class Spec032ProcesamientoTests : IDisposable
{
    private readonly List<ImageProcessor> _puertas = [];

    public void Dispose() => _puertas.ForEach(puerta => puerta.Dispose());

    private ProcesadorImagen Create(TimeSpan? tiempoMaximo = null)
    {
        _puertas.Add(new ImageProcessor(Microsoft.Extensions.Options.Options.Create(new MediaOptions { MaxConcurrentProcessing = 2 })));
        return new ProcesadorImagen(_puertas[^1], tiempoMaximo);
    }

    private Task<ResultadoProcesamiento> Run(byte[] bytes, PropositoImagen proposito = PropositoImagen.Trabajo, ProcesadorImagen? procesador = null) =>
        (procesador ?? Create()).ProcesarAsync(new ArraySegment<byte>(bytes), proposito, CancellationToken.None);

    [Fact]
    public async Task Quita_Metadatos_Y_Reduce_CA_032_05()
    {
        // 2400 x 1600 con orientación 6 (girada 90 grados) y coordenadas GPS: debe salir vertical, de 1 067 x 1 600, sin metadatos.
        var origen = ConExif(Codificar(Dibujo(2400, 1600), SKEncodedImageFormat.Jpeg), orientacion: 6);
        origen.AsSpan().IndexOf("Exif"u8).Should().BeGreaterThan(0, "la imagen de prueba sí trae metadatos");

        var resultado = await Run(origen);

        resultado.Codigo.Should().BeNull();
        var imagen = resultado.Imagen!;
        (imagen.Ancho, imagen.Alto).Should().Be((1067, 1600));
        SinMetadatos(imagen.Bytes).Should().BeTrue();
        using var salida = SKBitmap.Decode(imagen.Bytes);
        (salida.Width, salida.Height).Should().Be((imagen.Ancho, imagen.Alto));
        // El cuadro rojo estaba arriba a la izquierda; girado 90 grados a la derecha queda arriba a la derecha.
        salida.GetPixel(imagen.Ancho - 60, 60).Red.Should().BeGreaterThan((byte)200);
        salida.GetPixel(60, imagen.Alto - 60).Red.Should().BeLessThan((byte)100);
    }

    [Theory]
    [InlineData(2, 80, 48, 68, 8)]
    [InlineData(3, 80, 48, 68, 39)]
    [InlineData(4, 80, 48, 11, 39)]
    [InlineData(5, 48, 80, 8, 11)]
    [InlineData(6, 48, 80, 39, 11)]
    [InlineData(7, 48, 80, 39, 68)]
    [InlineData(8, 48, 80, 8, 68)]
    public async Task Las_Ocho_Orientaciones_Se_Aplican_A_Los_Pixeles_CA_032_05(int orientacion, int ancho, int alto, int x, int y)
    {
        var resultado = await Run(ConExif(Codificar(Dibujo(80, 48), SKEncodedImageFormat.Jpeg, 100), (ushort)orientacion));

        var imagen = resultado.Imagen!;
        (imagen.Ancho, imagen.Alto).Should().Be((ancho, alto));
        using var salida = SKBitmap.Decode(imagen.Bytes);
        var rojo = salida.GetPixel(x, y);
        (rojo.Red > 200 && rojo.Blue < 60).Should().BeTrue($"el cuadro rojo debe quedar en ({x},{y}) con la orientación {orientacion}");
    }

    [Theory]
    [InlineData("trabajo", 1600, 1200)]
    [InlineData("perfil", 512, 384)]
    public async Task Reduce_El_Lado_Mayor_Segun_El_Proposito_R_09(string nombre, int ancho, int alto)
    {
        var proposito = PropositoImagenExtensions.Parse(nombre)!.Value;
        var resultado = await Run(Codificar(Dibujo(3200, 2400), SKEncodedImageFormat.Jpeg), proposito);

        (resultado.Imagen!.Ancho, resultado.Imagen.Alto).Should().Be((ancho, alto));
    }

    [Theory]
    [InlineData("trabajo", 1600, 1000)]
    [InlineData("perfil", 512, 300)]
    public async Task No_Amplia_Una_Imagen_Menor_R_09(string nombre, int lado, int otro)
    {
        var proposito = PropositoImagenExtensions.Parse(nombre)!.Value;
        // Justo en el límite no se toca; una menor tampoco se amplía.
        var justo = await Run(Codificar(Dibujo(lado, otro), SKEncodedImageFormat.Jpeg), proposito);
        var menor = await Run(Codificar(Dibujo(200, 100), SKEncodedImageFormat.Jpeg), proposito);

        (justo.Imagen!.Ancho, justo.Imagen.Alto).Should().Be((lado, otro));
        (menor.Imagen!.Ancho, menor.Imagen.Alto).Should().Be((200, 100));
    }

    [Theory]
    [InlineData(SKEncodedImageFormat.Png)]
    [InlineData(SKEncodedImageFormat.Webp)]
    public async Task Png_Y_Webp_Con_Transparencia_Salen_Como_Jpeg_Sobre_Blanco_R_09(SKEncodedImageFormat formato)
    {
        using var transparente = new SKBitmap(new SKImageInfo(40, 40, SKColorType.Rgba8888, SKAlphaType.Premul));
        transparente.Erase(SKColors.Transparent);

        var resultado = await Run(Codificar(transparente, formato));

        var bytes = resultado.Imagen!.Bytes;
        MediaRules.DetectFormat(bytes).Should().Be(ImageFormat.Jpeg);
        SinMetadatos(bytes).Should().BeTrue();
        using var salida = SKBitmap.Decode(bytes);
        var pixel = salida.GetPixel(20, 20);
        (pixel.Red > 245 && pixel.Green > 245 && pixel.Blue > 245).Should().BeTrue();
    }

    [Fact]
    public async Task Rechaza_Formato_Falso_CA_032_09()
    {
        // HTML y SVG renombrados como .jpg y declarados image/jpeg: los bytes no coinciden con ningún formato permitido.
        foreach (var falso in new[] { "<html><script>alert(1)</script></html>"u8.ToArray(), "<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>"u8.ToArray(), [], [0xFF] })
        {
            var resultado = await Run(falso);

            resultado.Imagen.Should().BeNull();
            resultado.Codigo.Should().Be(CodigosImagen.FormatNotSupportedCode);
        }
    }

    [Fact]
    public async Task Firma_Valida_Con_Contenido_Roto_Es_Imagen_Invalida_R_08()
    {
        var truncada = Codificar(Dibujo(300, 200), SKEncodedImageFormat.Jpeg).AsSpan(0, 400).ToArray();

        (await Run(truncada)).Codigo.Should().Be(CodigosImagen.ImageInvalidCode);
        (await Run([0xFF, 0xD8, 0xFF, 0x01, 0x02, 0x03])).Codigo.Should().Be(CodigosImagen.ImageInvalidCode);
    }

    [Fact]
    public async Task Rechaza_Imagen_Bomba_CA_032_10()
    {
        // 20 000 x 20 000 declarados en la cabecera de un archivo de unos 100 bytes: se rechaza por la cabecera, sin decodificar (la imagen completa serían 1,6 GB).
        var bomba = PngSoloCabecera(20_000, 20_000);

        var resultado = await Run(bomba);

        resultado.Imagen.Should().BeNull();
        resultado.Codigo.Should().Be(CodigosImagen.ImageInvalidCode);
    }

    [Fact]
    public async Task Rechaza_Un_Lado_Mayor_A_8000_Y_Acepta_8000_R_08()
    {
        using var ancha = new SKBitmap(new SKImageInfo(8001, 1, SKColorType.Rgba8888, SKAlphaType.Opaque));
        using var limite = new SKBitmap(new SKImageInfo(8000, 1, SKColorType.Rgba8888, SKAlphaType.Opaque));

        (await Run(Codificar(ancha, SKEncodedImageFormat.Png))).Codigo.Should().Be(CodigosImagen.ImageInvalidCode);
        (await Run(Codificar(limite, SKEncodedImageFormat.Png))).Imagen!.Ancho.Should().Be(1600);
    }

    [Fact]
    public async Task Pasado_El_Tiempo_Maximo_Es_Imagen_Invalida_R_08()
    {
        var resultado = await Run(Codificar(Dibujo(600, 400), SKEncodedImageFormat.Jpeg), procesador: Create(TimeSpan.Zero));

        resultado.Codigo.Should().Be(CodigosImagen.ImageInvalidCode);
        ProcesadorImagen.TiempoMaximoPorOmision.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task La_Misma_Imagen_Produce_Los_Mismos_Bytes_R_11()
    {
        var origen = Codificar(Dibujo(900, 600), SKEncodedImageFormat.Jpeg);

        var primera = (await Run(origen)).Imagen!.Bytes;
        var segunda = (await Run(origen)).Imagen!.Bytes;

        segunda.Should().Equal(primera, "el nombre público es la huella de estos bytes, así que deben ser deterministas");
    }

    /// <summary>Fondo azul con un cuadro rojo de 16 x 14 en (4, 2) y, si la imagen es grande, un cuadro rojo proporcional arriba a la izquierda.</summary>
    private static SKBitmap Dibujo(int ancho, int alto)
    {
        var mapa = new SKBitmap(ancho, alto);
        using var lienzo = new SKCanvas(mapa);
        lienzo.Clear(SKColors.Blue);
        using var rojo = new SKPaint { Color = SKColors.Red };
        lienzo.DrawRect(4, 2, ancho >= 1000 ? 120 : 16, ancho >= 1000 ? 120 : 14, rojo);
        return mapa;
    }

    private static byte[] Codificar(SKBitmap mapa, SKEncodedImageFormat formato, int calidad = 90)
    {
        using (mapa)
        {
            using var datos = mapa.Encode(formato, calidad);
            return datos!.ToArray();
        }
    }

    /// <summary>Inserta, justo después de SOI, un segmento APP1 con la orientación y un IFD de GPS (latitud «N»).</summary>
    private static byte[] ConExif(byte[] jpeg, ushort orientacion)
    {
        var tiff = new byte[8 + 2 + 24 + 4 + 2 + 12 + 4];
        "II"u8.CopyTo(tiff);
        BinaryPrimitives.WriteUInt16LittleEndian(tiff.AsSpan(2), 42);
        BinaryPrimitives.WriteUInt32LittleEndian(tiff.AsSpan(4), 8);
        BinaryPrimitives.WriteUInt16LittleEndian(tiff.AsSpan(8), 2);
        Entrada(tiff, 10, 0x0112, 3, 1, orientacion);
        Entrada(tiff, 22, 0x8825, 4, 1, 38);
        BinaryPrimitives.WriteUInt16LittleEndian(tiff.AsSpan(38), 1);
        Entrada(tiff, 40, 0x0001, 2, 2, 0x004E);
        var segmento = new byte[4 + 6 + tiff.Length];
        segmento[0] = 0xFF;
        segmento[1] = 0xE1;
        BinaryPrimitives.WriteUInt16BigEndian(segmento.AsSpan(2), (ushort)(segmento.Length - 2));
        "Exif\0\0"u8.CopyTo(segmento.AsSpan(4));
        tiff.CopyTo(segmento, 10);
        return [.. jpeg.AsSpan(0, 2), .. segmento, .. jpeg.AsSpan(2)];
    }

    private static void Entrada(byte[] tiff, int desplazamiento, ushort etiqueta, ushort tipo, uint cuenta, uint valor)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(tiff.AsSpan(desplazamiento), etiqueta);
        BinaryPrimitives.WriteUInt16LittleEndian(tiff.AsSpan(desplazamiento + 2), tipo);
        BinaryPrimitives.WriteUInt32LittleEndian(tiff.AsSpan(desplazamiento + 4), cuenta);
        BinaryPrimitives.WriteUInt32LittleEndian(tiff.AsSpan(desplazamiento + 8), valor);
    }

    /// <summary>JPEG sin ningún segmento de aplicación salvo el JFIF (APP0): ni EXIF, ni XMP, ni IPTC, ni perfil de color, ni comentarios.</summary>
    private static bool SinMetadatos(byte[] jpeg)
    {
        if (jpeg.AsSpan().IndexOf("Exif"u8) >= 0)
        {
            return false;
        }

        for (var i = 2; i + 3 < jpeg.Length && jpeg[i] == 0xFF && jpeg[i + 1] != 0xDA;)
        {
            if (jpeg[i + 1] is (>= 0xE1 and <= 0xEF) or 0xFE)
            {
                return false;
            }

            i += 2 + BinaryPrimitives.ReadUInt16BigEndian(jpeg.AsSpan(i + 2));
        }

        return true;
    }

    /// <summary>PNG con firma, cabecera (IHDR) y el comienzo de los datos, sin un solo píxel: lo justo para que la cabecera se lea.</summary>
    private static byte[] PngSoloCabecera(int ancho, int alto)
    {
        var ihdr = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr, ancho);
        BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(4), alto);
        ihdr[8] = 8;
        ihdr[9] = 2;
        return [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, .. Trozo("IHDR"u8, ihdr), .. Trozo("IDAT"u8, [0x78, 0x9C, 0x01, 0x00, 0x00, 0xFF, 0xFF])];
    }

    private static byte[] Trozo(ReadOnlySpan<byte> tipo, byte[] datos)
    {
        var trozo = new byte[12 + datos.Length];
        BinaryPrimitives.WriteInt32BigEndian(trozo, datos.Length);
        tipo.CopyTo(trozo.AsSpan(4));
        datos.CopyTo(trozo, 8);
        var crc = uint.MaxValue;
        foreach (var b in trozo.AsSpan(4, 4 + datos.Length))
        {
            crc ^= b;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
            }
        }

        BinaryPrimitives.WriteUInt32BigEndian(trozo.AsSpan(8 + datos.Length), ~crc);
        return trozo;
    }
}
