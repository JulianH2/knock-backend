using FluentValidation;
using Knock.Identity.Api.Modules.Addresses.Dtos;
using Knock.Identity.Api.Modules.Addresses.Services;
using MongoDB.Bson;

namespace Knock.Identity.Api.Modules.Addresses.Validators;

/// <summary>Forma de los campos que se editan (spec 040, R-05 y R-10): solo se validan los enviados. Ningún mensaje repite el valor enviado.</summary>
internal sealed class UpdateAddressRequestValidator : AbstractValidator<UpdateAddressRequest>
{
    public UpdateAddressRequestValidator()
    {
        When(x => x.CalleNumero is not null, () => RuleFor(x => x.CalleNumero)
            .Cascade(CascadeMode.Stop)
            .Must(value => AddressRules.Normalize(value).Length >= AddressRules.StreetMinLength)
            .WithMessage($"Must have at least {AddressRules.StreetMinLength} characters.")
            .Must(value => !AddressRules.HasControlCharacters(AddressRules.Normalize(value))).WithMessage("The value is not valid."));

        When(x => x.Colonia is not null, () => RuleFor(x => x.Colonia)
            .Must(value => AddressRules.IsColoniaName(AddressRules.Normalize(value))).WithMessage("The value is not valid."));

        When(x => x.Referencias is not null, () => RuleFor(x => x.Referencias)
            .Cascade(CascadeMode.Stop)
            .Must(value => AddressRules.Normalize(value).Length <= AddressRules.ReferencesMaxLength)
            .WithMessage($"Must have at most {AddressRules.ReferencesMaxLength} characters.")
            .Must(value => !AddressRules.HasControlCharacters(AddressRules.Normalize(value))).WithMessage("The value is not valid."));
    }
}

/// <summary>El identificador de la ruta debe ser un identificador de dirección (24 dígitos hexadecimales). No se repite en el error.</summary>
internal sealed class AddressRouteValidator : AbstractValidator<AddressRoute>
{
    public AddressRouteValidator()
    {
        RuleFor(x => x.Id)
            .Must(value => value is { Length: 24 } && ObjectId.TryParse(value, out _))
            .WithMessage("Must be a valid address identifier.");
    }
}
