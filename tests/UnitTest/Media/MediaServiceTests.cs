using AwesomeAssertions;
using Knock.Contracts.Moderation;
using Knock.Market.Api.Modules.Media;
using Knock.Market.Api.Modules.Media.Data;
using Knock.Market.Api.Modules.Media.Domain;
using Knock.Market.Api.Modules.Media.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SkiaSharp;
using Xunit;

namespace Knock.UnitTest.Media;

/// <summary>OI-2: confirmation flow with in-memory stores and a fake moderator (CA-04 / CA-05 fail closed).</summary>
public sealed class MediaServiceTests
{
    private const string _ownerId = "owner-1";

    [Fact]
    public async Task Confirm_DevelopmentDefaultModerator_LeavesUploadPending_CA_013_05()
    {
        var registrations = new ServiceCollection();
        registrations.AddMediaModule(isDevelopment: true);
        using var provider = registrations.BuildServiceProvider();
        using var processor = CreateProcessor();
        var files = new InMemoryMediaFileStore();
        var blobs = new CountingBlobStore(new InMemoryMediaBlobStore());
        var service = new MediaService(files, blobs, processor, provider.GetRequiredService<IContentModerator>(), TimeProvider.System);
        var ticket = await service.CreateUploadAsync(_ownerId, "perfil", "image/jpeg", 1000, CancellationToken.None);
        var id = ticket.Value!.Id;
        var key = (await files.FindAsync(id, CancellationToken.None))!.File.QuarantineBlob;
        blobs.UploadRaw(key, Jpeg(400, 300));

        (await service.ConfirmAsync(_ownerId, id, CancellationToken.None)).Status.Should().Be(MediaOutcomeStatus.Unavailable);
        (await files.FindAsync(id, CancellationToken.None))!.File.State.Should().Be(MediaFileState.Pending);
        blobs.ContainsQuarantine(key).Should().BeTrue();
        blobs.CountFinalBlobs().Should().Be(0);
    }

    [Fact]
    public async Task Confirm_AllowedContent_PublishesReadyAndClearsQuarantine_OI2_02()
    {
        using var processor = CreateProcessor();
        var harness = CreateService(processor, ModerationOutcome.Allowed);

        var ticket = await harness.Service.CreateUploadAsync(_ownerId, "perfil", "image/jpeg", 1000, CancellationToken.None);
        ticket.Status.Should().Be(MediaOutcomeStatus.Success);
        ticket.Value.Should().NotBeNull();
        var ticketId = ticket.Value.Id;
        string quarantine = (await harness.Files.FindAsync(ticketId, CancellationToken.None))!.File.QuarantineBlob;
        harness.Blobs.UploadRaw(quarantine, Jpeg(400, 300));

        var confirm = await harness.Service.ConfirmAsync(_ownerId, ticketId, CancellationToken.None);

        confirm.Status.Should().Be(MediaOutcomeStatus.Success);
        confirm.Value.Should().NotBeNull();
        confirm.Value.State.Should().Be("listo");
        harness.Blobs.ContainsQuarantine(quarantine).Should().BeFalse("quarantine is removed after confirmation");
        (await harness.Service.IsReadyAndOwnedByAsync(ticketId, _ownerId, CancellationToken.None)).Should().BeTrue();
        (await harness.Service.IsReadyAndOwnedByAsync(ticketId, "other-owner", CancellationToken.None)).Should().BeFalse();
        (await harness.Files.FindAsync(ticketId, CancellationToken.None))!.File.State.Should().Be(MediaFileState.Ready);
    }

