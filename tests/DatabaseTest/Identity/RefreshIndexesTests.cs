using System.Globalization;
using AwesomeAssertions;
using Knock.DatabaseTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.DatabaseTest.Identity;

public sealed class RefreshIndexesTests
{
    [Fact]
    public async Task IndexesExistAndAreIdempotent_TC_HU_003_13()
    {
        await using var database = await IdentityDatabase.CreateAsync(withIndexes: false);

        await database.Store.EnsureIndexesAsync(CancellationToken.None);
        await database.Store.EnsureIndexesAsync(CancellationToken.None);

        var indexes = await (await database.Store.RefreshTokens.Indexes.ListAsync()).ToListAsync();
        var described = indexes.Select(index =>
        {
            var key = string.Join(',', index["key"].AsBsonDocument.Select(e => e.Name + ":" + e.Value.ToInt32().ToString(CultureInfo.InvariantCulture)));
            var unique = index.GetValue("unique", false).ToBoolean() ? " unique" : string.Empty;
            var ttl = index.Contains("expireAfterSeconds") && index["expireAfterSeconds"].ToInt64() == 0 ? " ttl0" : string.Empty;
            return key + unique + ttl;
        }).ToList();
        described.Should().BeEquivalentTo("_id:1", "huella:1 unique", "familiaId:1", "venceEn:1 ttl0");
    }
}
