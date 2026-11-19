using FluentValidation;
using Knock.Market.Api.Modules.Trust.Dtos;
using MongoDB.Bson;

namespace Knock.Market.Api.Modules.Trust.Validators;

/// <summary>Formato de <c>GET /me/tecnicos-confianza</c> (spec 042, R-02): <c>limite</c> de 1 a 50; otro valor responde 422 con <c>errors.Limite</c>. Un cursor ilegible lo rechaza el servicio con 400.</summary>
internal sealed class TrustedListQueryValidator : AbstractValidator<TrustedListQuery>
{
    public const string FormatReason = "The value is not valid.";

    public TrustedListQueryValidator() =>
        RuleFor(x => x.Limite).InclusiveBetween(1, TrustedListQuery.MaxLimit).WithMessage(FormatReason);
}

/// <summary>Formato de <c>PUT</c> y <c>DELETE /me/tecnicos-confianza/{tecnicoId}</c> (spec 042, R-04 y R-07): <c>tecnicoId</c> es un <c>ObjectId</c>; si no, 422 sin repetir el valor.</summary>
internal sealed class TrustedTechnicianRouteValidator : AbstractValidator<TrustedTechnicianRoute>
{
    public const string FormatReason = "The value is not valid.";

    public TrustedTechnicianRouteValidator() =>
        RuleFor(x => x.TecnicoId).Must(value => value is { Length: 24 } && ObjectId.TryParse(value, out _)).WithMessage(FormatReason);
}
