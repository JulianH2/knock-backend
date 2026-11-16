extern alias MarketApi;

using System.IO.Compression;
using System.Net;
using AwesomeAssertions;
using Knock.SecurityTest.Support;
using MarketApi::Knock.Market.Api.Modules.Catalog.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Knock.SecurityTest.Market;

/// <summary>Spec 010, R-09 y CA-04: GET /api/v1/oficios comprime JSON mayor a 1 KB con Accept-Encoding (br/gzip), conserva ETag/Cache-Control y nunca comprime 304.</summary>
public class MarketCompressionTests
{
    private const string _oficios = "/api/v1/oficios";

    private static HttpRequestMessage Request(string acceptEncoding, string? ifNoneMatch = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_oficios, UriKind.Relative));
        request.Headers.TryAddWithoutValidation("Accept-Encoding", acceptEncoding);
        if (ifNoneMatch is not null)
        {
            request.Headers.TryAddWithoutValidation("If-None-Match", ifNoneMatch);
        }

        return request;
    }

    [Fact]
    public async Task LargeCatalogIsCompressedWithBrotliAndKeepsETag_CA_010_04()
    {
        await using var factory = await SecurityMarketFactory.CreateAsync();
        using var client = factory.CreateClient();
        for (var i = 0; i < 120; i++)
        {
            await factory.AddOficioAsync($"oficio-{i:000}", "Oficio de prueba con nombre bastante largo para superar el umbral", "domicilio", activo: true);
        }

        // La caché de oficios es compartida con otras fábricas en paralelo (bases Redis cíclicas): se invalida tras sembrar para que el GET lea los 120 de esta base y no una lista pequeña cacheada por otra.
        // No se resuelve IConnectionMultiplexer directo: este host arranca la caché deshabilitada (sin cadena en el chequeo de AddCatalogModule) y el multiplexer no está registrado; ICatalogCache es el mismo contrato con o sin Redis.
        await factory.Services.GetRequiredService<ICatalogCache>().RemoveOficiosAsync(CancellationToken.None);

        using var request = Request("gzip, deflate, br");
        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.ETag.Should().NotBeNull();
        response.Headers.CacheControl?.Public.Should().BeTrue();
        response.Content.Headers.GetValues("Content-Encoding").Should().ContainSingle().Which.Should().Be("br");
        response.Headers.GetValues("Vary").Should().Contain("Accept-Encoding");

        var compressed = await response.Content.ReadAsByteArrayAsync();
        using var input = new MemoryStream(compressed);
        await using var brotli = new BrotliStream(input, CompressionMode.Decompress);
        using var reader = new StreamReader(brotli);
        var json = await reader.ReadToEndAsync();
        json.Should().Contain("oficio-119");
        json.Should().StartWith("[");
    }

    [Fact]
    public async Task SmallCatalogIsNotCompressed_CA_010_04()
    {
        var hosts = await SecurityHosts.SharedAsync();

        using var request = Request("gzip, br");
        using var response = await hosts.MarketClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Contains("Content-Encoding").Should().BeFalse();
        (await response.Content.ReadAsStringAsync()).Should().StartWith("[");
    }

    [Fact]
    public async Task NotModifiedIsNeverCompressed_CA_010_04()
    {
        var hosts = await SecurityHosts.SharedAsync();

        using var firstRequest = Request("gzip, br");
        using var first = await hosts.MarketClient.SendAsync(firstRequest);
        var etag = first.Headers.ETag!.ToString();

        using var conditional = Request("gzip, br", etag);
        using var notModified = await hosts.MarketClient.SendAsync(conditional);

        notModified.StatusCode.Should().Be(HttpStatusCode.NotModified);
        notModified.Headers.Contains("Content-Encoding").Should().BeFalse();
        (await notModified.Content.ReadAsStringAsync()).Should().BeEmpty();
    }
}
