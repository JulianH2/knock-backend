using FluentValidation;
using Knock.Api.Modules.Identity.Dtos;

namespace Knock.Api.Modules.Identity.Validators;

/// <summary>Forma del cierre de sesión (spec HU-004, caso 05). Ningún mensaje repite el valor enviado.</summary>
internal sealed class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    public LogoutRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Is required.")
            .Must(RefreshTokenShape.IsValid).WithMessage("Must be a Base64Url value of 32 bytes.");
    }
}
