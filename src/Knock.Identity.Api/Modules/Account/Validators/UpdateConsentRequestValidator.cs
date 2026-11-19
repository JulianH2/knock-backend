using FluentValidation;
using Knock.Identity.Api.Modules.Account.Domain;
using Knock.Identity.Api.Modules.Account.Dtos;

namespace Knock.Identity.Api.Modules.Account.Validators;

/// <summary>
/// Forma de <c>PUT /me/consentimientos</c> (spec 039, R-13 y R-23): solo los opcionales se activan o retiran por esta ruta; un tipo fuera de la lista
/// o uno obligatorio responde 422. Ningún mensaje repite el valor enviado.
/// </summary>
internal sealed class UpdateConsentRequestValidator : AbstractValidator<UpdateConsentRequest>
{
    public UpdateConsentRequestValidator()
    {
        RuleFor(x => x.Tipo)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Is required.")
            .Must(ConsentTypes.IsOptional).WithMessage("Must be one of: promociones, encuestas, recomendaciones.");
        RuleFor(x => x.Vigente)
            .NotNull().WithMessage("Is required.");
    }
}
