using System.Diagnostics;
using Knock.Market.Api.Modules.Media.Domain;
using SkiaSharp;

namespace Knock.Market.Api.Modules.Media.Services;

/// <summary>Imagen limpia lista para moderar y publicar: JPEG sin metadatos y sus dimensiones.</summary>
internal sealed record ImagenProcesada(byte[] Bytes, int Ancho, int Alto);

/// <summary>Resultado de <see cref="ProcesadorImagen"/>: la imagen o el <c>code</c> de rechazo (<see cref="CodigosImagen"/>).</summary>
internal sealed record ResultadoProcesamiento(ImagenProcesada? Imagen, string? Codigo)
{
    public static ResultadoProcesamiento Ok(ImagenProcesada imagen) => new(imagen, null);

    public static ResultadoProcesamiento Rechazada(string codigo) => new(null, codigo);
}

/// <summary>
/// Única pieza de la spec 032 que toca bytes de imagen (§7): bytes de entrada, bytes de salida, sin almacenamiento ni E/S.
/// Valida el formato por los bytes iniciales (R-07), lee la cabecera y aplica los límites de píxeles antes de reservar memoria y un
/// tiempo máximo (R-08), orienta y descarta todos los metadatos, convierte a sRGB, reduce sin ampliar y recodifica a JPEG de calidad 82 (R-09).
/// Reutiliza <see cref="MediaRules"/> (firmas y límites) y la puerta de concurrencia de <see cref="ImageProcessor"/> de la spec 013, que no cambia:
/// las dos canalizaciones comparten las mismas plazas, así que juntas no superan la memoria que acota <c>MediaOptions.MaxConcurrentProcessing</c>.
/// </summary>
internal sealed class ProcesadorImagen(ImageProcessor puerta, TimeSpan? tiempoMaximo = null)
{
    /// <summary>Calidad JPEG de la salida (R-09). SkiaSharp no expone JPEG progresivo: la salida es secuencial.</summary>
    public const int CalidadJpeg = 82;

    /// <summary>Tiempo máximo de decodificación: 5 segundos (R-08).</summary>
    public static readonly TimeSpan TiempoMaximoPorOmision = TimeSpan.FromSeconds(5);

    private static readonly SKColorSpace _srgb = SKColorSpace.CreateSrgb();

    private readonly TimeSpan _tiempoMaximo = tiempoMaximo ?? TiempoMaximoPorOmision;

    /// <summary>Lado mayor de la salida: 1 600 píxeles, 512 en <c>perfil</c> (R-09).</summary>
    public static int LadoMaximo(PropositoImagen proposito) => proposito == PropositoImagen.Perfil ? 512 : 1_600;

    /// <summary>Procesa cuando hay una plaza libre; el tiempo máximo cuenta desde que empieza el trabajo, no desde que espera la plaza.</summary>
    public Task<ResultadoProcesamiento> ProcesarAsync(ArraySegment<byte> contenido, PropositoImagen proposito, CancellationToken cancellationToken) =>
        puerta.RunLimitedAsync(() => Procesar(contenido, proposito), cancellationToken);

    private ResultadoProcesamiento Procesar(ArraySegment<byte> contenido, PropositoImagen proposito)
    {
        if (contenido.Array is null || MediaRules.DetectFormat(contenido) is not { } formato)
        {
            return ResultadoProcesamiento.Rechazada(CodigosImagen.FormatNotSupportedCode);
        }

        var reloj = Stopwatch.StartNew();
        using var flujo = new MemoryStream(contenido.Array, contenido.Offset, contenido.Count, writable: false);
        using var codec = SKCodec.Create(flujo, out var resultado);
        if (resultado != SKCodecResult.Success || Formato(codec.EncodedFormat) != formato || !MediaRules.AreAllowedDimensions(codec.Info.Width, codec.Info.Height))
        {
            return ResultadoProcesamiento.Rechazada(CodigosImagen.ImageInvalidCode);
        }

        using var decodificada = Decodificar(codec);
        if (decodificada is null || reloj.Elapsed > _tiempoMaximo)
        {
            return ResultadoProcesamiento.Rechazada(CodigosImagen.ImageInvalidCode);
        }

        using var derecha = Orientar(decodificada, codec.EncodedOrigin);
        using var reducida = Reducir(derecha ?? decodificada, LadoMaximo(proposito));
        if (reloj.Elapsed > _tiempoMaximo)
        {
            return ResultadoProcesamiento.Rechazada(CodigosImagen.ImageInvalidCode);
        }

        return ResultadoProcesamiento.Ok(new ImagenProcesada(CodificarJpeg(reducida), reducida.Width, reducida.Height));
    }

    private static ImageFormat? Formato(SKEncodedImageFormat formato) => formato switch
    {
        SKEncodedImageFormat.Jpeg => ImageFormat.Jpeg,
        SKEncodedImageFormat.Png => ImageFormat.Png,
        SKEncodedImageFormat.Webp => ImageFormat.WebP,
        _ => null,
    };

