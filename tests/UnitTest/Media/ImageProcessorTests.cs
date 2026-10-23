using AwesomeAssertions;
using Knock.Api.Modules.Media.Data;
using Knock.Api.Modules.Media.Domain;
using Knock.Api.Modules.Media.Services;
using Microsoft.Extensions.Options;
using SkiaSharp;
using Xunit;

namespace Knock.UnitTest.Media;

public class ImageProcessorTests
{
    private static readonly TimeSpan _patience = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task RunLimited_NeverRunsMoreThanTheConfiguredSlots_TC_TT_API_05_03()
    {
        using var processor = CreateProcessor(maxConcurrent: 2);
        using var release = new ManualResetEventSlim();
        var running = 0;
        var peak = 0;

        int Work()
        {
            var now = Interlocked.Increment(ref running);
            InterlockedMax(ref peak, now);
            release.Wait(_patience);
            Interlocked.Decrement(ref running);
            return now;
        }

        var calls = Enumerable.Range(0, 6)
            .Select(_ => Task.Run(() => processor.RunLimitedAsync(Work, CancellationToken.None)))
            .ToArray();
        await WaitUntilAsync(() => Volatile.Read(ref running) == 2);
        await Task.Delay(TimeSpan.FromMilliseconds(300)); // margen para que las demás entren si el límite no existiera

        Volatile.Read(ref running).Should().Be(2, "las otras 4 esperan turno");
        release.Set();
        var results = await Task.WhenAll(calls);

        peak.Should().Be(2);
        results.Should().HaveCount(6, "todas terminan cuando se liberan los turnos");
    }

    [Fact]
    public async Task RunLimited_CancelledWhileWaiting_ReleasesNoSlot_TC_TT_API_05_03()
    {
        using var processor = CreateProcessor(maxConcurrent: 1);
        using var release = new ManualResetEventSlim();
        using var entered = new ManualResetEventSlim();
        using var cancel = new CancellationTokenSource();
        var busy = Task.Run(() => processor.RunLimitedAsync(
            () =>
            {
                entered.Set();
                return release.Wait(_patience);
            },
            CancellationToken.None));
        await WaitUntilAsync(() => entered.IsSet);

        var waiting = processor.RunLimitedAsync(() => true, cancel.Token);
        await cancel.CancelAsync();

        await FluentActions.Awaiting(() => waiting).Should().ThrowAsync<OperationCanceledException>();
        release.Set();
        await busy;
        (await processor.RunLimitedAsync(() => 1, CancellationToken.None)).Should().Be(1, "el turno ocupado se devolvió");
    }

    [Fact]
    public async Task Process_DimensionsOverTheLimit_UseTheirOwnDetail_TC_TT_API_05_03()
    {
        using var processor = CreateProcessor(maxConcurrent: 1);

        var tooWide = await processor.ProcessAsync(Png(MediaRules.MaxSidePixels + 1, 1), CancellationToken.None);
        var notAnImage = await processor.ProcessAsync("%PDF-1.7"u8.ToArray(), CancellationToken.None);
        var valid = await processor.ProcessAsync(Png(40, 20), CancellationToken.None);

        tooWide.Status.Should().Be(MediaOutcomeStatus.Unprocessable);
        tooWide.Detail.Should().Be(MediaRejection.DimensionsTooLarge);
        notAnImage.Detail.Should().Be(MediaRejection.InvalidFile);
        MediaRejection.DimensionsTooLarge.Should().NotBe(MediaRejection.InvalidFile).And.Contain("24 megapixels");
        valid.Status.Should().Be(MediaOutcomeStatus.Success);
        valid.Value!.Width.Should().Be(40);
        valid.Value.Height.Should().Be(20);
    }

    private static ImageProcessor CreateProcessor(int maxConcurrent) =>
        new(Options.Create(new MediaOptions { MaxConcurrentProcessing = maxConcurrent }));

    private static byte[] Png(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.Gray);
        using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return png.ToArray();
    }

    private static void InterlockedMax(ref int target, int value)
    {
        var seen = Volatile.Read(ref target);
        while (value > seen)
        {
            var prior = Interlocked.CompareExchange(ref target, value, seen);
            if (prior == seen)
            {
                return;
            }

            seen = prior;
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + _patience;
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("The condition was not reached in time.");
            }

            await Task.Delay(10);
        }
    }
}
