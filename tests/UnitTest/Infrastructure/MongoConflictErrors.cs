using System.Net;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Knock.UnitTest.Infrastructure;

/// <summary>Errores de comando del servidor construidos con el constructor público del driver, para probar la relectura ante «llave duplicada» y conflicto de escritura (112).</summary>
internal static class MongoConflictErrors
{
    public const int DuplicateKey = 11000;
    public const int WriteConflict = 112;

    public static MongoCommandException Command(int code) => new(
        new ConnectionId(new ServerId(new ClusterId(1), new DnsEndPoint("localhost", 27017))),
        $"error {code}",
        new BsonDocument("insert", "x"),
        new BsonDocument { { "ok", 0 }, { "code", code } });
}
