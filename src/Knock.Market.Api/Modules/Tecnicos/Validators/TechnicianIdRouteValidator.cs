using System.Text.RegularExpressions;
using FluentValidation;
using Knock.Market.Api.Modules.Tecnicos.Dtos;

namespace Knock.Market.Api.Modules.Tecnicos.Validators;

/// <summary>Spec 046, R-15: el identificador de la ruta son 24 hexadecimales.</summary>
internal sealed partial class TechnicianIdRouteValidator : AbstractValidator<TechnicianIdRoute>
{
    public TechnicianIdRouteValidator() =>
        RuleFor(x => x.Id).Must(v => v is not null && ObjectIdText().IsMatch(v)).WithMessage("The value is not valid.");

    [GeneratedRegex(@"\A[0-9a-fA-F]{24}\z", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex ObjectIdText();
}
