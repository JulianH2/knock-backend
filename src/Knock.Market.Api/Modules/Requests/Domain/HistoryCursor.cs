using System.Globalization;
using System.Text;
using Knock.Market.Api.Common.Errors;
using Microsoft.AspNetCore.WebUtilities;
using MongoDB.Bson;

namespace Knock.Market.Api.Modules.Requests.Domain;

/// <summary>
/// Cursor opaco del historial de un elemento (spec 041, R-13 y R-17): Base64url del día de la última visita (<c>YYYY-MM-DD</c>) y su <c>_id</c>. Cualquier cosa que no se pueda
/// decodificar a exactamente eso responde 400 <c>invalid_cursor</c>, sin repetir lo recibido.
/// </summary>
internal static class HistoryCursor
{
    private const char _separator = ':';

    public static string Encode(string day, ObjectId id) => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes($"{day}{_separator}{id}"));

    public static (string Day, ObjectId Id) Decode(string cursor)
    {
        try
        {
            var parts = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(cursor)).Split(_separator);
            if (parts.Length == 2
                && DateOnly.TryParseExact(parts[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
                && ObjectId.TryParse(parts[1], out var id))
            {
                return (parts[0], id);
            }
        }
        catch (FormatException)
        {
            // Se responde igual que con cualquier otro cursor ilegible.
        }

        throw new DomainException(DomainErrorKind.BadRequest, "The cursor is not valid.", RequestErrorCodes.InvalidCursorCode);
    }
}