    [Fact]
    public async Task Confirm_RejectedContent_DoesNotPublish_CA_013_04()
    {
        using var processor = CreateProcessor();
        var harness = CreateService(processor, ModerationOutcome.Rejected);

        var ticket = await harness.Service.CreateUploadAsync(_ownerId, "perfil", "image/jpeg", 1000, CancellationToken.None);
        ticket.Value.Should().NotBeNull();
        var ticketId = ticket.Value.Id;
        string quarantine = (await harness.Files.FindAsync(ticketId, CancellationToken.None))!.File.QuarantineBlob;
        harness.Blobs.UploadRaw(quarantine, Jpeg(400, 300));

        var confirm = await harness.Service.ConfirmAsync(_ownerId, ticketId, CancellationToken.None);

        confirm.Status.Should().Be(MediaOutcomeStatus.Unprocessable);
        confirm.Detail.Should().Be(MediaRejection.ModerationRejected);
        harness.Blobs.ContainsQuarantine(quarantine).Should().BeFalse("rejected quarantine is removed");
        harness.Blobs.CountFinalBlobs().Should().Be(0, "rejected content never reaches the final container");
        (await harness.Files.FindAsync(ticketId, CancellationToken.None))!.File.State.Should().Be(MediaFileState.Rejected);
    }

    [Fact]
    public async Task Confirm_ModerationDown_DoesNotPublish_CA_013_05()
    {
        using var processor = CreateProcessor();
        var harness = CreateService(processor, ModerationOutcome.Unavailable);

        var ticket = await harness.Service.CreateUploadAsync(_ownerId, "perfil", "image/jpeg", 1000, CancellationToken.None);
        ticket.Value.Should().NotBeNull();
        var ticketId = ticket.Value.Id;
        string quarantine = (await harness.Files.FindAsync(ticketId, CancellationToken.None))!.File.QuarantineBlob;
        harness.Blobs.UploadRaw(quarantine, Jpeg(400, 300));

        var confirm = await harness.Service.ConfirmAsync(_ownerId, ticketId, CancellationToken.None);

        confirm.Status.Should().Be(MediaOutcomeStatus.Unavailable);
        harness.Blobs.ContainsQuarantine(quarantine).Should().BeTrue("a retry needs the uploaded image");
        harness.Blobs.CountFinalBlobs().Should().Be(0, "nothing is published when moderation is down");
        (await harness.Files.FindAsync(ticketId, CancellationToken.None))!.File.State.Should().Be(MediaFileState.Pending);
    }

    [Fact]
    public async Task Confirm_ModeratorTimeout_CanRetrySuccessfully_CA_013_05()
    {
        using var processor = CreateProcessor();
        var files = new InMemoryMediaFileStore();
        var blobs = new CountingBlobStore(new InMemoryMediaBlobStore());
        var moderator = new TimeoutThenAllowModerator();
        var service = new MediaService(files, blobs, processor, moderator, TimeProvider.System);
        var ticket = await service.CreateUploadAsync(_ownerId, "perfil", "image/jpeg", 1000, CancellationToken.None);
        var id = ticket.Value!.Id;
        var key = (await files.FindAsync(id, CancellationToken.None))!.File.QuarantineBlob;
        blobs.UploadRaw(key, Jpeg(400, 300));
        (await service.ConfirmAsync(_ownerId, id, CancellationToken.None)).Status.Should().Be(MediaOutcomeStatus.Unavailable);
        blobs.ContainsQuarantine(key).Should().BeTrue();
        blobs.CountFinalBlobs().Should().Be(0);
        (await files.FindAsync(id, CancellationToken.None))!.File.State.Should().Be(MediaFileState.Pending);
        (await service.ConfirmAsync(_ownerId, id, CancellationToken.None)).Status.Should().Be(MediaOutcomeStatus.Success);
    }

    [Fact]
    public async Task Confirm_UnknownOwner_ReturnsNotFound_OI2_03()
    {
        using var processor = CreateProcessor();
        var harness = CreateService(processor, ModerationOutcome.Allowed);

        var ticket = await harness.Service.CreateUploadAsync(_ownerId, "perfil", "image/jpeg", 1000, CancellationToken.None);
        ticket.Value.Should().NotBeNull();
        var ticketId = ticket.Value.Id;
        harness.Blobs.UploadRaw((await harness.Files.FindAsync(ticketId, CancellationToken.None))!.File.QuarantineBlob, Jpeg(400, 300));

        var confirm = await harness.Service.ConfirmAsync("other-owner", ticketId, CancellationToken.None);

        confirm.Status.Should().Be(MediaOutcomeStatus.NotFound);
    }

