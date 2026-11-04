using FluentValidation;
using Knock.Api.Modules.Identity.Domain;
using Knock.Api.Modules.Identity.Dtos;

namespace Knock.Api.Modules.Identity.Validators;

/// <summary>Forma del nombre (spec HU-002, casos 03 y 04). Ningún mensaje repite el valor enviado.</summary>
internal sealed class ProfileRequestValidator : AbstractValidator<ProfileRequest>
{
    public ProfileRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Is required.")
            .Must(name => PersonName.TryNormalize(name, out _))
            .WithMessage($"Must have {PersonName.MinLength} to {PersonName.MaxLength} letters, spaces, hyphens or apostrophes.");
    }
}
