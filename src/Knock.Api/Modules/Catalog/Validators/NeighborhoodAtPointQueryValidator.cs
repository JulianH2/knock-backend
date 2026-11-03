using System.Globalization;
using System.Text.RegularExpressions;
using FluentValidation;
using Knock.Api.Modules.Catalog.Dtos;

namespace Knock.Api.Modules.Catalog.Validators;

/// <summary>
/// Formato y rangos de spec TT-API-07 §4 con cultura invariante. Los motivos nunca incluyen el valor. Rechaza el valor ausente y el
/// que deja el binder para un parámetro repetido (no cumple el formato).
/// </summary>
internal sealed partial class NeighborhoodAtPointQueryValidator : AbstractValidator<NeighborhoodAtPointQuery>
{
    public const string RequiredReason = "Required.";
    public const string FormatReason = "Must be a decimal number with a dot separator and up to 10 decimals.";

    public NeighborhoodAtPointQueryValidator()
    {
        RuleFor(x => x.Lat).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(RequiredReason)
            .Must(HasCoordinateFormat).WithMessage(FormatReason)
            .Must(value => Parse(value!) is >= -90 and <= 90).WithMessage("Must be between -90 and 90.");

        RuleFor(x => x.Lon).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(RequiredReason)
            .Must(HasCoordinateFormat).WithMessage(FormatReason)
            .Must(value => Parse(value!) is >= -180 and <= 180).WithMessage("Must be between -180 and 180.");
    }

    /// <summary>Interpreta un valor que ya cumple el formato; no depende de la cultura del hilo ni de <c>Accept-Language</c>.</summary>
    public static double Parse(string value) =>
        double.Parse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);

    /// <summary>
    /// Mismo patrón que <see cref="NeighborhoodAtPointQuery.Pattern"/> con dígitos ASCII y <c>\z</c>: <c>\d</c> aceptaría dígitos de
    /// otros alfabetos y <c>$</c> un salto de línea final.
    /// </summary>
    [GeneratedRegex(@"^-?[0-9]{1,3}(\.[0-9]{1,10})?\z", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex CoordinateFormat();

    private static bool HasCoordinateFormat(string? value) =>
        value is { Length: <= NeighborhoodAtPointQuery.MaxLength } && CoordinateFormat().IsMatch(value);
}
