using AwesomeAssertions;
using Knock.Spike.DocumentDb.Support;
using MongoDB.Bson;
using Xunit;
using Xunit.Abstractions;

namespace Knock.Spike.DocumentDb;

/// <summary>
/// P6: índice TTL sobre la expiración de tokens revocados. El monitor TTL de MongoDB corre cada
/// <c>ttlMonitorSleepSecs</c> (60 s por defecto); se lee el parámetro cuando el servidor lo expone y se mide el tiempo real.
/// </summary>
public sealed class DocumentDbTtlTests(ITestOutputHelper output)
{
    internal static readonly TimeSpan MaxWait = TimeSpan.FromMinutes(3);

    [Theory]
    [LocalTargets]
    [AzureTarget]
    public async Task ExpiredRevokedTokenIsDeletedByTtlIndex_TC_TT_BD_05_06(string target)
    {
        await using var spike = await SpikePool.CreateDatabaseAsync(target);
        var tokens = await SpikeQueries.TokensWithTtlAsync(spike.Database);
        var monitor = await SpikeQueries.ReadTtlMonitorSecondsAsync(spike.Client);

        var elapsed = await SpikeQueries.WaitForTtlDeletionAsync(tokens, MaxWait);

        output.WriteLine($"[{target}] ttlMonitorSleepSecs: {monitor} · borrado tras {elapsed.TotalSeconds:0} s");
        (await tokens.CountDocumentsAsync(new BsonDocument("jti", "vencido"))).Should().Be(0);
        (await tokens.CountDocumentsAsync(new BsonDocument("jti", "vigente"))).Should().Be(1);
    }
}
