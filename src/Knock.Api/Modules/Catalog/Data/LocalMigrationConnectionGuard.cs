using Npgsql;

namespace Knock.Api.Modules.Catalog.Data;

internal static class LocalMigrationConnectionGuard
{
    public static void EnsureLocalHost(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        var host = new NpgsqlConnectionStringBuilder(connectionString).Host;
        if (!string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(host, "127.0.0.1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The local EF migration command only accepts localhost or 127.0.0.1.");
        }
    }
}
