using FluentValidation;
using Knock.Market.Api.Modules.Catalog.Domain;
using Knock.Market.Api.Modules.Catalog.Dtos;

namespace Knock.Market.Api.Modules.Catalog.Validators;

internal sealed class PostalCodeRouteValidator : AbstractValidator<PostalCodeRoute>
{
    public PostalCodeRouteValidator() =>
        RuleFor(route => route.Cp).Must(Colonia.IsPostalCode).WithMessage("The value is not valid.");
}
