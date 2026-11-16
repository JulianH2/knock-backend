extern alias identity;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Knock.Market.Api.Hosting;
using Microsoft.AspNetCore.Http;
using Xunit;
using IdentityDtos = identity::Knock.Identity.Api.Modules.Sesiones.Dtos;
using IdentityHosting = identity::Knock.Identity.Api.Hosting;

namespace Knock.UnitTest.Hosting;

/// <summary>Spec 010, R-09 y CA-04: el middleware de Market/Identity comprime solo JSON 200 de más de 1 KB con Accept-Encoding compatible; la autenticación nunca se comprime.</summary>
public sealed class JsonCompressionTests
{
    private static readonly JsonSerializerOptions _webOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, TypeInfoResolver = MarketJsonSerializerContext.Default };

    private static readonly JsonSerializerOptions _identityWebOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, TypeInfoResolver = IdentityHosting.IdentityJsonSerializerContext.Default };

    private static async Task<(HttpContext Context, byte[] Body)> RunMiddlewareAsync(
        string path,
        string? acceptEncoding,
        int bodySize,
        int status = StatusCodes.Status200OK,
        string contentType = "application/json; charset=utf-8",
        string? preSetEncoding = null,
        string? etag = null,
        string? cacheControl = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        if (acceptEncoding is not null)
        {
            context.Request.Headers["Accept-Encoding"] = acceptEncoding;
        }

        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var payload = Encoding.UTF8.GetBytes(new string('x', bodySize));
        var middleware = new JsonCompressionMiddleware(async ctx =>
        {
            ctx.Response.StatusCode = status;
            ctx.Response.ContentType = contentType;
            if (etag is not null)
            {
                ctx.Response.Headers.ETag = etag;
            }

            if (cacheControl is not null)
            {
                ctx.Response.Headers.CacheControl = cacheControl;
            }

            if (preSetEncoding is not null)
            {
                ctx.Response.Headers.ContentEncoding = preSetEncoding;
            }

            await ctx.Response.Body.WriteAsync(payload, CancellationToken.None);
        });

        await middleware.InvokeAsync(context);

        return (context, responseBody.ToArray());
    }

    [Theory]
    [InlineData(1024, false)]
    [InlineData(1025, true)]
    public async Task CompressesOnlyAboveOneKibibyte_CA_010_04(int size, bool shouldCompress)
    {
        var (context, body) = await RunMiddlewareAsync("/api/v1/oficios", "br", size);

        if (shouldCompress)
        {
            context.Response.Headers.ContentEncoding.ToString().Should().Be("br");
            body.Length.Should().BeLessThan(size);
        }
        else
        {
            context.Response.Headers.ContentEncoding.ToString().Should().BeEmpty();
            body.Length.Should().Be(size);
        }
    }

    [Fact]
    public async Task PrefersBrotliThenGzip_CA_010_04()
    {
        var (brotli, _) = await RunMiddlewareAsync("/api/v1/oficios", "gzip, br", 2048);
        brotli.Response.Headers.ContentEncoding.ToString().Should().Be("br");

        var (gzip, _) = await RunMiddlewareAsync("/api/v1/oficios", "gzip", 2048);
        gzip.Response.Headers.ContentEncoding.ToString().Should().Be("gzip");
    }

    [Theory]
    [InlineData("gzip;q=0.8", "gzip")]
    [InlineData("gzip;q=0.5", "gzip")]
    [InlineData("br;q=1.0, gzip;q=0.8", "br")]
    [InlineData("br;q=0.0, gzip;q=0.8", "gzip")]
    public async Task OnlyZeroQualityRejectsAnEncoding_CA_010_04(string header, string expected)
    {
        var (context, _) = await RunMiddlewareAsync("/api/v1/oficios", header, 2048);

        context.Response.Headers.ContentEncoding.ToString().Should().Be(expected, "solo q=0 rechaza una codificación (RFC 9110, 12.4.2)");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("identity")]
    [InlineData("br;q=0")]
    public async Task DoesNotCompressWithoutCompatibleAcceptEncoding_CA_010_04(string? header)
    {
        var (context, body) = await RunMiddlewareAsync("/api/v1/oficios", header, 2048);

        context.Response.Headers.ContentEncoding.ToString().Should().BeEmpty();
        body.Length.Should().Be(2048);
    }

    [Theory]
    [InlineData("/api/v1/auth/firebase")]
    [InlineData("/api/v1/auth/refresh")]
    [InlineData("/api/v1/auth/logout")]
    public async Task NeverCompressesAuthentication_CA_010_04(string path)
    {
        var (context, body) = await RunMiddlewareAsync(path, "br", 2048);

        context.Response.Headers.ContentEncoding.ToString().Should().BeEmpty();
        body.Length.Should().Be(2048);
    }

    [Theory]
    [InlineData(StatusCodes.Status304NotModified, 0)]
    [InlineData(StatusCodes.Status204NoContent, 0)]
    [InlineData(StatusCodes.Status401Unauthorized, 2048)]
    public async Task DoesNotCompressNonSuccessOrEmptyResponses_CA_010_04(int status, int size)
    {
        var (context, body) = await RunMiddlewareAsync("/api/v1/oficios", "br", size, status);

        context.Response.Headers.ContentEncoding.ToString().Should().BeEmpty();
        body.Length.Should().Be(size);
    }

    [Fact]
    public async Task DoesNotCompressNonJsonOrAlreadyEncoded_CA_010_04()
    {
        var (text, textBody) = await RunMiddlewareAsync("/api/v1/oficios", "br", 2048, contentType: "text/plain");
        text.Response.Headers.ContentEncoding.ToString().Should().BeEmpty();
        textBody.Length.Should().Be(2048);

        var (encoded, encodedBody) = await RunMiddlewareAsync("/api/v1/oficios", "br", 2048, preSetEncoding: "gzip");
        encoded.Response.Headers.ContentEncoding.ToString().Should().Be("gzip");
        encodedBody.Length.Should().Be(2048);
    }

    [Fact]
    public async Task PreservesETagCacheControlAndSetsVary_CA_010_04()
    {
        var (context, _) = await RunMiddlewareAsync("/api/v1/oficios", "br", 2048, etag: "W/\"abc\"", cacheControl: "public, max-age=86400");

        context.Response.Headers.ETag.ToString().Should().Be("W/\"abc\"");
        context.Response.Headers.CacheControl.ToString().Should().Be("public, max-age=86400");
        context.Response.Headers["Vary"].ToString().Should().Contain("Accept-Encoding");
    }

    [Fact]
    public async Task CompressedBodyRoundTrips_CA_010_04()
    {
        var original = Encoding.UTF8.GetBytes("{\"clave\":\"ele\",\"nombre\":\"Electricidad\"}" + new string('x', 2000));
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/oficios";
        context.Request.Headers["Accept-Encoding"] = "br";
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        var middleware = new JsonCompressionMiddleware(async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Body.WriteAsync(original, CancellationToken.None);
        });

        await middleware.InvokeAsync(context);

        var compressed = responseBody.ToArray();
        using var input = new MemoryStream(compressed);
        await using var brotli = new BrotliStream(input, CompressionMode.Decompress);
        using var reader = new StreamReader(brotli);
        var decompressed = await reader.ReadToEndAsync();
        decompressed.Should().Be(Encoding.UTF8.GetString(original));
    }

    [Fact]
    public async Task LargeResponseIsNotBufferedBeyondCap_CA_010_04()
    {
        var size = JsonCompressionMiddleware.MaximumBufferedBytes + 1;
        var (context, body) = await RunMiddlewareAsync("/api/v1/oficios", "br", size, etag: "W/\"abc\"");

        context.Response.Headers.ContentEncoding.ToString().Should().BeEmpty();
        body.Length.Should().Be(size);
        context.Response.Headers.ETag.ToString().Should().Be("W/\"abc\"");
    }

    [Fact]
    public async Task IdentityLargeResponseAlsoPassesThrough_CA_010_04()
    {
        var size = IdentityHosting.JsonCompressionMiddleware.MaximumBufferedBytes + 1;
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/me";
        context.Request.Headers["Accept-Encoding"] = "gzip, br";
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        var middleware = new IdentityHosting.JsonCompressionMiddleware(async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Body.WriteAsync(new byte[size], CancellationToken.None);
        });

        await middleware.InvokeAsync(context);

        context.Response.Headers.ContentEncoding.ToString().Should().BeEmpty();
        responseBody.ToArray().Length.Should().Be(size);
    }

    [Fact]
    public void SourceGeneratedContextSerializesIdentityDtosCamelCase_CA_010_04()
    {
        var session = JsonSerializer.Serialize(new IdentityDtos.SessionResponse("a", "r", true, false), _identityWebOptions);
        session.Should().Contain("\"accessToken\":\"a\"");
        session.Should().Contain("\"refreshToken\":\"r\"");

        var request = JsonSerializer.Deserialize<IdentityDtos.RefreshRequest>(
            "{\"refreshToken\":\"r123\"}", _identityWebOptions);
        request.Should().NotBeNull();
        request?.RefreshToken.Should().Be("r123");
    }

    [Fact]
    public void SourceGeneratedContextSerializesMarketDtosCamelCase_CA_010_04()
    {
        var json = JsonSerializer.Serialize(
            new List<Knock.Market.Api.Modules.Catalog.Dtos.OficioResponse> { new("ele", "Electricidad", "domicilio") },
            _webOptions);

        json.Should().Contain("\"clave\":\"ele\"");
        json.Should().Contain("\"nombre\":\"Electricidad\"");
        json.Should().Contain("\"modalidad\":\"domicilio\"");
    }

    [Fact]
    public async Task IdentityMiddlewareNeverCompressesAuth_CA_010_04()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/auth/firebase";
        context.Request.Headers["Accept-Encoding"] = "gzip, br";
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        var middleware = new IdentityHosting.JsonCompressionMiddleware(async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Body.WriteAsync(new byte[2048], CancellationToken.None);
        });

        await middleware.InvokeAsync(context);

        context.Response.Headers.ContentEncoding.ToString().Should().BeEmpty();
        responseBody.ToArray().Length.Should().Be(2048);
    }

    [Theory]
    [InlineData(null, 2048)]
    [InlineData("identity", 2048)]
    [InlineData("br", 100)]
    public async Task UncompressedJsonVariant_AlsoSendsVary_CA_010_04(string? acceptEncoding, int size)
    {
        var (context, body) = await RunMiddlewareAsync("/api/v1/oficios", acceptEncoding, size, cacheControl: "public, max-age=86400");

        context.Response.Headers.ContentEncoding.ToString().Should().BeEmpty();
        body.Length.Should().Be(size);
        context.Response.Headers["Vary"].ToString().Should().Be("Accept-Encoding", "una caché pública no puede entregar esta variante a quien pidió br o gzip");
    }

    [Fact]
    public async Task NotModified_SendsVary_CA_010_04()
    {
        var (context, _) = await RunMiddlewareAsync("/api/v1/oficios", null, 0, status: StatusCodes.Status304NotModified);

        context.Response.Headers["Vary"].ToString().Should().Be("Accept-Encoding");
    }

    [Fact]
    public async Task VaryIsNotDuplicated_CA_010_04()
    {
        var (context, _) = await RunMiddlewareAsync("/api/v1/oficios", "br", 2048);

        context.Response.Headers["Vary"].Count.Should().Be(1);
    }

    [Fact]
    public async Task AuthenticationGetsNoVary_CA_010_04()
    {
        var (context, _) = await RunMiddlewareAsync("/api/v1/auth/refresh", "br", 2048);

        context.Response.Headers.ContainsKey("Vary").Should().BeFalse("la autenticación no pasa por el middleware");
    }

    [Fact]
    public async Task ExceptionBeforeOverflow_DiscardsBufferSoTheErrorHandlerCanWrite_CA_010_04()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/oficios";
        context.Request.Headers["Accept-Encoding"] = "br";
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        var middleware = new JsonCompressionMiddleware(async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Body.WriteAsync(new byte[512], CancellationToken.None);
            throw new InvalidOperationException("falla a mitad de la respuesta");
        });

        var act = () => middleware.InvokeAsync(context);

        await act.Should().ThrowAsync<InvalidOperationException>();
        responseBody.Length.Should().Be(0, "lo bufferizado se descarta y la respuesta no empieza");
        context.Response.Body.Should().BeSameAs(responseBody, "el cuerpo real queda restaurado para el manejador de excepciones");
    }

    [Fact]
    public async Task IdentityExceptionBeforeOverflow_AlsoDiscardsBuffer_CA_010_04()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/me";
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        var middleware = new IdentityHosting.JsonCompressionMiddleware(async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Body.WriteAsync(new byte[512], CancellationToken.None);
            throw new InvalidOperationException("falla a mitad de la respuesta");
        });

        var act = () => middleware.InvokeAsync(context);

        await act.Should().ThrowAsync<InvalidOperationException>();
        responseBody.Length.Should().Be(0);
    }
}
