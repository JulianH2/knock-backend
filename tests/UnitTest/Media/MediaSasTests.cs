using System.Security.Cryptography;
using System.Web;
using AwesomeAssertions;
using Azure.Storage;
using Azure.Storage.Blobs;
using Knock.Api.Modules.Media.Data;
using Knock.Api.Modules.Media.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace Knock.UnitTest.Media;

public class MediaSasTests
{
    private static readonly DateTimeOffset _now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void UploadAndReadLinks_ExpireInFiveMinutesWithMinimalPermissions_TC_TT_API_05_01()
    {
        var storage = CreateStorage();

        var upload = storage.CreateUploadLink("blob-subida");
        var read = storage.CreateReadLink("blob/original.jpg");

        upload.ExpiresAt.Should().Be(_now.AddMinutes(5));
        read.ExpiresAt.Should().Be(_now.AddMinutes(5));
        var uploadQuery = HttpUtility.ParseQueryString(upload.Url.Query);
        var readQuery = HttpUtility.ParseQueryString(read.Url.Query);
        uploadQuery["sp"].Should().Be("cw", "escritura: solo create y write");
        readQuery["sp"].Should().Be("r", "lectura: solo read");
        uploadQuery["sr"].Should().Be("b", "SAS de un solo blob, nunca de contenedor ni de cuenta");
        readQuery["sr"].Should().Be("b");
        uploadQuery["se"].Should().Be("2026-10-21T12:05:00Z");
        upload.Url.AbsolutePath.Should().EndWith("/cuarentena/blob-subida");
        read.Url.AbsolutePath.Should().EndWith("/archivos/blob/original.jpg");
    }

    /// <summary>Cliente sin red: cuenta y clave sintéticas aleatorias (solo firman; nunca se conecta).</summary>
    private static MediaBlobStorage CreateStorage()
    {
        var credential = new StorageSharedKeyCredential("knockunittest", Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)));
        var client = new BlobServiceClient(new Uri("https://knockunittest.blob.core.windows.net"), credential);
        return new MediaBlobStorage(client, Options.Create(new MediaOptions()), new FixedTimeProvider(_now));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
