using System.Net;
using System.Web;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.SecurityTest.Media;

public class PrivateFilesSecurityTests
{
    [Fact]
    public async Task FileEndpoints_WithoutJwt_Return401_TC_TT_API_05_03()
    {
        using var factory = new KnockApiFactory();
        using var client = factory.CreateClient();
        var id = Guid.CreateVersion7();

        using var upload = await MediaHttp.CreateUploadAsync(client, accessToken: null, MediaHttp.JpegUpload(1024));
        using var confirm = await MediaHttp.ConfirmAsync(client, accessToken: null, id);
        using var read = await MediaHttp.ReadAsync(client, accessToken: null, id, "original");

        foreach (var response in new[] { upload, confirm, read })
        {
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        }
    }

    [Fact]
    public async Task OversizedJsonBody_Returns413_TC_TT_API_05_03()
    {
        using var factory = new KnockApiFactory();
        using var client = factory.CreateClient();
        var token = MediaHttp.IssueAccessToken(factory);

        using var response = await MediaHttp.CreateUploadAsync(
            client, token, new { uso = "perfil", contentType = "image/jpeg", tamanoBytes = 1024, relleno = new string('x', 4096) });

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task SasLinks_AllowOnlyTheirOwnOperation_TC_TT_API_05_01()
    {
        await using var azurite = await AzuriteFixture.StartAsync();
        using var factory = MediaHttp.CreateFactory(azurite);
        using var client = factory.CreateClient();
        var token = MediaHttp.IssueAccessToken(factory);
        var photo = TestImages.JpegWithExifAndGps(64, 32);
        var (id, uploadUrl) = await MediaHttp.StartUploadAsync(client, token, photo.Length);

        (await MediaHttp.GetBlobAsync(uploadUrl).StatusAsync()).Should().Be(HttpStatusCode.Forbidden, "el SAS de escritura no lee");
        (await MediaHttp.PutBlobAsync(uploadUrl, photo).StatusAsync()).Should().Be(HttpStatusCode.Created);
        (await MediaHttp.ConfirmAsync(client, token, id).StatusAsync()).Should().Be(HttpStatusCode.OK);
        var readUrl = await MediaHttp.ReadLinkAsync(client, token, id, "reducida");

        (await MediaHttp.PutBlobAsync(readUrl, [1, 2, 3]).StatusAsync()).Should().Be(HttpStatusCode.Forbidden, "el SAS de lectura no escribe");
        (await MediaHttp.GetBlobAsync(readUrl).StatusAsync()).Should().Be(HttpStatusCode.OK);
        HttpUtility.ParseQueryString(readUrl.Query)["sr"].Should().Be("b", "nunca SAS de contenedor ni de cuenta");
        (await azurite.Container(MediaHttp.FilesContainer).GetAccessPolicyAsync()).Value.BlobPublicAccess
            .Should().Be(Azure.Storage.Blobs.Models.PublicAccessType.None, "el contenedor es privado");
    }

    [Fact]
    public async Task ResponsesWithSasLinks_AreNotStored_TC_TT_API_05_01()
    {
        await using var azurite = await AzuriteFixture.StartAsync();
        using var factory = MediaHttp.CreateFactory(azurite);
        using var client = factory.CreateClient();
        var token = MediaHttp.IssueAccessToken(factory);
        var photo = TestImages.JpegWithExifAndGps(64, 32);

        using var upload = await MediaHttp.CreateUploadAsync(client, token, MediaHttp.JpegUpload(photo.Length));
        upload.StatusCode.Should().Be(HttpStatusCode.Created);
        var (id, uploadUrl) = await MediaHttp.StartUploadAsync(client, token, photo.Length);
        (await MediaHttp.PutBlobAsync(uploadUrl, photo).StatusAsync()).Should().Be(HttpStatusCode.Created);
        (await MediaHttp.ConfirmAsync(client, token, id).StatusAsync()).Should().Be(HttpStatusCode.OK);
        using var read = await MediaHttp.ReadAsync(client, token, id, "original");
        read.StatusCode.Should().Be(HttpStatusCode.OK);

        foreach (var response in new[] { upload, read })
        {
            response.Headers.CacheControl?.NoStore.Should().BeTrue("una respuesta con SAS no se guarda en cachés ni proxies");
        }
    }

    [Fact]
    public async Task OtherUser_CannotConfirmOrReadAFile_Returns404_TC_TT_API_05_01()
    {
        await using var azurite = await AzuriteFixture.StartAsync();
        using var factory = MediaHttp.CreateFactory(azurite);
        using var client = factory.CreateClient();
        var owner = MediaHttp.IssueAccessToken(factory);
        var stranger = MediaHttp.IssueAccessToken(factory);
        var photo = TestImages.JpegWithExifAndGps(64, 32);
        var (id, uploadUrl) = await MediaHttp.StartUploadAsync(client, owner, photo.Length);
        (await MediaHttp.PutBlobAsync(uploadUrl, photo).StatusAsync()).Should().Be(HttpStatusCode.Created);

        (await MediaHttp.ConfirmAsync(client, stranger, id).StatusAsync()).Should().Be(HttpStatusCode.NotFound);
        (await MediaHttp.ConfirmAsync(client, owner, id).StatusAsync()).Should().Be(HttpStatusCode.OK);
        (await MediaHttp.ReadAsync(client, stranger, id, "original").StatusAsync()).Should().Be(HttpStatusCode.NotFound);
        (await MediaHttp.ReadAsync(client, stranger, Guid.CreateVersion7(), "original").StatusAsync())
            .Should().Be(HttpStatusCode.NotFound, "un id ajeno y uno inexistente responden igual");
        (await MediaHttp.ConfirmAsync(client, owner, id).StatusAsync()).Should().Be(HttpStatusCode.Conflict, "ya confirmado");
    }

    [Fact]
    public async Task SasLinks_AreNeverWrittenToLogs_TC_TT_API_05_01()
    {
        await using var azurite = await AzuriteFixture.StartAsync();
        var photo = TestImages.JpegWithExifAndGps(64, 32);
        var signatures = new List<string>();

        var logs = await ConsoleCapture.RunAsync(async () =>
        {
            using var factory = MediaHttp.CreateFactory(azurite);
            using var client = factory.CreateClient();
            var token = MediaHttp.IssueAccessToken(factory);
            var (id, uploadUrl) = await MediaHttp.StartUploadAsync(client, token, photo.Length);
            (await MediaHttp.PutBlobAsync(uploadUrl, photo).StatusAsync()).Should().Be(HttpStatusCode.Created);
            (await MediaHttp.ConfirmAsync(client, token, id).StatusAsync()).Should().Be(HttpStatusCode.OK);
            var readUrl = await MediaHttp.ReadLinkAsync(client, token, id, "original");
            using var invalid = await MediaHttp.CreateUploadAsync(client, token, new { uso = "perfil", contentType = "application/pdf", tamanoBytes = 1 });
            invalid.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            signatures.Add(HttpUtility.ParseQueryString(uploadUrl.Query)["sig"]!);
            signatures.Add(HttpUtility.ParseQueryString(readUrl.Query)["sig"]!);
        });

        logs.Should().Contain("/api/v1/archivos", "la captura debe incluir las peticiones (si no, la prueba sería vacía)");
        logs.Should().NotContain("sig=").And.NotContain("se=20");
        foreach (var signature in signatures)
        {
            signature.Should().NotBeNullOrEmpty();
            logs.Should().NotContain(signature).And.NotContain(Uri.EscapeDataString(signature));
        }
    }
}
