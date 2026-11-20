using FluentValidation;
using Knock.Identity.Api.Modules.Panel.Dtos;

namespace Knock.Identity.Api.Modules.Panel.Validators;

/// <summary>Forma del primer paso (R-04): <c>idToken</c> de 1 a 4 096 caracteres. Ningún mensaje repite el valor enviado.</summary>
internal sealed class PanelCelularRequestValidator : AbstractValidator<PanelCelularRequest>
{
    public const int MaxTokenLength = 4096;

    public PanelCelularRequestValidator() =>
        RuleFor(x => x.IdToken)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Is required.")
            .MaximumLength(MaxTokenLength).WithMessage($"Must have at most {MaxTokenLength} characters.");
}

/// <summary>Forma del segundo paso (R-08): <c>desafioId</c> en base64url de 128 bits y <c>codigo</c> de exactamente 6 dígitos.</summary>
internal sealed class PanelTotpRequestValidator : AbstractValidator<PanelTotpRequest>
{
    public const int ChallengeIdLength = 22;
    public const int CodeLength = 6;

    public PanelTotpRequestValidator()
    {
        RuleFor(x => x.DesafioId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Is required.")
            .Must(IsChallengeId).WithMessage("Is not valid.");
        RuleFor(x => x.Codigo)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Is required.")
            .Must(IsSixDigits).WithMessage($"Must be exactly {CodeLength} digits.");
    }

    /// <summary>Seis dígitos ASCII; <c>char.IsDigit</c> aceptaría otros alfabetos.</summary>
    internal static bool IsSixDigits(string? value) =>
        value is { Length: CodeLength } && value.All(c => c is >= '0' and <= '9');

    internal static bool IsChallengeId(string? value) =>
        value is { Length: ChallengeIdLength } && value.All(c => c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-' or '_');
}

/// <summary>Forma del consumo del enlace de activación (R-29): token de 256 bits en base64url (43 caracteres), con tope de 1 KB en el cuerpo.</summary>
internal sealed class PanelActivacionRequestValidator : AbstractValidator<PanelActivacionRequest>
{
    public const int TokenLength = 43;

    public PanelActivacionRequestValidator() =>
        RuleFor(x => x.Token)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Is required.")
            .Must(value => value is { Length: TokenLength } && value.All(c => c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-' or '_'))
            .WithMessage("Is not valid.");
}
