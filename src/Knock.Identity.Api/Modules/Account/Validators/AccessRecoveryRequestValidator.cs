using FluentValidation;
using Knock.Identity.Api.Modules.Account.Domain;
using Knock.Identity.Api.Modules.Account.Dtos;
using Knock.Identity.Api.Modules.Sesiones.Services;
using Knock.Identity.Api.Modules.Sesiones.Validators;

namespace Knock.Identity.Api.Modules.Account.Validators;

/// <summary>Forma de <c>POST /recuperacion-acceso</c> (spec 039, R-08). Ningún mensaje repite el valor enviado.</summary>
internal sealed class AccessRecoveryRequestValidator : AbstractValidator<AccessRecoveryRequest>
{
    public AccessRecoveryRequestValidator()
    {
        RuleFor(x => x.FirebaseIdToken)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Is required.")
            .MaximumLength(FirebaseExchangeRequestValidator.MaxTokenLength)
            .WithMessage($"Must have at most {FirebaseExchangeRequestValidator.MaxTokenLength} characters.");
        RuleFor(x => x.CelularAnterior)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Is required.")
            .Must(PhoneNumbers.IsValid).WithMessage("Must be a Mexican E.164 number (+52 and 10 digits).");
        RuleFor(x => x.Tipo)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Is required.")
            .Must(RecoveryTypes.IsValid).WithMessage("Must be one of: tecnico, cliente.");
    }
}
