using FluentValidation;
using Knock.Market.Api.Hosting;

namespace Knock.Market.Api.Modules.Requests.Validators;

internal sealed class BandejaQuery
{
    public string? Cursor { get; set; }
    public int Limit { get; set; } = 20;
}

internal sealed class BandejaQueryValidator : AbstractValidator<BandejaQuery>
{
    public BandejaQueryValidator() =>
        RuleFor(query => query.Limit).InclusiveBetween(1, 20).WithMessage(ValidationFilter.GenericReason);
}
