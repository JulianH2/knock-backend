using FluentValidation;
using Knock.Identity.Api.Modules.Addresses.Dtos;
using Knock.Identity.Api.Modules.Addresses.Services;

namespace Knock.Identity.Api.Modules.Addresses.Validators;

/// <summary>Forma de la dirección nueva (spec 040, R-03 y R-10). La existencia de la colonia se comprueba en el servicio. Ningún mensaje repite el valor enviado.</summary>
internal sealed class CreateAddressRequestValidator : AbstractValidator<CreateAddressRequest>
{
    public CreateAddressRequestValidator()
    {
        RuleFor(x => x.CalleNumero)
            .Cascade(CascadeMode.Stop)
            .Must(value => AddressRules.Normalize(value).Length > 0).WithMessage("Is required.")
            .Must(value => AddressRules.Normalize(value).Length >= AddressRules.StreetMinLength)
            .WithMessage($"Must have at least {AddressRules.StreetMinLength} characters.")
            .Must(value => !AddressRules.HasControlCharacters(AddressRules.Normalize(value))).WithMessage("The value is not valid.");

        RuleFor(x => x.Colonia)
            .Cascade(CascadeMode.Stop)
            .Must(value => AddressRules.Normalize(value).Length > 0).WithMessage("Is required.")
            .Must(value => AddressRules.IsColoniaName(AddressRules.Normalize(value))).WithMessage("The value is not valid.");

        RuleFor(x => x.Referencias)
            .Cascade(CascadeMode.Stop)
            .Must(value => AddressRules.Normalize(value).Length <= AddressRules.ReferencesMaxLength)
            .WithMessage($"Must have at most {AddressRules.ReferencesMaxLength} characters.")
            .Must(value => !AddressRules.HasControlCharacters(AddressRules.Normalize(value))).WithMessage("The value is not valid.");
    }
}
