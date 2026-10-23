using FluentValidation;
using Knock.Api.Modules.Media.Domain;
using Knock.Api.Modules.Media.Dtos;

namespace Knock.Api.Modules.Media.Validators;

/// <summary>Forma de la petición de subida; los campos se nombran como en el JSON (<c>uso</c>, <c>tamanoBytes</c>).</summary>
internal sealed class CreateUploadRequestValidator : AbstractValidator<CreateUploadRequest>
{
    public CreateUploadRequestValidator()
    {
        RuleFor(x => x.Usage)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(usage => MediaRules.ParseUsage(usage!) is not null).WithMessage("Must be perfil, solicitud or trabajo.")
            .OverridePropertyName("uso");
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SizeBytes).NotNull().GreaterThan(0).OverridePropertyName("tamanoBytes");
    }
}
