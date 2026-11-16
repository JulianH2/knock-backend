using System.Globalization;
using System.Text.RegularExpressions;
using FluentValidation;
using Knock.Market.Api.Modules.Requests.Dtos;

namespace Knock.Market.Api.Modules.Requests.Validators;

/// <summary>
/// Formato de <c>POST /solicitudes</c> (spec 020, R-01 y R-03 a R-06). Las reglas que dependen del catálogo, de las fotos, de la moderación o de la modalidad las
/// aplica <c>SolicitudService</c>. Los motivos nunca repiten el valor recibido.
/// </summary>
internal sealed partial class CreateRequestBodyValidator : AbstractValidator<CreateRequestBody>
{
    public const int MinDescription = 8;
    public const int MaxDescription = 500;
    public const int MinAddress = 8;
    public const int MaxAddress = 200;
    public const int MaxPhotos = 5;
    public const int MaxDaysAhead = 5;
    public const int MaxTradeKey = 16;
    public const string TimeZoneId = "America/Mexico_City";

    public const string RequiredReason = "Required.";
    public const string FormatReason = "The value is not valid.";

    private static readonly string[] _slots = ["manana", "tarde", "noche"];

    public CreateRequestBodyValidator(TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        RuleFor(x => x.OficioClave).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(RequiredReason)
            .Must(value => value!.Length <= MaxTradeKey).WithMessage(FormatReason);

        RuleFor(x => x.Descripcion).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(RequiredReason)
            .Must(value => value!.Trim().Length is >= MinDescription and <= MaxDescription).WithMessage(FormatReason);

        RuleFor(x => x.Fotos).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(RequiredReason)
            .Must(photos => photos!.Count is >= 1 and <= MaxPhotos).WithMessage(FormatReason)
            .Must(photos => photos!.All(IsObjectId)).WithMessage(FormatReason)
            .Must(photos => photos!.Select(p => p.ToUpperInvariant()).Distinct(StringComparer.Ordinal).Count() == photos!.Count).WithMessage(FormatReason);

        RuleFor(x => x.Dia).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(RequiredReason)
            .Must(value => IsWithinWindow(value!, clock)).WithMessage(FormatReason);

        RuleFor(x => x.Franja).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(RequiredReason)
            .Must(value => _slots.Contains(value, StringComparer.Ordinal)).WithMessage(FormatReason);

        RuleFor(x => x.ColoniaId).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(RequiredReason)
            .Must(IsObjectId).WithMessage(FormatReason);

        RuleFor(x => x.Direccion)
            .Must(value => value!.Trim().Length is >= MinAddress and <= MaxAddress).WithMessage(FormatReason)
            .When(x => x.Direccion is not null);
    }

    private static bool IsObjectId(string? value) => value is not null && ObjectIdPattern().IsMatch(value);

    /// <summary>Hoy a hoy + 5 días en la hora de Ciudad de México; el formato es exactamente <c>YYYY-MM-DD</c>.</summary>
    private static bool IsWithinWindow(string value, TimeProvider clock)
    {
        if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
        {
            return false;
        }

        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId)).DateTime);
        return day >= today && day <= today.AddDays(MaxDaysAhead);
    }

    [GeneratedRegex("^[0-9a-fA-F]{24}$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex ObjectIdPattern();
}
