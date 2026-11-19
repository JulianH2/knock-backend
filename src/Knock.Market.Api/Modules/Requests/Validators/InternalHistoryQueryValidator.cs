using System.Text.RegularExpressions;
using FluentValidation;
using Knock.Market.Api.Modules.Requests.Dtos;

namespace Knock.Market.Api.Modules.Requests.Validators;

/// <summary>Formato de la consulta interna del historial (spec 041, R-17): cliente, elemento de 24 hexadecimales, <c>limite</c> de 1 a 50 y un cursor opaco acotado. Nunca repite lo recibido.</summary>
internal sealed partial class InternalHistoryQueryValidator : AbstractValidator<InternalHistoryQuery>
{
    public const int MaxClientLength = 64;
    public const int MaxCursorLength = 256;
    public const string RequiredReason = "Required.";
    public const string FormatReason = "The value is not valid.";

    public InternalHistoryQueryValidator()
    {
        RuleFor(x => x.UsuarioId).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(RequiredReason)
            .Must(value => value!.Length <= MaxClientLength && value.All(character => !char.IsControl(character) && !char.IsWhiteSpace(character))).WithMessage(FormatReason);
        RuleFor(x => x.ElementoId).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(RequiredReason)
            .Must(value => ObjectIdPattern().IsMatch(value!)).WithMessage(FormatReason);
        RuleFor(x => x.Limite).InclusiveBetween(1, InternalHistoryQuery.MaxLimit).WithMessage(FormatReason);
        RuleFor(x => x.Cursor)
            .Must(value => value!.Length <= MaxCursorLength && CursorPattern().IsMatch(value))
            .WithMessage(FormatReason)
            .When(x => !string.IsNullOrEmpty(x.Cursor));
    }

    [GeneratedRegex("^[0-9a-fA-F]{24}$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex ObjectIdPattern();

    [GeneratedRegex("^[A-Za-z0-9_-]+$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex CursorPattern();
}
