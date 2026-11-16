extern alias MarketApi;

using System.Text;
using AwesomeAssertions;
using Azure.Storage.Blobs;
using Knock.Contracts.Moderation;
using Knock.IntegrationTest.Support;
using MarketApi::Knock.Market.Api.Modules.Media.Data;
using MarketApi::Knock.Market.Api.Modules.Media.Domain;
using MarketApi::Knock.Market.Api.Modules.Media.Services;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace Knock.IntegrationTest.Media;

/// <summary>
/// OI-2: confirmation flow against real Azurite (quarantine → process → moderate → final container → delete quarantine),
/// with the real <see cref="AzureMediaBlobStore"/>, <see cref="BlobMediaFileStore"/> and SkiaSharp processor.
/// The client upload to quarantine is simulated directly (SAS links are OI-3).
/// </summary>
public class MediaConfirmationBlobTests
{
    private const string _ownerId = "owner-1";

    [Fact]
    public async Task DevelopmentHost_WithoutModerator_Starts_CA_013_05()
    {
        using var factory = new MarketApiFactory(new Dictionary<string, string?>
        {
            ["ConnectionStrings:KnockMongo"] = "mongodb://127.0.0.1:27017",
            ["Mongo:Databases:Catalog"] = "market_test",
            ["Mongo:Databases:Requests"] = "market_test",
        });
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative));
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmedPhotoMovesCleanedToFinalAndClearsQuarantine_OI2_02()
    {
        await using var azurite = await AzuriteFixture.StartAsync();
        using var processor = CreateProcessor();
        var (service, options, store) = CreateService(azurite, processor, ModerationOutcome.Allowed);
        byte[] photo = Jpeg(800, 400);

        var ticket = await service.CreateUploadAsync(_ownerId, "perfil", "image/jpeg", photo.Length, CancellationToken.None);
        ticket.Status.Should().Be(MediaOutcomeStatus.Success);
        ticket.Value.Should().NotBeNull();
        var id = ticket.Value.Id;
        var key = (await store.FindAsync(id, CancellationToken.None))!.File.QuarantineBlob;
        await UploadQuarantineAsync(azurite, options, key, photo);

        var confirm = await service.ConfirmAsync(_ownerId, id, CancellationToken.None);

        confirm.Status.Should().Be(MediaOutcomeStatus.Success);
        confirm.Value.Should().NotBeNull();
        confirm.Value.State.Should().Be("listo");
        var files = await azurite.ListBlobNamesAsync(options.FilesContainer);
        files.Should().HaveCount(2, "the cleaned original and the reduced copy reach the final container");
        files.Should().OnlyContain(name => name.StartsWith(key, StringComparison.Ordinal));
        (await azurite.ListBlobNamesAsync(options.QuarantineContainer)).Should().BeEmpty("the quarantine copy is deleted after confirmation");

        var originalBlob = azurite.Container(options.FilesContainer).GetBlobClient($"{key}/original.jpg");
        (await originalBlob.GetPropertiesAsync()).Value.ContentType.Should().Be("image/jpeg");
        var original = await originalBlob.DownloadContentAsync();
        Encoding.ASCII.GetString(original.Value.Content.ToArray()).Should().NotContain("Exif", "re-encoded bytes carry no EXIF/GPS metadata");
    }

    [Fact]
    public async Task PdfNamedAsJpgIsRejectedAndQuarantineIsEmpty_OI2_04()
    {
        await using var azurite = await AzuriteFixture.StartAsync();
        using var processor = CreateProcessor();
        var (service, options, store) = CreateService(azurite, processor, ModerationOutcome.Allowed);
        byte[] pdf = "%PDF-1.7\n% malicious payload disguised as a .jpg\n"u8.ToArray();

        var ticket = await service.CreateUploadAsync(_ownerId, "perfil", "image/jpeg", pdf.Length, CancellationToken.None);
        ticket.Value.Should().NotBeNull();
        var id = ticket.Value.Id;
        var key = (await store.FindAsync(id, CancellationToken.None))!.File.QuarantineBlob;
        await UploadQuarantineAsync(azurite, options, key, pdf);

        var confirm = await service.ConfirmAsync(_ownerId, id, CancellationToken.None);

        confirm.Status.Should().Be(MediaOutcomeStatus.Unprocessable);
        confirm.Detail.Should().Be(MediaRejection.InvalidFile);
        (await azurite.ListBlobNamesAsync(options.FilesContainer)).Should().BeEmpty("nothing reaches the final container");
        (await azurite.ListBlobNamesAsync(options.QuarantineContainer)).Should().BeEmpty("the rejected quarantine blob is deleted");
    }

    private static async Task UploadQuarantineAsync(AzuriteFixture azurite, MediaOptions options, string key, byte[] content)
    {
        var container = azurite.Container(options.QuarantineContainer);
        await container.CreateIfNotExistsAsync();
        await container.GetBlobClient(key).UploadAsync(BinaryData.FromBytes(content));
    }

    private static (MediaService Service, MediaOptions Options, IMediaFileStore Store) CreateService(
        AzuriteFixture azurite, ImageProcessor processor, ModerationOutcome outcome)
    {
        var options = new MediaOptions { ReducedMaxSide = 512, MaxConcurrentProcessing = 2 };
        var configured = Options.Create(options);
        var blobs = new BlobServiceClient(azurite.ConnectionString);
        var store = new BlobMediaFileStore(blobs, configured);
        var service = new MediaService(
            store,
            new AzureMediaBlobStore(blobs, configured),
            processor,
            new FakeModerator(outcome),
            TimeProvider.System);
        return (service, options, store);
    }

    private static ImageProcessor CreateProcessor() =>
        new(Options.Create(new MediaOptions { ReducedMaxSide = 512, MaxConcurrentProcessing = 2 }));

    private static byte[] Jpeg(int width, int height)
    {
        using var bitmap = new SKBitmap(new SKImageInfo(width, height));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.CornflowerBlue);
        using var data = bitmap.Encode(SKEncodedImageFormat.Jpeg, 90);
        return data.ToArray();
    }

    /// <summary>Fake moderator with a fixed outcome; never calls a real API (spec 013 §8).</summary>
    private sealed class FakeModerator(ModerationOutcome outcome) : IContentModerator
    {
        public Task<ModerationResult> ReviewTextAsync(string text, CancellationToken cancellationToken) =>
            Task.FromResult(new ModerationResult(outcome));

        public Task<ModerationResult> ReviewImageAsync(ReadOnlyMemory<byte> image, CancellationToken cancellationToken) =>
            Task.FromResult(new ModerationResult(outcome));
    }
}
