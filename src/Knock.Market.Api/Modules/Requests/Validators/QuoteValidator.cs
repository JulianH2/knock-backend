using System.Globalization;
using FluentValidation;
using Knock.Market.Api.Modules.Requests.Domain;
using Microsoft.Extensions.Options;

namespace Knock.Market.Api.Modules.Requests.Validators;

// RequestDate must come from the stored request, never from the quote submitted by the client.
internal sealed record QuoteValidationInput(
    string? TipoPrecio, decimal? Precio, decimal? PrecioMin, decimal? PrecioMax,
    DateOnly? Dia, string? Hora, string? Nota, DateOnly RequestDate);

/// <summary>Validates quote fields; the service must separately moderate the note before saving.</summary>
internal sealed class QuoteValidator : AbstractValidator<QuoteValidationInput>
{
    public QuoteValidator(IOptions<QuotePricingOptions> pricing)
    {
        ArgumentNullException.ThrowIfNull(pricing);
        var options = pricing.Value;
        RuleFor(x => x.TipoPrecio).Must(x => x is "fijo" or "rango" or "visita").WithMessage("Unknown price type.");
        RuleFor(x => x.Precio).Must((x, value) => x.TipoPrecio == "visita"
                ? AmountForType(value, true, options.VisitFeeMin, options.VisitFeeMax)
                : AmountForType(value, x.TipoPrecio == "fijo"))
            .WithMessage(x => x.TipoPrecio == "visita"
                ? $"Visit fee must be a whole number between {options.VisitFeeMin} and {options.VisitFeeMax}."
                : "Amount is missing, invalid, or not applicable.");
        RuleFor(x => x.PrecioMin).Must((x, value) => AmountForType(value, x.TipoPrecio == "rango"))
            .WithMessage("Amount is missing, invalid, or not applicable.");
        RuleFor(x => x.PrecioMax).Must((x, value) => AmountForType(value, x.TipoPrecio == "rango"))
            .WithMessage("Amount is missing, invalid, or not applicable.");
        RuleFor(x => x.PrecioMax).Must((x, value) => x.TipoPrecio != "rango" || x.PrecioMin is null || value is null || x.PrecioMin < value)
            .WithMessage("Maximum must be greater than minimum.");
        RuleFor(x => x.Dia).Must((x, value) => value is not null && value.Value.DayNumber - x.RequestDate.DayNumber is >= 0 and <= 7)
            .WithMessage("Day must be within seven days of the request date.");
        RuleFor(x => x.Hora).Must(ValidHour).WithMessage("Time must use HH:mm in the 24-hour clock.");
        RuleFor(x => x.Nota).MaximumLength(300).WithMessage("Note must contain at most 300 characters.");
    }

    private static bool AmountForType(decimal? amount, bool required, int min = QuotePricing.MinAmount, int max = QuotePricing.MaxAmount) => required
        ? amount >= min && amount <= max && decimal.Truncate(amount.Value) == amount.Value
        : amount is null;

    private static bool ValidHour(string? value) => value is { Length: 5 }
        && value[0] is >= '0' and <= '9' && value[1] is >= '0' and <= '9' && value[2] == ':'
        && value[3] is >= '0' and <= '9' && value[4] is >= '0' and <= '9'
        && (value[0] - '0') * 10 + value[1] - '0' < 24
        && TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
}
