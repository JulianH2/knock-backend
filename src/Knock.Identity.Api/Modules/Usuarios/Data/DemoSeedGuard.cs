using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Identity.Api.Modules.Usuarios.Data;

internal static class DemoSeedGuard
{
    public static async Task ValidateAsync(string? connection, IMongoCollection<BsonDocument> users, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(connection) || connection.StartsWith("mongodb+srv://", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Demo seed requires a local MongoDB connection without SRV.");
        }

        MongoUrl url;
        try
        {
            url = MongoUrl.Create(connection);
        }
        catch (Exception error) when (error is MongoConfigurationException or ArgumentException or FormatException)
        {
            throw new InvalidOperationException("Demo seed requires a valid local MongoDB connection.");
        }

        if (!url.Servers.Any() || url.Servers.Any(server => !IsLocal(server.Host)))
        {
            throw new InvalidOperationException("Demo seed requires exclusively local MongoDB hosts.");
        }

        if (await users.Find(Builders<BsonDocument>.Filter.Ne("demo", true)).Limit(1).AnyAsync(cancellationToken))
        {
            throw new InvalidOperationException("Demo seed cannot run with non-demo users.");
        }
    }

    private static bool IsLocal(string host) =>
        string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
        || string.Equals(host, "127.0.0.1", StringComparison.Ordinal)
        || string.Equals(host, "documentdb-local", StringComparison.OrdinalIgnoreCase);
}
