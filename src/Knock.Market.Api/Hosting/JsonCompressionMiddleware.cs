// jscpd:ignore-start -- EXC-018
using System.Globalization;
using System.IO.Compression;
using System.Text.Json.Serialization;
using Knock.Market.Api.Modules.Catalog.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Market.Api.Hosting;

/// <summary>
/// Compresión de JSON superior a 1 KB (spec 010, R-09 y CA-04): la compresión estándar de ASP.NET Core no garantiza umbral, así que este
/// middleware comprime solo respuestas <c>application/json</c> 200 con más de <see cref="MinimumBytes"/> bytes y <c>Accept-Encoding</c>
/// compatible (<c>br</c> preferido, luego <c>gzip</c>). Nunca comprime rutas de autenticación (llevan tokens; amenaza BREACH/CRIME), ni
/// 304/204 ni respuestas sin cuerpo, ni respuestas pequeñas ni las que ya llegan codificadas. Conserva <c>ETag</c> y <c>Cache-Control</c>
/// y agrega <c>Vary: Accept-Encoding</c> a toda respuesta JSON 200 y a los 304, se compriman o no, para que una caché pública no
/// entregue una variante a quien pidió otra. El Gateway no comprime (deja pasar el <c>Content-Encoding</c> tal cual).
/// La respuesta se bufferiza solo hasta <see cref="MaximumBufferedBytes"/>: si es mayor, el resto pasa sin comprimir y no se tocan las
/// cabeceras ya enviadas (el consumo máximo en memoria es el techo más lo que se escribe en esa pasada).
/// </summary>
internal sealed class JsonCompressionMiddleware(RequestDelegate next)
{
    public const int MinimumBytes = 1024;

    /// <summary>Techo del búfer en memoria; respuestas mayores pasan sin comprimir (la protección no depende de tamaño del payload).</summary>
    public const int MaximumBufferedBytes = 5 * 1024 * 1024;

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (ShouldSkipRequest(context))
        {
            await next(context);
            return;
        }

        var originalBody = context.Response.Body;
        var buffered = new CappedBufferStream(originalBody);
        context.Response.Body = buffered;

        try
        {
            await next(context);
        }
        catch
        {
            // Lo bufferizado se descarta: si se volcara, la respuesta empezaría con 200 y el manejador de excepciones ya no podría
            // escribir el 500 de ProblemDetails. Si ya hubo desbordamiento, la respuesta empezó y no hay nada que descartar.
            context.Response.Body = originalBody;
            await buffered.DisposeAsync();
            throw;
        }

        context.Response.Body = originalBody;
        if (buffered.Overflowed)
        {
            await buffered.DisposeAsync();
            return;
        }

