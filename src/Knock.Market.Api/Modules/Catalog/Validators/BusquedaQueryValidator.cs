using System.Globalization;
using FluentValidation;
using Knock.Market.Api.Modules.Catalog.Domain;
using Knock.Market.Api.Modules.Catalog.Dtos;
using MongoDB.Bson;

namespace Knock.Market.Api.Modules.Catalog.Validators;

internal sealed class BusquedaQueryValidator : AbstractValidator<BusquedaQuery>
{
    private static readonly string[] _known = ["q", "coloniaId", "oficio", "soloVerificados", "cursor", "limit"];

    public BusquedaQueryValidator()
    {
        RuleFor(x => x).Custom((query, context) =>
        {
            foreach (var name in _known)
                if (query.Values.TryGetValue(name, out var values) && values.Count != 1) context.AddFailure(name, "Only one value is allowed.");
            ValidateQuery(query, context);
            ValidateOptions(query, context);
        });
    }

    private static void ValidateQuery(BusquedaQuery query, ValidationContext<BusquedaQuery> context)
    {
        var raw = query.Get("q");
        var normalized = raw is null ? string.Empty : SearchNameNormalizer.NormalizeQuery(raw);
        if (normalized.Length is < 2 or > 80 || normalized.Any(c => !char.IsLetterOrDigit(c) && c != ' '))
            context.AddFailure("q", "The value is not valid.");
        if (!ObjectId.TryParse(query.Get("coloniaId"), out _)) context.AddFailure("coloniaId", "The value is not valid.");
    }

    private static void ValidateOptions(BusquedaQuery query, ValidationContext<BusquedaQuery> context)
    {
        var limit = query.Get("limit");
        if (limit is not null && (!int.TryParse(limit, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) || parsed is < 1 or > 20))
            context.AddFailure("limit", "The value is not valid.");
        var verified = query.Get("soloVerificados");
        if (verified is not null && !bool.TryParse(verified, out _)) context.AddFailure("soloVerificados", "The value is not valid.");
        if (query.Values.ContainsKey("oficio") && query.Get("oficio") is "") context.AddFailure("oficio", "The value is not valid.");
    }
}
