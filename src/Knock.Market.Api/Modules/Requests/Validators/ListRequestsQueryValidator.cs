using FluentValidation;
using Knock.Market.Api.Modules.Requests.Domain;
using Knock.Market.Api.Modules.Requests.Dtos;

namespace Knock.Market.Api.Modules.Requests.Validators;

/// <summary>Formato de <c>GET /solicitudes</c> (spec 020, R-09): <c>limit</c> de 1 a 50 y <c>estado</c> uno de los siete valores. Un cursor ilegible lo rechaza el servicio con 400.</summary>
internal sealed class ListRequestsQueryValidator : AbstractValidator<ListRequestsQuery>
{
    public const string FormatReason = "The value is not valid.";

    private static readonly string[] _states = [.. Enum.GetValues<RequestState>().Select(RequestStates.ToStored)];

    public ListRequestsQueryValidator()
    {
        RuleFor(x => x.Limit).InclusiveBetween(1, ListRequestsQuery.MaxLimit).WithMessage(FormatReason);
        RuleFor(x => x.Estado)
            .Must(value => _states.Contains(value, StringComparer.Ordinal))
            .WithMessage(FormatReason)
            .When(x => x.Estado is not null);
    }
}
