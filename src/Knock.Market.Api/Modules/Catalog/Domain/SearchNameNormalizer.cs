using System.Globalization;
using System.Text;

namespace Knock.Market.Api.Modules.Catalog.Domain;

/// <summary>
/// Calcula <c>nombreBusqueda</c> (spike P5): sin marcas diacríticas, en mayúsculas invariantes y con los espacios recortados y
/// colapsados. Toda escritura de oficios o colonias debe usarlo. La «ñ» queda como «N» (límite conocido).
/// </summary>
internal static class SearchNameNormalizer
{
    public static string NormalizeQuery(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var result = new StringBuilder(text.Length);
        var separator = false;
        foreach (var original in text.Normalize(NormalizationForm.FormC))
        {
            var character = char.ToLowerInvariant(original);
            if (character == 'ñ')
            {
                if (separator && result.Length > 0) result.Append(' ');
                separator = false;
                result.Append(character);
                continue;
            }

            var decomposed = character.ToString().Normalize(NormalizationForm.FormD);
            var baseCharacter = decomposed[0];
            if (!char.IsLetterOrDigit(baseCharacter))
            {
                separator = true;
                continue;
            }

            if (separator && result.Length > 0) result.Append(' ');
            separator = false;
            result.Append(baseCharacter);
        }

        return result.ToString();
    }

    public static string Normalize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var pendingSpace = false;
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(c);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }
}
