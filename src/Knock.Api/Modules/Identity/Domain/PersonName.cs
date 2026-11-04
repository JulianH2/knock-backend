using System.Globalization;
using System.Text;

namespace Knock.Api.Modules.Identity.Domain;

/// <summary>
/// Nombre de una persona (spec HU-002, §4 y duda 2): de 2 a 60 caracteres que son letras, espacios, guion y apóstrofo (<c>'</c> o <c>’</c>). Se
/// normaliza a NFC y con un solo espacio entre palabras. Cualquier otro carácter (números, símbolos, caracteres de control o invisibles) se rechaza: la lista es
/// cerrada, no una lista de prohibidos.
/// </summary>
internal static class PersonName
{
    public const int MinLength = 2;
    public const int MaxLength = 60;

    /// <returns><c>true</c> si el nombre es válido; <paramref name="normalized"/> trae la forma que se guarda (vacío si es inválido).</returns>
    public static bool TryNormalize(string? input, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        string composed;
        try
        {
            composed = input.Normalize(NormalizationForm.FormC);
        }
        catch (ArgumentException)
        {
            // Un sustituto Unicode sin pareja no se puede normalizar: no es un nombre.
            return false;
        }

        var builder = new StringBuilder(composed.Length);
        var letters = 0;
        var pendingSpace = false;
        foreach (var character in composed)
        {
            if (char.GetUnicodeCategory(character) == UnicodeCategory.SpaceSeparator)
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (char.IsLetter(character))
            {
                letters++;
            }
            else if (character is not ('-' or '\'' or '’'))
            {
                return false;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(character);
        }

        if (letters < MinLength || builder.Length > MaxLength || !char.IsLetter(builder[0]))
        {
            return false;
        }

        normalized = builder.ToString();
        return true;
    }
}
