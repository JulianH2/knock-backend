using System.Globalization;
using FluentValidation;
using Knock.Market.Api.Common.Errors;
using Knock.Market.Api.Modules.Requests.Domain;
using Knock.Market.Api.Modules.Requests.Dtos;

namespace Knock.Market.Api.Modules.Requests.Validators;

internal sealed class MyQuotesQueryValidator : AbstractValidator<MyQuotesQuery>
{
    private static readonly string[] _states = ["enviada", "aceptada", "rechazada", "retirada"];

    public MyQuotesQueryValidator()
    {
        RuleFor(x => x.Estado).Must(x => x is null || _states.Contains(x, StringComparer.Ordinal)).WithMessage("Invalid value.");
        RuleFor(x => x.Limite).Must(x => x is null || int.TryParse(x, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value is >= 1 and <= 50)
            .WithMessage("Invalid value.");
        RuleFor(x => x.Cursor).Must(ValidCursor).WithMessage("Invalid value.");
    }

    private static bool ValidCursor(string? value)
    {
        if (value is null) return true;
        try
        {
            _ = RequestCursor.Decode(value);
            return true;
        }
        catch (DomainException)
        {
            return false;
        }
    }
}
