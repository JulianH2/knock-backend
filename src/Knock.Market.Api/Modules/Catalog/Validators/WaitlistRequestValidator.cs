using System.Text.RegularExpressions;
using FluentValidation;
using Knock.Market.Api.Modules.Catalog.Dtos;

namespace Knock.Market.Api.Modules.Catalog.Validators;

internal sealed partial class WaitlistRequestValidator : AbstractValidator<WaitlistRequest>
{
    public WaitlistRequestValidator()
    {
        RuleFor(x => x.Cp).Must(value => value is not null && PostalCode().IsMatch(value)).WithMessage("The value is not valid.");
        RuleFor(x => x.Municipio).Must(value => value is null || (value.Length is > 0 and <= 100 && !string.IsNullOrWhiteSpace(value) && Municipality().IsMatch(value)))
            .WithMessage("The value is not valid.");
    }

    [GeneratedRegex(@"\A[0-9]{5}\z", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex PostalCode();

    [GeneratedRegex(@"\A[\p{L} .'-]+\z", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex Municipality();
}
