using FluentValidation;
using Knock.Identity.Api.Modules.Account.Dtos;
using Knock.Identity.Api.Modules.Sesiones.Validators;

namespace Knock.Identity.Api.Modules.Account.Validators;

/// <summary>Forma de <c>POST /me/cambio-numero/confirmar</c> (spec 039, R-05): el token de Firebase del número nuevo. Ningún mensaje repite el valor.</summary>
internal sealed class ConfirmPhoneChangeRequestValidator : AbstractValidator<ConfirmPhoneChangeRequest>
{
    public ConfirmPhoneChangeRequestValidator()
    {
        RuleFor(x => x.FirebaseIdToken)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Is required.")
            .MaximumLength(FirebaseExchangeRequestValidator.MaxTokenLength)
            .WithMessage($"Must have at most {FirebaseExchangeRequestValidator.MaxTokenLength} characters.");
    }
}
