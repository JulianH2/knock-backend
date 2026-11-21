using System.Globalization;
using FluentValidation;
using Knock.Market.Api.Modules.Tecnicos.Domain;
using Knock.Market.Api.Modules.Tecnicos.Dtos;
using MongoDB.Bson;

namespace Knock.Market.Api.Modules.Tecnicos.Validators;

/// <summary>TEC-02 (R-04 y R-05): los filtros de búsqueda responden 422 con <c>errors.&lt;campo&gt;</c> sin repetirlos; <c>radioKm</c> es 3, 5, 8 o 12;
/// <c>limite</c> entre 1 y 50.</summary>
internal sealed class TechnicianListQueryValidator : AbstractValidator<TechnicianListQuery>
{
    public TechnicianListQueryValidator()
    {
        const string invalid = "The value is not valid.";
        RuleFor(x => x.Orden).Must(v => v is null or "distancia" or "calificacion" or "recomendadas").WithMessage(invalid).WithName("orden");
        RuleFor(x => x.Cursor).Must(v => v is null || ObjectId.TryParse(v, out _)).WithMessage(invalid).WithName("cursor");
        RuleFor(x => x.RadioKm).Must(v => v is null || int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && TechnicianRules.Radii.Contains(parsed)).WithMessage(invalid).WithName("radioKm");
        RuleFor(x => x.Lat).Must(v => v is null || double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && parsed is >= -90 and <= 90).WithMessage(invalid).WithName("lat");
        RuleFor(x => x.Lng).Must(v => v is null || double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && parsed is >= -180 and <= 180).WithMessage(invalid).WithName("lng");
        RuleFor(x => x.DisponibleHoy).Must(v => v is null || bool.TryParse(v, out _)).WithMessage(invalid).WithName("disponibleHoy");
        RuleFor(x => x.SoloVerificados).Must(v => v is null || bool.TryParse(v, out _)).WithMessage(invalid).WithName("soloVerificados");
        RuleFor(x => x.Limite).Must(v => v is null || int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed is >= 1 and <= TechnicianListQuery.MaxLimit).WithMessage(invalid).WithName("limite");
        RuleFor(x => x.Lat).Must(v => v is not null).WithMessage(invalid).WithName("lat").When(x => x.Orden == "distancia");
        RuleFor(x => x.Lng).Must(v => v is not null).WithMessage(invalid).WithName("lng").When(x => x.Orden == "distancia");
    }
}
