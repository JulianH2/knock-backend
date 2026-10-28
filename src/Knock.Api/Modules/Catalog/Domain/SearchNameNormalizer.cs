using System.Globalization;
using System.Text;

namespace Knock.Api.Modules.Catalog.Domain;

/// <summary>
/// Calcula <c>nombreBusqueda</c> (spike P5): sin marcas diacríticas, en mayúsculas invariantes y con los espacios recortados y
/// colapsados. Toda escritura de oficios o colonias debe usarlo. La «ñ» queda como «N» (límite conocido).
/// </summary>
internal static class SearchNameNormalizer
{
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
