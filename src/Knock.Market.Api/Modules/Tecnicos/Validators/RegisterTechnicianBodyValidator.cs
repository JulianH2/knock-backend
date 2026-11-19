using System.Text.RegularExpressions;
using FluentValidation;
using Knock.Market.Api.Modules.Tecnicos.Domain;
using Knock.Market.Api.Modules.Tecnicos.Dtos;

namespace Knock.Market.Api.Modules.Tecnicos.Validators;

/// <summary>Reglas de forma de <c>POST /tecnicos</c> (spec 046, R-02 y R-39). Que los oficios estén activos y la colonia exista lo revisa el servicio.</summary>
internal sealed partial class RegisterTechnicianBodyValidator : AbstractValidator<RegisterTechnicianBody>
{
    private const string _invalid = "The value is not valid.";

    public RegisterTechnicianBodyValidator()
    {
        RuleFor(x => x.Nombre).Must(ValidName).WithMessage(_invalid);
        RuleFor(x => x.Oficios).Must(ValidTrades).WithMessage(_invalid);
        RuleFor(x => x.OtroOficio).Must(ValidOtherTrade).WithMessage(_invalid);
        RuleFor(x => x.Atencion).Must(TechnicianRules.IsKnownAttention).WithMessage(_invalid);
        RuleFor(x => x.DireccionTaller).Must(ValidWorkshop).When(x => TechnicianRules.IsKnownAttention(x.Atencion)).WithMessage(_invalid);
        RuleFor(x => x.RadioKm).Must(ValidRadius).When(x => TechnicianRules.IsKnownAttention(x.Atencion)).WithMessage(_invalid);
        RuleFor(x => x.ColoniaId).Must(v => v is not null && ObjectIdText().IsMatch(v)).WithMessage(_invalid);
        RuleFor(x => x.AceptaTerminos).Must(v => v == true).WithMessage(_invalid);
        RuleFor(x => x.Perfil!.NombreNegocio).Must(ValidBusinessName)
            .When(x => x.Perfil is not null).OverridePropertyName("perfil.nombreNegocio").WithMessage(_invalid);
        RuleFor(x => x.Perfil!.Descripcion).Must(ValidDescription)
            .When(x => x.Perfil is not null).OverridePropertyName("perfil.descripcion").WithMessage(_invalid);
        RuleFor(x => x.Perfil!.Anios).Must(v => v is null or >= 0 and <= 60)
            .When(x => x.Perfil is not null).OverridePropertyName("perfil.anios").WithMessage(_invalid);
    }

    private static bool ValidName(string? value) => value is not null && value.Trim().Length is >= 2 and <= 80 && PersonName().IsMatch(value);

    private static bool ValidOtherTrade(string? value) => value is null || (value.Trim().Length is >= 2 and <= 60 && FreeText().IsMatch(value));

    /// <summary>Obligatoria con taller o ambos (8 a 200 caracteres); no admitida a domicilio, para que nunca se guarde ni se muestre (R-39).</summary>
    private static bool ValidWorkshop(RegisterTechnicianBody body, string? value) =>
        TechnicianRules.UsesWorkshop(body.Atencion) ? value is not null && value.Trim().Length is >= 8 and <= 200 && !HasControl(value) : value is null;

    /// <summary>Obligatorio a domicilio o ambos (3, 5, 8 o 12); no admitido solo en taller.</summary>
    private static bool ValidRadius(RegisterTechnicianBody body, int? value) =>
        TechnicianRules.VisitsHomes(body.Atencion) ? value is not null && TechnicianRules.Radii.Contains(value.Value) : value is null;

    private static bool ValidBusinessName(string? value) => value is null || (value.Trim().Length is >= 1 and <= 60 && !HasControl(value));

    private static bool ValidDescription(string? value) => value is null || (value.Length <= 300 && !HasControl(value));

    private static bool ValidTrades(IReadOnlyList<string>? trades) =>
        trades is { Count: > 0 and <= TechnicianRules.MaxTradesInBody }
        && trades.All(t => t is not null && TradeKey().IsMatch(t))
        && trades.Distinct(StringComparer.Ordinal).Count() == trades.Count;

    private static bool HasControl(string value) => value.Any(char.IsControl);

    [GeneratedRegex(@"\A[\p{L}][\p{L} .'’-]*\z", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex PersonName();

    [GeneratedRegex(@"\A[\p{L}\p{N} .,'’()/-]+\z", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex FreeText();

    [GeneratedRegex(@"\A[a-z0-9_-]{1,16}\z", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex TradeKey();

    [GeneratedRegex(@"\A[0-9a-fA-F]{24}\z", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex ObjectIdText();
}
