using System.Globalization;
using AwesomeAssertions;
using Knock.Api.Modules.Identity.Data;
using Knock.Api.Modules.Identity.Domain;
using Knock.DatabaseTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.DatabaseTest.Identity;

public sealed class IdentityIndexTests
{
    [Fact]
    public async Task CollectionsHaveDeclaredIndexesAndInitializerIsIdempotent_TC_HU_001_22()
    {
        await using var database = await IdentityDatabase.CreateAsync(withIndexes: false);

        await database.Store.EnsureIndexesAsync(CancellationToken.None);
        await database.Store.EnsureIndexesAsync(CancellationToken.None);

        (await IndexesAsync(database.Store.Users)).Should().BeEquivalentTo("_id:1", "sub:1 unique", "celularHash:1 unique");
        (await IndexesAsync(database.Store.Consents)).Should().BeEquivalentTo("_id:1", "usuarioId:1,documento:1,version:1 unique");
        (await IndexesAsync(database.Store.Audit)).Should().BeEquivalentTo("_id:1", "usuarioId:1,ocurridoEn:-1");
        (await IndexesAsync(database.Store.RefreshTokens)).Should().BeEquivalentTo("_id:1", "huella:1 unique", "venceEn:1 ttl0");
    }

    [Fact]
    public async Task DuplicateSubOrPhoneHashIsRejectedByUniqueIndex_TC_HU_001_10()
    {
        await using var database = await IdentityDatabase.CreateAsync();
        var (first, _) = database.BuildAccount();
        var (sameSub, _) = database.BuildAccount(IdentityDatabase.OtherPhone, sub: first.Sub);
        var (samePhone, _) = database.BuildAccount(sub: Guid.NewGuid().ToString("D"));

        var created = await database.Store.CreateAccountAsync(first, CancellationToken.None);
        var duplicatedSub = await database.Store.CreateAccountAsync(sameSub, CancellationToken.None);
        var duplicatedPhone = await database.Store.CreateAccountAsync(samePhone, CancellationToken.None);

        created.Should().Be(AccountCreation.Created);
        duplicatedSub.Should().Be(AccountCreation.AlreadyExists, "el índice único de sub rechaza al segundo");
        duplicatedPhone.Should().Be(AccountCreation.AlreadyExists, "el índice único de celularHash rechaza al segundo");
        (await database.CountsAsync()).Should().BeEquivalentTo(new Dictionary<string, long>
        {
            [IdentityStore.UsersCollection] = 1,
            [IdentityStore.ConsentsCollection] = 2,
            [IdentityStore.AuditCollection] = 1,
            [IdentityStore.RefreshTokensCollection] = 1,
        }, "los rechazados no dejan nada, ni siquiera consentimientos o auditoría");
    }

    /// <summary>Cada índice como texto: <c>campo:tipo</c> separados por comas, «unique» y «ttl0» si aplica (los números como enteros).</summary>
    private static async Task<List<string>> IndexesAsync(IMongoCollection<BsonDocument> collection)
    {
        var indexes = await (await collection.Indexes.ListAsync()).ToListAsync();
        return [.. indexes.Select(index =>
        {
            var key = string.Join(',', index["key"].AsBsonDocument.Select(e => e.Name + ":" + e.Value.ToInt32().ToString(CultureInfo.InvariantCulture)));
            var unique = index.GetValue("unique", false).ToBoolean() ? " unique" : string.Empty;
            var ttl = index.Contains("expireAfterSeconds") && index["expireAfterSeconds"].ToInt64() == 0 ? " ttl0" : string.Empty;
            return key + unique + ttl;
        })];
    }
}