    /// <summary>Decodifica todos los píxeles convirtiendo los colores a sRGB (los perfiles de color se descartan con el resto de metadatos); una imagen truncada se rechaza.</summary>
    private static SKBitmap? Decodificar(SKCodec codec)
    {
        var alfa = codec.Info.AlphaType == SKAlphaType.Opaque ? SKAlphaType.Opaque : SKAlphaType.Premul;
        var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, alfa, _srgb);
        var mapa = new SKBitmap(info);
        if (codec.GetPixels(info, mapa.GetPixels()) == SKCodecResult.Success)
        {
            return mapa;
        }

        mapa.Dispose();
        return null;
    }

    /// <summary>Copia los píxeles con la orientación EXIF aplicada; devuelve <see langword="null"/> si no hace falta girar nada.</summary>
    private static SKBitmap? Orientar(SKBitmap origen, SKEncodedOrigin orientacion)
    {
        // Las ocho orientaciones son una transposición opcional seguida de un espejo horizontal y/o vertical.
        var (transponer, espejoX, espejoY) = orientacion switch
        {
            SKEncodedOrigin.TopRight => (false, true, false),
            SKEncodedOrigin.BottomRight => (false, true, true),
            SKEncodedOrigin.BottomLeft => (false, false, true),
            SKEncodedOrigin.LeftTop => (true, false, false),
            SKEncodedOrigin.RightTop => (true, true, false),
            SKEncodedOrigin.RightBottom => (true, true, true),
            SKEncodedOrigin.LeftBottom => (true, false, true),
            _ => (false, false, false),
        };
        if (!transponer && !espejoX && !espejoY)
        {
            return null;
        }

        var ancho = transponer ? origen.Height : origen.Width;
        var alto = transponer ? origen.Width : origen.Height;
        float escalaX = transponer ? 0 : 1, sesgoX = transponer ? 1 : 0, sesgoY = transponer ? 1 : 0, escalaY = transponer ? 0 : 1;
        float moverX = 0, moverY = 0;
        if (espejoX)
        {
            (escalaX, sesgoX, moverX) = (-escalaX, -sesgoX, ancho);
        }

        if (espejoY)
        {
            (sesgoY, escalaY, moverY) = (-sesgoY, -escalaY, alto);
        }

        var destino = new SKBitmap(new SKImageInfo(ancho, alto, origen.Info.ColorType, origen.Info.AlphaType, origen.Info.ColorSpace));
        using var lienzo = new SKCanvas(destino);
        var matriz = new SKMatrix(escalaX, sesgoX, moverX, sesgoY, escalaY, moverY, 0, 0, 1);
        lienzo.SetMatrix(in matriz);
        lienzo.DrawBitmap(origen, 0, 0, SKSamplingOptions.Default, null);
        return destino;
    }

    /// <summary>Reduce para que el lado mayor mida como máximo <paramref name="ladoMaximo"/>; una imagen menor se copia sin ampliarla.</summary>
    private static SKBitmap Reducir(SKBitmap origen, int ladoMaximo)
    {
        var escala = Math.Min(1d, (double)ladoMaximo / Math.Max(origen.Width, origen.Height));
        if (escala >= 1d)
        {
            return origen.Copy() ?? throw new InvalidOperationException("SkiaSharp could not copy the image.");
        }

        var tamano = new SKSizeI(Math.Max(1, (int)Math.Round(origen.Width * escala)), Math.Max(1, (int)Math.Round(origen.Height * escala)));
        return origen.Resize(tamano, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear))
            ?? throw new InvalidOperationException("SkiaSharp could not resize the image.");
    }

    /// <summary>
    /// Compone sobre blanco (JPEG no tiene transparencia) y recodifica. El mapa de destino no declara espacio de color a propósito: los píxeles ya son sRGB y,
    /// si se declarara, el codificador incrustaría el perfil ICC como un segmento más (R-09 pide ningún perfil).
    /// </summary>
    private static byte[] CodificarJpeg(SKBitmap imagen)
    {
        using var opaca = new SKBitmap(new SKImageInfo(imagen.Width, imagen.Height, SKColorType.Rgba8888, SKAlphaType.Opaque));
        using (var lienzo = new SKCanvas(opaca))
        {
            lienzo.Clear(SKColors.White);
            lienzo.DrawBitmap(imagen, 0, 0, SKSamplingOptions.Default, null);
        }

        using var pixeles = opaca.PeekPixels();
        using var flujo = new MemoryStream();
        using var salida = new SKManagedWStream(flujo);
        return pixeles.Encode(salida, new SKJpegEncoderOptions(CalidadJpeg, SKJpegEncoderDownsample.Downsample420, SKJpegEncoderAlphaOption.Ignore))
            ? flujo.ToArray()
            : throw new InvalidOperationException("SkiaSharp could not encode the image.");
    }
}
