using System.Text.RegularExpressions;
using Knock.Identity.Api.Modules.Addresses.Dtos;

namespace Knock.Identity.Api.Modules.Addresses.Services;

/// <summary>
/// Reglas de los campos de la dirección (spec 040, R-10) y su normalización: se recortan los espacios y una referencia vacía equivale a no tenerla. Ningún mensaje repite el
/// valor enviado (R-16).
/// </summary>
internal static partial class AddressRules
{
    public const int StreetMinLength = 8;
    public const int ColoniaMaxLength = 80;
    public const int ReferencesMaxLength = 200;

    /// <summary>Máximo de direcciones por persona (spec 040, R-12 y D-04): tope de cordura contra abuso de almacenamiento.</summary>
    public const int MaxPerPerson = 10;

    // Mismo patrón del nombre de colonia que acepta el catálogo de Mercado (GET /colonias/existe): una colonia que no lo cumple no puede existir allí.
    [GeneratedRegex(@"^[\p{L}\p{M}\p{N}][\p{L}\p{M}\p{N} .,'’()/#-]*$", RegexOptions.None, matchTimeoutMilliseconds: 200)]
    private static partial Regex ColoniaPattern();

    public static string Normalize(string? value) => value?.Trim() ?? string.Empty;

    public static bool HasControlCharacters(string value) => value.Any(char.IsControl);

    public static bool IsColoniaName(string value) => value.Length <= ColoniaMaxLength && ColoniaPattern().IsMatch(value);
}

/// <summary>Los datos de una dirección ya validados y recortados. Sin <c>ToString</c> con datos: un registro imprimiría la dirección.</summary>
internal sealed record AddressInput(string Street, string Colonia, string? References, bool? Principal)
{
    public static AddressInput From(CreateAddressRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new AddressInput(
            AddressRules.Normalize(request.CalleNumero),
            AddressRules.Normalize(request.Colonia),
            AddressRules.Normalize(request.Referencias) is { Length: > 0 } references ? references : null,
            request.Principal);
    }

    public sealed override string ToString() => nameof(AddressInput);
}

/// <summary>
/// Los campos que una edición cambia, ya recortados: <see langword="null"/> deja el campo igual. En <see cref="References"/>, un texto vacío las borra. Sin <c>ToString</c> con
/// datos: un registro imprimiría la dirección.
/// </summary>
internal sealed record AddressEdit(string? Street, string? Colonia, string? References, bool? Principal)
{
    public static AddressEdit From(UpdateAddressRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new AddressEdit(
            request.CalleNumero is null ? null : AddressRules.Normalize(request.CalleNumero),
            request.Colonia is null ? null : AddressRules.Normalize(request.Colonia),
            request.Referencias is null ? null : AddressRules.Normalize(request.Referencias),
            request.Principal);
    }

    public sealed override string ToString() => nameof(AddressEdit);
}
