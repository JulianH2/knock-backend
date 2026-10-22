using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using SkiaSharp;
using Xunit;

namespace Knock.IntegrationTest.Media;

public class PrivateFilesTests
{
    private const int _photoWidth = 1600;
    private const int _photoHeight = 800;

    [Fact]
    public async Task ExpiredSasLinkIsRejectedByBlobStorage_TC_TT_API_05_01()
    {
        await using var azurite = await AzuriteFixture.StartAsync();
        var clock = new MutableTimeProvider();
        using var factory = MediaHttp.CreateFactory(azurite, clock);
        using var client = factory.CreateClient();
        var token = MediaHttp.IssueAccessToken(factory);
        var photo = TestImages.JpegWithExifAndGps(_photoWidth, _photoHeight);

        // Control: con el reloj real el SAS de escritura y el de lectura funcionan.
        var (readyId, uploadUrl) = await MediaHttp.StartUploadAsync(client, token, photo.Length);
        (await MediaHttp.PutBlobAsync(uploadUrl, photo).StatusAsync()).Should().Be(HttpStatusCode.Created);
        (await MediaHttp.ConfirmAsync(client, token, readyId).StatusAsync()).Should().Be(HttpStatusCode.OK);
        var readUrl = await MediaHttp.ReadLinkAsync(client, token, readyId, "original");
        (await MediaHttp.GetBlobAsync(readUrl).StatusAsync()).Should().Be(HttpStatusCode.OK);

        // Emitidos 6 min «antes»: su vencimiento de 5 min ya pasó.
        clock.Offset = TimeSpan.FromMinutes(-6);
        var (expiredId, expiredUploadUrl) = await MediaHttp.StartUploadAsync(client, token, photo.Length);
        var expiredReadUrl = await MediaHttp.ReadLinkAsync(client, token, readyId, "original");

        using var write = await MediaHttp.PutBlobAsync(expiredUploadUrl, photo);
        using var read = await MediaHttp.GetBlobAsync(expiredReadUrl);

        write.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        read.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        TestImages.ContainsBytes(await read.Content.ReadAsByteArrayAsync(), photo[..64]).Should().BeFalse("no se leyó el blob");
        (await azurite.ListBlobNamesAsync(MediaHttp.QuarantineContainer)).Should().NotContain(expiredId.ToString("N"), "no se escribió el blob");
    }

