using FluentValidation;
using Knock.Market.Api.Modules.Requests.Domain;
using Knock.Market.Api.Modules.Requests.Dtos;
using MongoDB.Bson;

namespace Knock.Market.Api.Modules.Requests.Validators;

/// <summary>Formato de <c>GET /solicitudes/{id}/cotizaciones</c> (spec 021, R-04): <c>orden</c> es uno de los cuatro valores; otro responde 422 con <c>errors.orden</c>.</summary>
internal sealed class QuoteComparisonQueryValidator : AbstractValidator<QuoteComparisonQuery>
{
    public const string FormatReason = "The value is not valid.";

    public QuoteComparisonQueryValidator() =>
        RuleFor(x => x.Orden)
            .Must(value => QuoteComparer.Orders.Contains(value, StringComparer.Ordinal))
            .WithMessage(FormatReason)
            .When(x => x.Orden is not null);
}

/// <summary>Formato de <c>POST /solicitudes/{id}/elegir</c> (spec 021, R-09): <c>cotizacionId</c> es un <c>ObjectId</c>; si no, 422 con <c>errors.cotizacionId</c>. Nunca repite el valor.</summary>
internal sealed class ChooseQuoteBodyValidator : AbstractValidator<ChooseQuoteBody>
{
    public const string RequiredReason = "Required.";
    public const string FormatReason = "The value is not valid.";

    public ChooseQuoteBodyValidator() =>
        RuleFor(x => x.CotizacionId).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(RequiredReason)
            .Must(value => value!.Length == 24 && ObjectId.TryParse(value, out _)).WithMessage(FormatReason);
}
