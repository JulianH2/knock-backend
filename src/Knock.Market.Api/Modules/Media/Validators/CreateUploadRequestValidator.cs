using FluentValidation;
using Knock.Market.Api.Modules.Media.Domain;
using Knock.Market.Api.Modules.Media.Dtos;

namespace Knock.Market.Api.Modules.Media.Validators;

/// <summary>Validación de forma de la petición de subida (422 con campos).</summary>
internal sealed class CreateUploadRequestValidator : AbstractValidator<CreateUploadRequest>
{
    public CreateUploadRequestValidator()
    {
        RuleFor(x => x.Usage).Cascade(CascadeMode.Stop).NotEmpty()
            .Must(u => MediaRules.ParseUsage(u!) is not null).WithMessage("Must be perfil, solicitud or trabajo.")
            .OverridePropertyName("uso");
        RuleFor(x => x.ContentType).Cascade(CascadeMode.Stop).NotEmpty()
            .Must(c => MediaRules.IsAllowedContentType(c!)).OverridePropertyName("contentType");
        RuleFor(x => x.SizeBytes).Cascade(CascadeMode.Stop).NotNull()
            .Must(s => s is { } v && MediaRules.IsAllowedSize(v)).OverridePropertyName("tamanoBytes");
    }
}
