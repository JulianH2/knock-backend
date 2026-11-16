using System.Text.RegularExpressions;
using Knock.Identity.Api.Modules.Usuarios.Domain;
using Knock.Identity.Api.Modules.Usuarios.Dtos;
using Microsoft.Extensions.Options;

namespace Knock.Identity.Api.Modules.Usuarios.Services;

/// <summary>Valores de <c>Perfil</c> en la configuración: los dominios desde los que se acepta una foto (almacenamiento o CDN de Knock).</summary>
internal sealed class ProfileOptions
{
    public const string SectionName = "Perfil";

    public string[] HostsFoto { get; set; } = [];
}

/// <summary>
/// Reglas y normalización de los campos del perfil (spec 017, R-05). Ningún mensaje repite el valor enviado.
/// </summary>
internal sealed partial class ProfileRules(IOptions<ProfileOptions> options)
{
    public const int NameMinLength = 2;
    public const int NameMaxLength = 80;
    public const int ColoniaMaxLength = 80;
    public const int PhotoUrlMaxLength = 2048;

    [GeneratedRegex(@"^[\p{L}\p{M}][\p{L}\p{M} '’-]*$", RegexOptions.None, matchTimeoutMilliseconds: 200)]
    private static partial Regex NamePattern();

    [GeneratedRegex(@"^[0-9]{5}$", RegexOptions.None, matchTimeoutMilliseconds: 200)]
    private static partial Regex PostalCodePattern();

    [GeneratedRegex(@"^[\p{L}\p{M}\p{N}][\p{L}\p{M}\p{N} .,'’()/#-]*$", RegexOptions.None, matchTimeoutMilliseconds: 200)]
    private static partial Regex ColoniaPattern();

    /// <summary>Valida y normaliza (recorta espacios). Devuelve los errores por campo en camelCase; vacío si no hay errores.</summary>
    public (ProfileUpdate? Update, Dictionary<string, string[]> Errors) Validate(UpdateProfileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var name = request.Nombre?.Trim() ?? string.Empty;
        var photo = Empty(request.FotoUrl);
        var colonia = Empty(request.Colonia);
        var postalCode = Empty(request.CodigoPostal);

        if (name.Length is < NameMinLength or > NameMaxLength || !NamePattern().IsMatch(name))
        {
            errors["nombre"] = [$"Must have {NameMinLength} to {NameMaxLength} characters: letters, spaces, hyphen and apostrophe."];
        }

        if (postalCode is not null && !PostalCodePattern().IsMatch(postalCode))
        {
            errors["codigoPostal"] = ["Must have 5 digits."];
        }

        if (colonia is not null && (colonia.Length > ColoniaMaxLength || !ColoniaPattern().IsMatch(colonia)))
        {
            errors["colonia"] = ["The value is not valid."];
        }

        if (photo is not null && !IsAllowedPhoto(photo))
        {
            errors["fotoUrl"] = ["Must be an https address of the Knock storage or CDN."];
        }

        return (errors.Count == 0 ? new ProfileUpdate(name, photo, colonia, postalCode) : null, errors);
    }

    private static string? Empty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private bool IsAllowedPhoto(string value) =>
        value.Length <= PhotoUrlMaxLength
        && !value.Any(character => char.IsControl(character) || char.IsWhiteSpace(character))
        && Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && string.IsNullOrEmpty(uri.UserInfo)
        && options.Value.HostsFoto.Contains(uri.IdnHost, StringComparer.OrdinalIgnoreCase);
}
