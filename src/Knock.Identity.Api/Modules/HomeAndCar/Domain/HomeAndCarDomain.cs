using MongoDB.Bson;

namespace Knock.Identity.Api.Modules.HomeAndCar.Domain;

/// <summary>Los cuatro tipos de elemento de «Mi casa y mi auto» (spec 041, R-14 y D-06): lista cerrada, sin catálogo administrable.</summary>
internal static class ItemTypes
{
    public const string Car = "auto";
    public const string Appliance = "electrodomestico";
    public const string Installation = "instalacion";
    public const string Other = "otro";

    public static readonly IReadOnlyList<string> All = [Car, Appliance, Installation, Other];

    public static bool IsValid(string? value) => value is not null && All.Contains(value, StringComparer.Ordinal);
}

/// <summary>Reglas fijas de los datos del elemento (spec 041, R-14, R-15 y §6).</summary>
internal static class HomeAndCarRules
{
    public const int NameMinLength = 2;
    public const int NameMaxLength = 60;
    public const int MinYear = 1950;

    /// <summary>Elementos por persona (R-15 y D-05): el 21.º responde 409 <see cref="LimitReachedCode"/>.</summary>
    public const int MaxItemsPerPerson = 20;

    public const string LimitReachedCode = "item_limit_reached";

    /// <summary>El año más alto que se acepta: el año actual más uno (R-14), con la hora UTC del servidor.</summary>
    public static int MaxYear(DateTimeOffset now) => now.UtcDateTime.Year + 1;

    /// <summary>Nombre sin espacios en los extremos y sin caracteres de control; <see langword="null"/> si no cumple los 2 a 60 caracteres.</summary>
    public static string? NormalizeName(string? value)
    {
        var name = value?.Trim();
        return name is { Length: >= NameMinLength and <= NameMaxLength } && !name.Any(char.IsControl) ? name : null;
    }
}

/// <summary>Un auto o una cosa de la casa de una persona (spec 041, §6). <c>OwnerId</c> es el <c>sub</c> del token; nunca sale en una respuesta.</summary>
internal sealed record HomeAndCarItem(ObjectId Id, string OwnerId, string Type, string Name, int? Year, DateTime CreatedAt)
{
    /// <summary>Sin datos: un registro del elemento imprimiría el nombre que escribió la persona (spec 011).</summary>
    public sealed override string ToString() => nameof(HomeAndCarItem);
}

/// <summary>Resultado de registrar un elemento: se guardó o la persona ya tenía el máximo (spec 041, R-15).</summary>
internal enum AddItemOutcome
{
    Created,
    LimitReached,
}
