using FluentValidation;
using Knock.Api.Modules.Identity.Dtos;

namespace Knock.Api.Modules.Identity.Validators;

/// <summary>Forma del canje (spec HU-001, D2 y D5). Los campos se nombran como en el JSON; ningún mensaje repite el valor enviado.</summary>
internal sealed class FirebaseExchangeRequestValidator : AbstractValidator<FirebaseExchangeRequest>
{
    public const int MaxTokenLength = 4096;
    public const int DeviceIdLength = 36;

    public FirebaseExchangeRequestValidator()
    {
        RuleFor(x => x.FirebaseIdToken)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Is required.")
            .MaximumLength(MaxTokenLength).WithMessage($"Must have at most {MaxTokenLength} characters.");
        RuleFor(x => x.DeviceId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Is required.")
            .Must(IsCanonicalUuid).WithMessage("Must be a UUID of 36 characters.");
        RuleFor(x => x.AceptaAviso)
            .Must(accepted => accepted == true).WithMessage("Must be true.");
    }

    private static bool IsCanonicalUuid(string? value) =>
        value is { Length: DeviceIdLength } && Guid.TryParseExact(value, "D", out _);
}
