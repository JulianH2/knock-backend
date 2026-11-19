using System.Text.RegularExpressions;
using FluentValidation;
using Knock.Market.Api.Modules.Tecnicos.Domain;
using Knock.Market.Api.Modules.Tecnicos.Dtos;

namespace Knock.Market.Api.Modules.Tecnicos.Validators;

/// <summary>TEC-09: <c>disponibleHoy</c> es obligatorio (R-30).</summary>
internal sealed class AvailabilityBodyValidator : AbstractValidator<AvailabilityBody>
{
    public AvailabilityBodyValidator() => RuleFor(x => x.DisponibleHoy).NotNull().WithMessage("The value is not valid.");
}

/// <summary>TEC-11: la lista completa de los siete días (R-47).</summary>
internal sealed class ScheduleBodyValidator : AbstractValidator<ScheduleBody>
{
    public ScheduleBodyValidator() => RuleFor(x => x.Horarios).Must(ScheduleRules.IsValid).WithMessage("The value is not valid.");
}

/// <summary>
/// TEC-08 (R-45): forma de cada campo enviado. Las reglas que dependen de la forma de atención ya guardada (dirección y radio) las revisa el servicio.
/// </summary>
internal sealed partial class UpdateTechnicianBodyValidator : AbstractValidator<UpdateTechnicianBody>
{
    private const string _invalid = "The value is not valid.";

    public UpdateTechnicianBodyValidator()
    {
        RuleFor(x => x.Nombre).Must(ValidName).WithMessage(_invalid);
        RuleFor(x => x.NombreNegocio).Must(v => Plain(v, 60)).WithMessage(_invalid);
        RuleFor(x => x.Descripcion).Must(v => Plain(v, 300)).WithMessage(_invalid);
        RuleFor(x => x.Anios).Must(v => v is null or >= 0 and <= 60).WithMessage(_invalid);
        RuleFor(x => x.Oficios).Must(ValidTrades).WithMessage(_invalid);
        RuleFor(x => x.OtroOficio).Must(ValidOtherTrade).WithMessage(_invalid);
        RuleFor(x => x.Atencion).Must(v => v is null || TechnicianRules.IsKnownAttention(v)).WithMessage(_invalid);
        RuleFor(x => x.DireccionTaller).Must(ValidWorkshop).WithMessage(_invalid);
        RuleFor(x => x.RadioKm).Must(v => v is null || TechnicianRules.Radii.Contains(v.Value)).WithMessage(_invalid);
        RuleFor(x => x.FotoUrl).Must(ValidPhoto).WithMessage(_invalid);
    }

    private static bool ValidName(string? v) => v is null || (v.Trim().Length is >= 2 and <= 80 && PersonName().IsMatch(v));

    /// <summary>Texto opcional sin caracteres de control; vacío quita el campo.</summary>
    private static bool Plain(string? v, int max) => v is null || (v.Length <= max && !v.Any(char.IsControl));

    private static bool ValidOtherTrade(string? v) => v is null || v.Length == 0 || (v.Trim().Length is >= 2 and <= 60 && FreeText().IsMatch(v));

    private static bool ValidWorkshop(string? v) => v is null || (v.Trim().Length is >= 8 and <= 200 && !v.Any(char.IsControl));

    private static bool ValidPhoto(string? v) =>
        v is null || (v.Length <= 500 && Uri.TryCreate(v, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps);

    private static bool ValidTrades(IReadOnlyList<string>? trades) =>
        trades is null
        || (trades is { Count: > 0 and <= TechnicianRules.MaxTradesInBody }
            && trades.All(t => t is not null && TradeKey().IsMatch(t))
            && trades.Distinct(StringComparer.Ordinal).Count() == trades.Count);

    [GeneratedRegex(@"\A[\p{L}][\p{L} .'’-]*\z", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex PersonName();

    [GeneratedRegex(@"\A[\p{L}\p{N} .,'’()/-]+\z", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex FreeText();

    [GeneratedRegex(@"\A[a-z0-9_-]{1,16}\z", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex TradeKey();
}
