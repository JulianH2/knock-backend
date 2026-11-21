using System.Net;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Knock.UnitTest.Infrastructure;

/// <summary>Errores de comando del servidor construidos con el constructor público del driver, para probar la relectura ante «llave duplicada», conflicto de escritura (112) e interbloqueo (24).</summary>
internal static class MongoConflictErrors
{
    public const int DuplicateKey = 11000;
    public const int WriteConflict = 112;
    public const int LockError = 24;
    public const string DeadlockMessage = "Command update failed: Could not acquire lock for operation due to deadlock.";

    public static MongoCommandException Command(int code) => Command(code, $"error {code}");

    /// <summary>Error de comando con mensaje propio, p. ej. el interbloqueo (24) que documentdb-local da a escrituras simultáneas sobre la misma clave única.</summary>
    public static MongoCommandException Command(int code, string message) => new(
        new ConnectionId(new ServerId(new ClusterId(1), new DnsEndPoint("localhost", 27017))),
        message,
        new BsonDocument("insert", "x"),
        new BsonDocument { { "ok", 0 }, { "code", code } });
}
