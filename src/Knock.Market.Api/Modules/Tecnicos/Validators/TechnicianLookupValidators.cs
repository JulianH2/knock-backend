using System.Text.RegularExpressions;
using FluentValidation;
using Knock.Market.Api.Modules.Tecnicos.Dtos;

namespace Knock.Market.Api.Modules.Tecnicos.Validators;

/// <summary>TEC-06 (R-19): la colonia son 24 hexadecimales; que exista en el catálogo lo revisa el servicio.</summary>
internal sealed partial class CoverageQueryValidator : AbstractValidator<CoverageQuery>
{
    public CoverageQueryValidator() =>
        RuleFor(x => x.Colonia).Must(v => v is not null && ObjectIdText().IsMatch(v)).WithMessage("The value is not valid.");

    [GeneratedRegex(@"\A[0-9a-fA-F]{24}\z", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex ObjectIdText();
}

/// <summary>TEC-03 (R-41): de 2 a 40 caracteres entre letras, números y espacios, con al menos 2 que no sean espacio.</summary>
internal sealed partial class AutocompleteQueryValidator : AbstractValidator<AutocompleteQuery>
{
    public AutocompleteQueryValidator() =>
        RuleFor(x => x.Q).Must(v => v is not null && v.Length <= 40 && v.Trim().Length >= 2 && Text().IsMatch(v)).WithMessage("The value is not valid.");

    [GeneratedRegex(@"\A[\p{L}\p{N} ]+\z", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex Text();
}
