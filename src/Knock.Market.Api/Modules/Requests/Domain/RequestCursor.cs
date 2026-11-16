using System.Globalization;
using System.Text;
using Knock.Market.Api.Common.Errors;
using Microsoft.AspNetCore.WebUtilities;
using MongoDB.Bson;

namespace Knock.Market.Api.Modules.Requests.Domain;

/// <summary>
/// Cursor opaco de <c>GET /solicitudes</c> (spec 020, R-09): Base64url de la fecha de creación (milisegundos UTC) y el <c>_id</c> de la última fila. Cualquier cosa que no
/// se pueda decodificar a exactamente eso responde 400 <c>invalid_cursor</c>, sin repetir lo recibido.
/// </summary>
internal static class RequestCursor
{
    private const char _separator = ':';

    public static string Encode(DateTime createdAtUtc, ObjectId id) =>
        WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(
            string.Create(CultureInfo.InvariantCulture, $"{new DateTimeOffset(createdAtUtc, TimeSpan.Zero).ToUnixTimeMilliseconds()}{_separator}{id}")));

    public static (DateTime CreatedAtUtc, ObjectId Id) Decode(string cursor)
    {
        try
        {
            var text = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(cursor));
            var parts = text.Split(_separator);
            if (parts.Length == 2
                && long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var milliseconds)
                && ObjectId.TryParse(parts[1], out var id))
            {
                return (DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime, id);
            }
        }
        catch (FormatException)
        {
            // Se responde igual que con cualquier otro cursor ilegible.
        }
        catch (ArgumentOutOfRangeException)
        {
            // Milisegundos fuera del rango de fechas.
        }

        throw new DomainException(DomainErrorKind.BadRequest, "The cursor is not valid.", RequestErrorCodes.InvalidCursorCode);
    }
}
