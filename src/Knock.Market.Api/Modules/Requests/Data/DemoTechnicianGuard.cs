using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Market.Api.Modules.Requests.Data;

// Mirrors Identity's local-only demo guard without a service-to-service reference.
internal static class DemoTechnicianGuard
{
    public static async Task ValidateAsync(string? connection, IMongoCollection<BsonDocument> technicians, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(connection) || connection.StartsWith("mongodb+srv://", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Demo seed requires a local MongoDB connection without SRV.");

        MongoUrl url;
        try { url = MongoUrl.Create(connection); }
        catch (Exception error) when (error is MongoConfigurationException or ArgumentException or FormatException)
        {
            throw new InvalidOperationException("Demo seed requires a valid local MongoDB connection.");
        }

        if (!url.Servers.Any() || url.Servers.Any(server => !IsLocal(server.Host)))
            throw new InvalidOperationException("Demo seed requires exclusively local MongoDB hosts.");
        if (await technicians.Find(Builders<BsonDocument>.Filter.Ne("demo", true)).Limit(1).AnyAsync(cancellationToken))
            throw new InvalidOperationException("Demo seed cannot run with non-demo technicians.");
    }

    private static bool IsLocal(string host) =>
        string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
        || string.Equals(host, "127.0.0.1", StringComparison.Ordinal)
        || string.Equals(host, "documentdb-local", StringComparison.OrdinalIgnoreCase);
}
