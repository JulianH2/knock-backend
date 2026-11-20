using MongoDB.Driver;

namespace Knock.Identity.Api.Hosting;

/// <summary>Clasificación de errores de escritura de MongoDB que varios módulos necesitan (clave duplicada y conflicto de escritura en transacciones).</summary>
internal static class MongoWriteErrors
{
    /// <summary>Código de «write conflict» de MongoDB y documentdb-local para escrituras concurrentes en transacciones.</summary>
    public const int WriteConflictCode = 112;

    /// <summary>Pausa técnica antes de reintentar una transacción que chocó con otra (50 ms por intento); no es un plazo de negocio.</summary>
    public static TimeSpan WriteConflictBackoff(int attempt) => TimeSpan.FromMilliseconds(50 * attempt);

    public static bool IsDuplicateKey(Exception exception) => exception switch
    {
        MongoWriteException write => write.WriteError.Category == ServerErrorCategory.DuplicateKey,
        MongoBulkWriteException bulk => bulk.WriteErrors.Any(error => error.Category == ServerErrorCategory.DuplicateKey),
        MongoCommandException command => command.Code == 11000,
        _ => false,
    };
}