    [Fact]
    public async Task ConfirmedPhotoHasNoExifIncludingGps_TC_TT_API_05_02()
    {
        await using var azurite = await AzuriteFixture.StartAsync();
        using var factory = MediaHttp.CreateFactory(azurite);
        using var client = factory.CreateClient();
        var token = MediaHttp.IssueAccessToken(factory);
        var photo = TestImages.JpegWithExifAndGps(_photoWidth, _photoHeight);
        AssertSourceCarriesExif(photo);

        var (id, uploadUrl) = await MediaHttp.StartUploadAsync(client, token, photo.Length);
        (await MediaHttp.PutBlobAsync(uploadUrl, photo).StatusAsync()).Should().Be(HttpStatusCode.Created);
        using var confirm = await MediaHttp.ConfirmAsync(client, token, id);

        confirm.StatusCode.Should().Be(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await confirm.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("estado").GetString().Should().Be("listo");
        body.RootElement.GetProperty("ancho").GetInt32().Should().Be(_photoHeight, "la orientación 6 gira la foto: el ancho pasa a ser el alto original");
        body.RootElement.GetProperty("alto").GetInt32().Should().Be(_photoWidth);

        var original = await DownloadAsync(client, token, id, "original");
        var reduced = await DownloadAsync(client, token, id, "reducida");
        AssertCleanAndUpright(original, expectedWidth: _photoHeight, expectedHeight: _photoWidth);
        AssertCleanAndUpright(reduced, expectedWidth: 512, expectedHeight: 1024);
        (await azurite.ListBlobNamesAsync(MediaHttp.QuarantineContainer)).Should().BeEmpty("el original de cuarentena se borra al confirmar");
    }

    [Theory]
    [InlineData("tamano-declarado-9mb")]
    [InlineData("tamano-real-9mb")]
    [InlineData("pdf-renombrado-jpg")]
    public async Task OversizedOrNonImageFileIsRejectedWith422_TC_TT_API_05_03(string scenario)
    {
        await using var azurite = await AzuriteFixture.StartAsync();
        using var factory = MediaHttp.CreateFactory(azurite);
        using var client = factory.CreateClient();
        var token = MediaHttp.IssueAccessToken(factory);
        const int nineMegabytes = 9 * 1024 * 1024;

        using var response = scenario switch
        {
            "tamano-declarado-9mb" => await MediaHttp.CreateUploadAsync(client, token, MediaHttp.JpegUpload(nineMegabytes)),
            "tamano-real-9mb" => await UploadAndConfirmAsync(client, token, TestImages.JpegSignaturePadding(nineMegabytes)),
            _ => await UploadAndConfirmAsync(client, token, TestImages.PdfNamedAsJpg()),
        };

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        (await azurite.ListBlobNamesAsync(MediaHttp.FilesContainer)).Should().BeEmpty("nada llega al contenedor final");
        (await azurite.ListBlobNamesAsync(MediaHttp.QuarantineContainer)).Should().BeEmpty("el blob rechazado se borra");
    }

    /// <summary>Precondición: la foto de prueba sí trae EXIF con GPS y orientación (si no, la prueba sería vacía).</summary>
    private static void AssertSourceCarriesExif(byte[] photo)
    {
        TestImages.JpegMetadataMarkers(photo).Should().Contain(0xE1);
        TestImages.ContainsAscii(photo, TestImages.ExifModel).Should().BeTrue();
        TestImages.ReadOrigin(photo).Should().Be(SKEncodedOrigin.RightTop);
    }

    /// <summary>Sin APP1 (EXIF/XMP) ni APP13 (IPTC), sin rastros de marca, modelo o fecha, y con los píxeles ya girados.</summary>
    private static void AssertCleanAndUpright(byte[] jpeg, int expectedWidth, int expectedHeight)
    {
        TestImages.JpegMetadataMarkers(jpeg).Should().NotContain([0xE1, 0xED]);
        foreach (var trace in new[] { "Exif", TestImages.ExifMake, TestImages.ExifModel, TestImages.ExifDate })
        {
            TestImages.ContainsAscii(jpeg, trace).Should().BeFalse($"no debe quedar «{trace}»");
        }

        TestImages.ReadOrigin(jpeg).Should().Be(SKEncodedOrigin.TopLeft);
        using var bitmap = SKBitmap.Decode(jpeg);
        bitmap.Width.Should().Be(expectedWidth);
        bitmap.Height.Should().Be(expectedHeight);

        // La mitad izquierda roja queda arriba al girar 90° a la derecha; la azul, abajo.
        var top = bitmap.GetPixel(expectedWidth / 2, expectedHeight / 4);
        var bottom = bitmap.GetPixel(expectedWidth / 2, expectedHeight * 3 / 4);
        top.Red.Should().BeGreaterThan(200);
        top.Blue.Should().BeLessThan(60);
        bottom.Blue.Should().BeGreaterThan(200);
        bottom.Red.Should().BeLessThan(60);
    }

    private static async Task<byte[]> DownloadAsync(HttpClient client, string token, Guid id, string version)
    {
        using var response = await MediaHttp.GetBlobAsync(await MediaHttp.ReadLinkAsync(client, token, id, version));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await response.Content.ReadAsByteArrayAsync();
    }

    /// <summary>Declara un tamaño permitido, sube <paramref name="content"/> con el SAS y confirma.</summary>
    private static async Task<HttpResponseMessage> UploadAndConfirmAsync(HttpClient client, string token, byte[] content)
    {
        var (id, uploadUrl) = await MediaHttp.StartUploadAsync(client, token, declaredBytes: 1024);
        (await MediaHttp.PutBlobAsync(uploadUrl, content).StatusAsync()).Should().Be(HttpStatusCode.Created);
        return await MediaHttp.ConfirmAsync(client, token, id);
    }
}
