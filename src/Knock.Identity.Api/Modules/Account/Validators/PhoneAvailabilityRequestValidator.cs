using FluentValidation;
using Knock.Identity.Api.Modules.Account.Dtos;
using Knock.Identity.Api.Modules.Sesiones.Domain;

namespace Knock.Identity.Api.Modules.Account.Validators;

/// <summary>Forma de <c>POST /me/cambio-numero</c> (spec 039, R-02): E.164 de México. Ningún mensaje repite el valor enviado.</summary>
internal sealed class PhoneAvailabilityRequestValidator : AbstractValidator<PhoneAvailabilityRequest>
{
    public PhoneAvailabilityRequestValidator()
    {
        RuleFor(x => x.CelularNuevo)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Is required.")
            .Must(PhoneNumberPolicy.IsValid).WithMessage("Must be a Mexican E.164 number (+52 and 10 digits).");
    }
}
