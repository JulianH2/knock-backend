using AwesomeAssertions;
using Knock.Market.Api.Modules.Media.Domain;
using Xunit;

namespace Knock.UnitTest.Media;

public class MediaRulesTests
{
    [Theory]
    [InlineData("image/jpeg", true)]
    [InlineData("image/png", true)]
    [InlineData("image/webp", true)]
    [InlineData("image/gif", false)]
    public void IsAllowedContentType_AcceptsJpegPngAndWebP_OI1_01(string contentType, bool expected)
    {
        MediaRules.IsAllowedContentType(contentType).Should().Be(expected);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(5242880, true)]
    [InlineData(0, false)]
    [InlineData(5242881, false)]
    public void IsAllowedSize_AcceptsOneTo5Mb_OI1_01(long sizeBytes, bool expected)
    {
        MediaRules.IsAllowedSize(sizeBytes).Should().Be(expected);
    }

    [Theory]
    [InlineData("/9j/AA==", 0)]
    [InlineData("iVBORw0KGgo=", 1)]
    [InlineData("UklGRgAAAABXRUJQ", 2)]
    [InlineData("UklGRgAAAABBVkkg", null)]
    [InlineData("AAAAAAAAAAA=", null)]
    public void DetectFormat_DetectsBySignature_OI1_01(string base64, int? expected)
    {
        var format = MediaRules.DetectFormat(Convert.FromBase64String(base64));

        format.Should().Be(expected.HasValue ? (ImageFormat)expected.Value : (ImageFormat?)null);
    }

    [Theory]
    [InlineData("perfil", 0)]
    [InlineData("solicitud", 1)]
    [InlineData("otro", null)]
    public void ParseUsage_AcceptsContractValues_OI1_01(string value, int? expected)
    {
        MediaRules.ParseUsage(value).Should().Be(expected.HasValue ? (MediaUsage)expected.Value : (MediaUsage?)null);
    }

    [Fact]
    public void SasLifetime_IsFiveMinutes_OI1_02()
    {
        MediaRules.SasLifetime.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Theory]
    [InlineData(8000, 5000, true)]
    [InlineData(8001, 100, false)]
    [InlineData(100, 8001, false)]
    [InlineData(8000, 5001, false)]
    [InlineData(0, 100, false)]
    public void AreAllowedDimensions_Uses8000PxAnd40Mp_OI1_01(int width, int height, bool expected)
    {
        MediaRules.AreAllowedDimensions(width, height).Should().Be(expected);
    }

    [Fact]
    public void NewBlobKey_Is128RandomBitsInLowercaseHex_OI1_01()
    {
        var first = MediaRules.NewBlobKey();
        var second = MediaRules.NewBlobKey();

        first.Should().MatchRegex("^[0-9a-f]{32}$", "128 bits en hexadecimal");
        second.Should().NotBe(first, "cada nombre sale de RandomNumberGenerator");
    }

    [Fact]
    public void MediaFile_UsesRandomBlobKeyAndAlwaysJpeg_OI1_01()
    {
        var now = DateTimeOffset.UtcNow;
        var pending = MediaFile.CreatePending("sub-demo", MediaUsage.Profile, "image/webp", 1024, now);

        pending.QuarantineBlob.Should().Be(pending.BlobKey).And.NotBe(pending.Id.ToString("N"), "el blob no deriva del id v7");
        var ready = pending.MarkReady(800, 600, now);
        ready.OriginalBlob.Should().Be($"{pending.BlobKey}/original.jpg", "toda foto se recodifica a JPEG");
        ready.ReducedBlob.Should().Be($"{pending.BlobKey}/reducida.jpg");
    }
}
