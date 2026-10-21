using System.Diagnostics;
using AwesomeAssertions;
using Knock.Spike.DocumentDb.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;
using Xunit.Abstractions;

namespace Knock.Spike.DocumentDb;

/// <summary>
/// P6: índice TTL sobre la expiración de tokens revocados. El monitor TTL de MongoDB corre cada
/// <c>ttlMonitorSleepSecs</c> (60 s por defecto); se lee el parámetro cuando el servidor lo expone y se mide el tiempo real.
/// </summary>
public sealed class DocumentDbTtlTests(ITestOutputHelper output)
{
    private static readonly TimeSpan _maxWait = TimeSpan.FromMinutes(3);

    [Theory]
    [LocalTargets]
    [AzureTarget]
    public async Task ExpiredRevokedTokenIsDeletedByTtlIndex_TC_TT_BD_05_06(string target)
    {
        await using var spike = await SpikePool.CreateDatabaseAsync(target);
        var tokens = spike.Database.GetCollection<BsonDocument>(SpikeSeeds.TokensRevocados);
        await tokens.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("expiraEn"),
            new CreateIndexOptions { ExpireAfter = TimeSpan.Zero }));
        var now = DateTime.UtcNow;
        await tokens.InsertManyAsync(
        [
            new BsonDocument { { "jti", "vencido" }, { "expiraEn", now.AddSeconds(-1) } },
            new BsonDocument { { "jti", "vigente" }, { "expiraEn", now.AddHours(1) } },
        ]);
        var monitor = await ReadTtlMonitorSecondsAsync(spike.Client);

        var elapsed = Stopwatch.StartNew();
        while (elapsed.Elapsed < _maxWait && await tokens.CountDocumentsAsync(new BsonDocument("jti", "vencido")) > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        output.WriteLine($"[{target}] ttlMonitorSleepSecs: {monitor} · borrado tras {elapsed.Elapsed.TotalSeconds:0} s");
        (await tokens.CountDocumentsAsync(new BsonDocument("jti", "vencido"))).Should().Be(0);
        (await tokens.CountDocumentsAsync(new BsonDocument("jti", "vigente"))).Should().Be(1);
    }

    private static async Task<string> ReadTtlMonitorSecondsAsync(MongoClient client)
    {
        try
        {
            var result = await client.GetDatabase("admin").RunCommandAsync<BsonDocument>(
                new BsonDocument { { "getParameter", 1 }, { "ttlMonitorSleepSecs", 1 } });
            return result.GetValue("ttlMonitorSleepSecs", "no expuesto").ToString()!;
        }
        catch (MongoCommandException)
        {
            return "no expuesto";
        }
    }
}
