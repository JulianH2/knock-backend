using FluentValidation;
using Knock.Api.Modules.Identity.Dtos;

namespace Knock.Api.Modules.Identity.Validators;

/// <summary>Forma de la renovación (spec HU-003, caso 11). Ningún mensaje repite el valor enviado.</summary>
internal sealed class RefreshSessionRequestValidator : AbstractValidator<RefreshSessionRequest>
{
    public RefreshSessionRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Is required.")
            .Must(RefreshTokenShape.IsValid).WithMessage("Must be a Base64Url value of 32 bytes.");
        RuleFor(x => x.DeviceId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Is required.")
            .Must(FirebaseExchangeRequestValidator.IsCanonicalUuid).WithMessage("Must be a UUID of 36 characters.");
    }
}
