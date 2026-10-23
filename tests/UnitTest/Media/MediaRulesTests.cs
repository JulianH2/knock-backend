using AwesomeAssertions;
using Knock.Api.Modules.Media.Domain;
using Xunit;

namespace Knock.UnitTest.Media;

public class MediaRulesTests
{
    public static TheoryData<byte[], string?> Signatures => new()
    {
        { [0xFF, 0xD8, 0xFF, 0xE0, 0x00], "Jpeg" },
        { [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00], "Png" },
        { "%PDF-1.7"u8.ToArray(), null },
        { "<svg onload=\"x\">"u8.ToArray(), null },
        { [0x47, 0x49, 0x46, 0x38, 0x39, 0x61], null },
        { [0xFF, 0xD8], null },
        { [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A], null },
        { [], null },
    };

    [Theory]
    [MemberData(nameof(Signatures))]
    public void DetectFormat_UsesMagicBytesOnly_TC_TT_API_05_03(byte[] content, string? expected)
    {
        var detected = MediaRules.DetectFormat(content)?.ToString();

        detected.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(8L * 1024 * 1024, true)]
    [InlineData((8L * 1024 * 1024) + 1, false)]
    [InlineData(9L * 1024 * 1024, false)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void IsAllowedSize_AcceptsUpToEightMegabytes_TC_TT_API_05_03(long sizeBytes, bool expected)
    {
        MediaRules.IsAllowedSize(sizeBytes).Should().Be(expected);
    }

    [Theory]
    [InlineData("image/jpeg", true)]
    [InlineData("image/png", true)]
    [InlineData("application/pdf", false)]
    [InlineData("image/svg+xml", false)]
    [InlineData("image/gif", false)]
    [InlineData("IMAGE/JPEG", false)]
    public void IsAllowedContentType_OnlyJpegAndPng_TC_TT_API_05_03(string contentType, bool expected)
    {
        MediaRules.IsAllowedContentType(contentType).Should().Be(expected);
    }

    [Theory]
    [InlineData(4000, 3000, true)]
    [InlineData(12_000, 2000, true)]
    [InlineData(6000, 4000, true)]
    [InlineData(6000, 4001, false)]
    [InlineData(12_000, 3000, false)]
    [InlineData(12_001, 10, false)]
    [InlineData(10, 12_001, false)]
    [InlineData(8000, 6000, false)]
    [InlineData(0, 10, false)]
    public void AreAllowedDimensions_BoundsSidesAndPixelsBeforeDecoding_TC_TT_API_05_03(int width, int height, bool expected)
    {
        MediaRules.AreAllowedDimensions(width, height).Should().Be(expected);
    }

    [Fact]
    public void ReadPermission_OnlyOwnerOfReadyFile_TC_TT_API_05_01()
    {
        var now = new DateTimeOffset(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
        var pending = MediaFile.CreatePending("owner-1", MediaUsage.Profile, "image/jpeg", 1024, now);
        var ready = pending.MarkReady(ImageFormat.Jpeg, 800, 600, now);

        pending.IsReadyFor("owner-1").Should().BeFalse("un archivo pendiente no se lee");
        ready.IsReadyFor("owner-1").Should().BeTrue();
        ready.IsReadyFor("owner-2").Should().BeFalse("solo el dueño lee por la API");
        ready.OriginalBlob.Should().Be($"{ready.Id:N}/original.jpg", "el nombre lo genera el servidor");
        ready.Id.Version.Should().Be(7);

        var reject = () => ready.MarkRejected(now);
        reject.Should().Throw<InvalidOperationException>("un archivo listo ya no cambia de estado");
    }
}
