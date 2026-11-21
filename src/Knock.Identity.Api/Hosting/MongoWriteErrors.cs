using MongoDB.Driver;

namespace Knock.Identity.Api.Hosting;

/// <summary>Clasificación de errores de escritura de MongoDB que varios módulos necesitan (clave duplicada y conflicto de escritura en transacciones).</summary>
internal static class MongoWriteErrors
{
    /// <summary>Código de «write conflict» de MongoDB y documentdb-local para escrituras concurrentes en transacciones.</summary>
    public const int WriteConflictCode = 112;

    /// <summary>Pausa técnica antes de reintentar una transacción que chocó con otra (50 ms por intento); no es un plazo de negocio.</summary>
    public static TimeSpan WriteConflictBackoff(int attempt) => TimeSpan.FromMilliseconds(50 * attempt);

    /// <summary>Intentos máximos de una escritura que choca con otra simultánea (la misma cota que usan los demás almacenes).</summary>
    public const int MaxWriteConflictAttempts = 6;

    /// <summary>«Conflicto de escritura» (112) en un comando o en una escritura suelta; no incluye la etiqueta de transacción transitoria.</summary>
    public static bool IsWriteConflict(Exception exception) =>
        exception is MongoCommandException { Code: WriteConflictCode } or MongoWriteException { WriteError.Code: WriteConflictCode };

    /// <summary>
    /// Inserta un documento cuya clave única decide una carrera. Ante «llave duplicada» o conflicto de escritura (112) relee por esa clave: si ya existe, la otra petición ganó y devuelve
    /// <see langword="false"/>; si no existe, reintenta con espera creciente y, agotados los intentos, relanza la excepción. Devuelve <see langword="true"/> si insertó.
    /// </summary>
    public static async Task<bool> InsertIfAbsentAsync(Func<Task> insert, Func<Task<bool>> exists, Func<TimeSpan, Task> wait)
    {
        var attempt = 0;
        while (true)
        {
            attempt++;
            try
            {
                await insert();
                return true;
            }
            catch (Exception exception) when (IsDuplicateKey(exception) || IsWriteConflict(exception))
            {
                if (await exists())
                {
                    return false;
                }

                if (attempt >= MaxWriteConflictAttempts)
                {
                    throw;
                }

                await wait(WriteConflictBackoff(attempt));
            }
        }
    }

    public static bool IsDuplicateKey(Exception exception) => exception switch
    {
        MongoWriteException write => write.WriteError.Category == ServerErrorCategory.DuplicateKey,
        MongoBulkWriteException bulk => bulk.WriteErrors.Any(error => error.Category == ServerErrorCategory.DuplicateKey),
        MongoCommandException command => command.Code == 11000,
        _ => false,
    };
}