    private static ImageProcessor CreateProcessor() =>
        new(Options.Create(new MediaOptions { ReducedMaxSide = 1024, MaxConcurrentProcessing = 2 }));

    private static Harness CreateService(ImageProcessor processor, ModerationOutcome outcome)
    {
        var inner = new InMemoryMediaBlobStore();
        var blobs = new CountingBlobStore(inner);
        var files = new InMemoryMediaFileStore();
        var service = new MediaService(files, blobs, processor, new FakeModerator(outcome), TimeProvider.System);
        return new Harness(service, blobs, files);
    }

    private static byte[] Jpeg(int width, int height)
    {
        using var bitmap = new SKBitmap(new SKImageInfo(width, height));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.CornflowerBlue);
        using var data = bitmap.Encode(SKEncodedImageFormat.Jpeg, 90);
        return data.ToArray();
    }

    /// <summary>Fake moderator with a fixed outcome; never calls a real API.</summary>
    private sealed class FakeModerator(ModerationOutcome outcome) : IContentModerator
    {
        public Task<ModerationResult> ReviewTextAsync(string text, CancellationToken cancellationToken) =>
            Task.FromResult(new ModerationResult(outcome));

        public Task<ModerationResult> ReviewImageAsync(ReadOnlyMemory<byte> image, CancellationToken cancellationToken) =>
            Task.FromResult(new ModerationResult(outcome));
    }

    private sealed class TimeoutThenAllowModerator : IContentModerator
    {
        private bool _first = true;
        public Task<ModerationResult> ReviewTextAsync(string text, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ModerationResult> ReviewImageAsync(ReadOnlyMemory<byte> image, CancellationToken cancellationToken)
        {
            if (_first)
            {
                _first = false;
                throw new TimeoutException();
            }

            return Task.FromResult(new ModerationResult(ModerationOutcome.Allowed));
        }
    }

    /// <summary>Wraps the in-memory blob store and counts final-container uploads (blobs under an id prefix).</summary>
    private sealed class CountingBlobStore(InMemoryMediaBlobStore inner) : IMediaBlobStore
    {
        private int _finalUploads;

        public void UploadRaw(string blob, byte[] content) => inner.UploadRaw(blob, content);

        public bool ContainsQuarantine(string blob) => inner.ContainsQuarantine(blob);

        public int CountFinalBlobs() => _finalUploads;

        public Task EnsureContainersAsync(CancellationToken cancellationToken) => inner.EnsureContainersAsync(cancellationToken);

        public Task<UploadedBlob?> GetUploadedAsync(string quarantineBlob, CancellationToken cancellationToken) =>
            inner.GetUploadedAsync(quarantineBlob, cancellationToken);

        public Task<byte[]?> DownloadQuarantineAsync(string quarantineBlob, UploadedBlob expected, CancellationToken cancellationToken) =>
            inner.DownloadQuarantineAsync(quarantineBlob, expected, cancellationToken);

        public Task UploadFileAsync(string fileBlob, byte[] content, ImageFormat format, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _finalUploads);
            return inner.UploadFileAsync(fileBlob, content, format, cancellationToken);
        }

        public Task DeleteQuarantineAsync(string quarantineBlob, CancellationToken cancellationToken) =>
            inner.DeleteQuarantineAsync(quarantineBlob, cancellationToken);
    }

    private sealed class Harness(MediaService service, CountingBlobStore blobs, IMediaFileStore files)
    {
        public MediaService Service { get; } = service;
        public CountingBlobStore Blobs { get; } = blobs;
        public IMediaFileStore Files { get; } = files;
    }
}
