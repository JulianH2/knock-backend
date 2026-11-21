using FluentValidation;
using Knock.Identity.Api.Modules.Trust.Domain;
using Knock.Identity.Api.Modules.Trust.Dtos;

namespace Knock.Identity.Api.Modules.Trust.Validators;

/// <summary>
/// Forma de <c>POST /me/verificacion</c> (spec 025): <c>nivel</c> desconocido responde 422 con <c>errors.nivel</c> (R-03, CA-04) y <c>versionAviso</c> es obligatoria.
/// El consentimiento (<c>consent_required</c>) y la CURP (<c>curp_invalid</c>) los revisa el servicio, porque tienen su propio código (R-02 y R-04).
/// </summary>
internal sealed class CreateVerificationRequestValidator : AbstractValidator<CreateVerificationRequest>
{
    public CreateVerificationRequestValidator()
    {
        RuleFor(x => x.Nivel).Must(VerificationLevels.IsKnown).WithMessage("The value is not valid.");
        RuleFor(x => x.VersionAviso).Must(v => !string.IsNullOrWhiteSpace(v) && v.Length <= 20).WithMessage("The value is not valid.");
    }
}
