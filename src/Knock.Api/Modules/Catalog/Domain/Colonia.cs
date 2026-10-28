namespace Knock.Api.Modules.Catalog.Domain;

/// <summary>
/// Colonia SEPOMEX tal como la devuelve la consulta interna de colonia por punto: <c>{ id, nombre, cp, municipio }</c>.
/// <c>Id</c> es el <c>_id</c> fijo de <c>colonias.json</c> en 24 caracteres hexadecimales.
/// </summary>
internal sealed record Colonia(string Id, string Nombre, string Cp, string Municipio)
{
    public const int MaxNombreLength = 150;
    public const int MaxMunicipioLength = 100;
    public const int PostalCodeLength = 5;

    /// <summary>Exactamente cinco dígitos ASCII (<c>^[0-9]{5}$</c>; <c>\d</c> aceptaría otros dígitos Unicode).</summary>
    public static bool IsPostalCode(string? value) =>
        value is { Length: PostalCodeLength } && value.All(char.IsAsciiDigit);
}
