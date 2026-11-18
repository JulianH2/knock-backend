using System.Globalization;
using System.Text.RegularExpressions;
using FluentValidation;
using Knock.Identity.Api.Modules.HomeAndCar.Domain;
using Knock.Identity.Api.Modules.HomeAndCar.Dtos;
using MongoDB.Bson;

namespace Knock.Identity.Api.Modules.HomeAndCar.Validators;

/// <summary>Motivos comunes. Ninguno repite el valor recibido (spec 041, R-03 y R-21).</summary>
internal static class ItemReasons
{
    public const string Required = "Is required.";
    public const string InvalidId = "The value is not valid.";
    public const string InvalidType = "Must be one of the allowed types.";
    public const string InvalidName = "Must have 2 to 60 characters without control characters.";
    public const string YearNotForType = "Is only allowed for the car type.";

    public static string InvalidYear(int maxYear) => string.Create(CultureInfo.InvariantCulture, $"Must be between {HomeAndCarRules.MinYear} and {maxYear}.");
}

/// <summary>Forma del <c>{id}</c> de la ruta (spec 041, R-08, R-11): 24 caracteres hexadecimales; otra cosa responde 422 <c>errors.id</c>.</summary>
internal sealed class ItemRouteValidator : AbstractValidator<ItemRoute>
{
    public ItemRouteValidator() =>
        RuleFor(route => route.Id)
            .Must(value => value is { Length: 24 } && ObjectId.TryParse(value, out _))
            .WithMessage(ItemReasons.InvalidId);
}

/// <summary>Formato del cuerpo de <c>POST /me/casa-y-auto</c> (spec 041, R-03 y R-14): tipo de la lista, nombre de 2 a 60 y año solo para autos y en rango.</summary>
internal sealed class CreateItemRequestValidator : AbstractValidator<CreateItemRequest>
{
    public CreateItemRequestValidator(TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        RuleFor(x => x.Tipo).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(ItemReasons.Required)
            .Must(ItemTypes.IsValid).WithMessage(ItemReasons.InvalidType);
        RuleFor(x => x.Nombre).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(ItemReasons.Required)
            .Must(name => HomeAndCarRules.NormalizeName(name) is not null).WithMessage(ItemReasons.InvalidName);
        RuleFor(x => x.Anio).Cascade(CascadeMode.Stop)
            .Must(year => year >= HomeAndCarRules.MinYear && year <= HomeAndCarRules.MaxYear(clock.GetUtcNow()))
            .WithMessage(_ => ItemReasons.InvalidYear(HomeAndCarRules.MaxYear(clock.GetUtcNow())))
            .Must((request, _) => !ItemTypes.IsValid(request.Tipo) || request.Tipo == ItemTypes.Car)
            .WithMessage(ItemReasons.YearNotForType)
            .When(x => x.Anio is not null);
    }
}

/// <summary>
/// Formato del cuerpo de <c>PUT /me/casa-y-auto/{id}</c> (spec 041, R-05 y R-14): cada campo enviado cumple su regla. Que el elemento resultante sea coherente (año solo en autos)
/// con lo ya guardado lo decide <c>HomeAndCarService</c>.
/// </summary>
internal sealed class UpdateItemRequestValidator : AbstractValidator<UpdateItemRequest>
{
    public UpdateItemRequestValidator(TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        RuleFor(x => x.Tipo).Must(ItemTypes.IsValid).WithMessage(ItemReasons.InvalidType).When(x => x.Tipo is not null);
        RuleFor(x => x.Nombre).Must(name => HomeAndCarRules.NormalizeName(name) is not null).WithMessage(ItemReasons.InvalidName).When(x => x.Nombre is not null);
        RuleFor(x => x.Anio).Cascade(CascadeMode.Stop)
            .Must(year => year >= HomeAndCarRules.MinYear && year <= HomeAndCarRules.MaxYear(clock.GetUtcNow()))
            .WithMessage(_ => ItemReasons.InvalidYear(HomeAndCarRules.MaxYear(clock.GetUtcNow())))
            .Must((request, _) => request.Tipo is null or ItemTypes.Car)
            .WithMessage(ItemReasons.YearNotForType)
            .When(x => x.Anio is not null);
    }
}

/// <summary>Formato de la consulta del historial (spec 041, R-11 y R-13): <c>limite</c> de 1 a 50 y un <c>cursor</c> opaco acotado y sin caracteres raros.</summary>
internal sealed partial class HistoryQueryValidator : AbstractValidator<HistoryQuery>
{
    public const int MaxCursorLength = 256;
    public const string LimitReason = "Must be a whole number from 1 to 50.";
    public const string CursorReason = "The value is not valid.";

    public HistoryQueryValidator()
    {
        RuleFor(x => x.Limite)
            .Must(value => int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var limit) && limit is >= 1 and <= HistoryQuery.MaxLimit)
            .WithMessage(LimitReason)
            .When(x => x.Limite is not null);
        RuleFor(x => x.Cursor)
            .Must(value => value!.Length <= MaxCursorLength && CursorPattern().IsMatch(value))
            .WithMessage(CursorReason)
            .When(x => x.Cursor is not null);
    }

    [GeneratedRegex("^[A-Za-z0-9_-]+$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex CursorPattern();
}