        var inner = buffered.ToMemoryStream();
        await WriteBufferedAsync(context, inner, originalBody);
        await buffered.DisposeAsync();
    }

    // Sin Accept-Encoding compatible la respuesta también pasa por el búfer: así lleva Vary igual que la variante comprimida.
    private static bool ShouldSkipRequest(HttpContext context) =>
        context.Request.Path.StartsWithSegments("/api/v1/auth", StringComparison.OrdinalIgnoreCase);

    private static string? PickEncoding(HttpContext context)
    {
        var accepted = context.Request.Headers.AcceptEncoding.ToString();
        if (AcceptsEncoding(accepted, "br"))
        {
            return "br";
        }

        return AcceptsEncoding(accepted, "gzip") ? "gzip" : null;
    }

    private static bool AcceptsEncoding(string accepted, string encoding) =>
        accepted
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(token =>
            {
                var parts = token.Split(';');
                // Solo q=0 (0, 0.0, 0.00, 0.000) rechaza la codificación; q=0.8 o q=0.5 la aceptan con menor preferencia.
                return !parts.Skip(1).Select(part => part.Trim()).Any(part =>
                    part.StartsWith("q=", StringComparison.OrdinalIgnoreCase)
                    && double.TryParse(part.AsSpan(2), NumberStyles.Float, CultureInfo.InvariantCulture, out var quality)
                    && quality <= 0);
            })
            .Select(token => token.Split(';')[0].Trim())
            .Any(token => string.Equals(token, encoding, StringComparison.OrdinalIgnoreCase));

    private static async Task FlushBufferedAsync(MemoryStream buffered, Stream destination, CancellationToken cancellationToken)
    {
        if (buffered.Length > 0)
        {
            buffered.Position = 0;
            await buffered.CopyToAsync(destination, cancellationToken);
        }
    }

    private static async Task WriteBufferedAsync(HttpContext context, MemoryStream buffered, Stream destination)
    {
        var response = context.Response;
        var length = buffered.Length;
        if (VariesByEncoding(response))
        {
            AddVaryAcceptEncoding(response);
        }

        var encoding = PickEncoding(context);
        var compressible = encoding is not null
            && response.StatusCode == StatusCodes.Status200OK
            && length > MinimumBytes
            && IsJson(response.ContentType)
            && !response.Headers.ContainsKey("Content-Encoding");

        if (!compressible)
        {
            await FlushBufferedAsync(buffered, destination, context.RequestAborted);
            return;
        }

        var compressed = new MemoryStream();
        if (encoding == "br")
        {
            await using var stream = new BrotliStream(compressed, CompressionLevel.Optimal, leaveOpen: true);
            buffered.Position = 0;
            await buffered.CopyToAsync(stream, context.RequestAborted);
        }
        else
        {
            await using var stream = new GZipStream(compressed, CompressionLevel.Optimal, leaveOpen: true);
            buffered.Position = 0;
            await buffered.CopyToAsync(stream, context.RequestAborted);
        }

        response.Headers.Remove("Content-Length");
        response.Headers.ContentEncoding = encoding;
        response.ContentLength = compressed.Length;
        compressed.Position = 0;
        await compressed.CopyToAsync(destination, context.RequestAborted);
    }

    private static bool VariesByEncoding(HttpResponse response) =>
        response.StatusCode == StatusCodes.Status304NotModified
        || (response.StatusCode == StatusCodes.Status200OK && IsJson(response.ContentType) && !response.Headers.ContainsKey("Content-Encoding"));

    private static void AddVaryAcceptEncoding(HttpResponse response)
    {
        if (!response.Headers.Vary.Any(value => value is not null && value.Contains("Accept-Encoding", StringComparison.OrdinalIgnoreCase)))
        {
            response.Headers.Append("Vary", "Accept-Encoding");
        }
    }

    private static bool IsJson(string? contentType) =>
        contentType is not null && contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Búfer con techo: escribe en memoria hasta <see cref="MaximumBufferedBytes"/>; al pasar el techo, vuelca lo bufferizado al cuerpo
    /// real y deja pasar el resto sin comprimir. El destino (el cuerpo real) nunca se cierra desde aquí.
    /// </summary>
    private sealed class CappedBufferStream(Stream destination) : Stream
    {
        private readonly MemoryStream _inner = new();

        public bool Overflowed { get; private set; }

        public MemoryStream ToMemoryStream() => _inner;

        public override void Write(byte[] buffer, int offset, int count) => WriteCore(buffer.AsSpan(offset, count));

        public override void Write(ReadOnlySpan<byte> buffer) => WriteCore(buffer);

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (Overflowed)
            {
                await destination.WriteAsync(buffer, cancellationToken);
                return;
            }

            if (_inner.Length + buffer.Length > MaximumBufferedBytes)
            {
                Overflowed = true;
                await FlushInnerAsync(cancellationToken);
                await destination.WriteAsync(buffer, cancellationToken);
                return;
            }

            await _inner.WriteAsync(buffer, cancellationToken);
        }

        private void WriteCore(ReadOnlySpan<byte> buffer)
        {
            if (Overflowed)
            {
                destination.Write(buffer);
                return;
            }

            if (_inner.Length + buffer.Length > MaximumBufferedBytes)
            {
                Overflowed = true;
                FlushInnerCore();
                destination.Write(buffer);
                return;
            }

            _inner.Write(buffer);
        }

        private void FlushInnerCore()
        {
            if (_inner.Length > 0)
            {
                _inner.Position = 0;
                _inner.CopyTo(destination);
            }
        }

        private async Task FlushInnerAsync(CancellationToken cancellationToken)
        {
            if (_inner.Length > 0)
            {
                _inner.Position = 0;
                await _inner.CopyToAsync(destination, cancellationToken);
            }
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override void Flush()
        {
            if (Overflowed)
            {
                destination.Flush();
            }
        }

        public override Task FlushAsync(CancellationToken cancellationToken) =>
            Overflowed ? destination.FlushAsync(cancellationToken) : Task.CompletedTask;

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}

/// <summary>Contexto de serialización JSON generado en compilación (spec 010, T-06): solo los DTO de respuesta de Market.</summary>
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(List<OficioResponse>))]
internal sealed partial class MarketJsonSerializerContext : JsonSerializerContext
{
}

// jscpd:ignore-end -- EXC-018
