using FluentValidation;
using Knock.Identity.Api.Modules.Account.Dtos;
using Knock.Identity.Api.Modules.Sesiones.Validators;

namespace Knock.Identity.Api.Modules.Account.Validators;

/// <summary>
/// Forma de <c>POST /me/eliminacion</c> (spec 039, R-16 y R-25): la confirmación es el texto exacto <c>ELIMINAR</c>, el token de Firebase es opcional aquí
/// (el servicio lo exige si la sesión tiene más de 24 horas) y el motivo, opcional, de hasta 200 caracteres. Ningún mensaje repite el valor enviado.
/// </summary>
internal sealed class DeleteAccountRequestValidator : AbstractValidator<DeleteAccountRequest>
{
    /// <summary>Texto que la persona debe escribir para confirmar.</summary>
    public const string ConfirmationText = "ELIMINAR";

    /// <summary>Máximo de caracteres del motivo.</summary>
    public const int MaxReasonLength = 200;

    public DeleteAccountRequestValidator()
    {
        RuleFor(x => x.Confirmacion)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Is required.")
            .Must(text => string.Equals(text, ConfirmationText, StringComparison.Ordinal)).WithMessage("Must be the exact confirmation text.");
        RuleFor(x => x.FirebaseIdToken)
            .MaximumLength(FirebaseExchangeRequestValidator.MaxTokenLength)
            .WithMessage($"Must have at most {FirebaseExchangeRequestValidator.MaxTokenLength} characters.");
        RuleFor(x => x.Motivo)
            .MaximumLength(MaxReasonLength)
            .WithMessage($"Must have at most {MaxReasonLength} characters.");
    }
}
