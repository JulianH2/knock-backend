using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Knock.Api.Modules.Catalog.ModelBinders;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Knock.Api.Modules.Catalog.Dtos;

/// <summary>
/// Query de <c>GET /colonias/en-punto</c>. <c>string</c> con un binder de un solo valor (spec TT-API-07 §3, opción 2 de (j)): sin tipos
/// numéricos para que el 400 automático no repita el valor ni acepte <c>NaN</c> o separadores de miles. <c>[BindRequired]</c>,
/// <c>[RegularExpression]</c> y <c>[MaxLength]</c> son solo para el OpenAPI; <c>[ValidateNever]</c> evita que MVC los valide. La
/// validación real es <c>NeighborhoodAtPointQueryValidator</c> (422).
/// </summary>
internal sealed class NeighborhoodAtPointQuery
{
    public const string Pattern = @"^-?\d{1,3}(\.\d{1,10})?$";
    public const int MaxLength = 16;

    [SingleValueFromQuery("lat")]
    [BindRequired]
    [RegularExpression(Pattern)]
    [MaxLength(MaxLength)]
    [ValidateNever]
    [Description("Latitude in decimal degrees, from -90 to 90, dot as decimal separator, up to 10 decimals. Exactly one value.")]
    public string? Lat { get; set; }

    [SingleValueFromQuery("lon")]
    [BindRequired]
    [RegularExpression(Pattern)]
    [MaxLength(MaxLength)]
    [ValidateNever]
    [Description("Longitude in decimal degrees, from -180 to 180, dot as decimal separator, up to 10 decimals. Exactly one value.")]
    public string? Lon { get; set; }
}
