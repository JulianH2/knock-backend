using System.Globalization;
using System.Text;
using Knock.Market.Api.Common.Errors;
using Microsoft.AspNetCore.WebUtilities;
using MongoDB.Bson;

namespace Knock.Market.Api.Modules.Trust.Domain;

/// <summary>Códigos de error de negocio de los técnicos de confianza (spec 042, §5). En inglés y <c>snake_case</c>, sin datos de nadie.</summary>
internal static class TrustErrorCodes
{
    /// <summary>Mismo código que da Técnicos (spec 046): no se declara dos veces (spec 002, CA-07).</summary>
    public const string TechnicianNotFound = Knock.Market.Api.Modules.Tecnicos.Domain.TechnicianRules.TechnicianNotFoundCode;
    public const string TrustedTechnicianNotFoundCode = "trusted_technician_not_found";
    public const string InvalidCursorCode = "invalid_trusted_cursor";
}

/// <summary>
/// Cursor opaco de la lista de técnicos de confianza (spec 042, R-02): la fecha de guardado y el identificador del último elemento devuelto, en texto base64url. Un valor que no se
/// puede leer es un 400, igual que los demás cursores del servicio.
/// </summary>
internal static class TrustCursor
{
    public static string Encode(DateTime savedAtUtc, ObjectId id) =>
        WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(string.Create(CultureInfo.InvariantCulture, $"{savedAtUtc.Ticks};{id}")));

    public static (DateTime SavedAtUtc, ObjectId Id) Decode(string cursor)
    {
        ArgumentException.ThrowIfNullOrEmpty(cursor);
        try
        {
            var parts = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(cursor)).Split(';');
            if (parts.Length == 2
                && long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)
                && ticks <= DateTime.MaxValue.Ticks
                && ObjectId.TryParse(parts[1], out var id))
            {
                return (new DateTime(ticks, DateTimeKind.Utc), id);
            }
        }
        catch (FormatException)
        {
            // Se responde igual que con cualquier otro cursor ilegible.
        }
        catch (DecoderFallbackException)
        {
            // Bytes que no son texto: mismo caso.
        }

        throw new DomainException(DomainErrorKind.BadRequest, "The cursor is not valid.", TrustErrorCodes.InvalidCursorCode);
    }
}
