using AwesomeAssertions;
using Knock.Market.Api.Modules.Media.Domain;
using Knock.Market.Api.Modules.Media.Dtos;
using Knock.Market.Api.Modules.Media.Validators;
using Xunit;

namespace Knock.UnitTest.Media;

public class CreateUploadRequestValidatorTests
{
    private readonly CreateUploadRequestValidator _validator = new();
    [Fact] public void ValidRequest_Passes_OI1_01() { _validator.Validate(new CreateUploadRequest("perfil", "image/jpeg", 1024)).IsValid.Should().BeTrue(); }
    [Fact] public void InvalidUsage_Fails_OI1_01() { _validator.Validate(new CreateUploadRequest("otro", "image/jpeg", 1024)).IsValid.Should().BeFalse(); }
    [Fact] public void PngContentType_Passes_OI1_01() { _validator.Validate(new CreateUploadRequest("perfil", "image/png", 1024)).IsValid.Should().BeTrue(); }
    [Fact] public void GifContentType_Fails_OI1_01() { var r = _validator.Validate(new CreateUploadRequest("perfil", "image/gif", 1024)); r.IsValid.Should().BeFalse(); r.Errors.Should().Contain(e => e.PropertyName == "contentType"); }
    [Fact] public void WebpContentType_Passes_OI1_01() { _validator.Validate(new CreateUploadRequest("perfil", "image/webp", 1024)).IsValid.Should().BeTrue(); }
    [Fact] public void OneByteSize_Passes_OI1_01() { _validator.Validate(new CreateUploadRequest("perfil", "image/jpeg", 1)).IsValid.Should().BeTrue(); }
    [Fact] public void ExactlyMaxSize_Passes_OI1_01() { _validator.Validate(new CreateUploadRequest("perfil", "image/jpeg", MediaRules.MaxFileBytes)).IsValid.Should().BeTrue(); }
    [Fact] public void MaxPlusOneSize_Fails_OI1_01() { var r = _validator.Validate(new CreateUploadRequest("perfil", "image/jpeg", MediaRules.MaxFileBytes + 1)); r.IsValid.Should().BeFalse(); r.Errors.Should().Contain(e => e.PropertyName == "tamanoBytes"); }
    [Fact] public void ZeroSize_Fails_OI1_01() { _validator.Validate(new CreateUploadRequest("perfil", "image/jpeg", 0)).IsValid.Should().BeFalse(); }
    [Fact] public void NullSize_Fails_OI1_01() { _validator.Validate(new CreateUploadRequest("perfil", "image/jpeg", null)).IsValid.Should().BeFalse(); }
}
