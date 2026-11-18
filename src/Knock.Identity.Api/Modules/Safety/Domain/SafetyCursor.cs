using Knock.Identity.Api.Common.Errors;
using MongoDB.Bson;

namespace Knock.Identity.Api.Modules.Safety.Domain;

/// <summary>
/// Cursor de las listas del módulo (bloqueos y reportes): el <c>_id</c> del último elemento devuelto, que crece con el tiempo y por eso sirve para ordenar del más reciente al más
/// antiguo. Un valor que no se puede leer es un 400 (<c>invalid_cursor</c>), igual que los demás cursores del servicio.
/// </summary>
internal static class SafetyCursor
{
    public static ObjectId? Parse(string? cursor)
    {
        if (string.IsNullOrEmpty(cursor))
        {
            return null;
        }

        return cursor.Length == 24 && ObjectId.TryParse(cursor, out var id)
            ? id
            : throw new DomainException(DomainErrorKind.BadRequest, "The cursor is not valid.", SafetyErrorCodes.InvalidCursorCode);
    }
}
