using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.Api.Modules.Identity.Data;
using Knock.Api.Modules.Identity.Domain;
using Knock.DatabaseTest.Support;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.DatabaseTest.Identity;

/// <summary>HU-003 §5: rotación atómica de <c>refresh_tokens</c> sobre documentdb-local. Solo datos ficticios.</summary>
public sealed class RefreshRotationTests
{
    private static readonly DateTime _now = new(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);

    private static byte[] IndependentSha256(string token) => SHA256.HashData(Base64UrlEncoder.DecodeBytes(token));

    [Fact]
    public async Task RotationLinksTokensAndStoresOnlyFingerprint_TC_HU_003_02()
    {
        await using var database = await IdentityDatabase.CreateAsync();
        var (account, refreshToken) = database.BuildAccount();
        (await database.Store.CreateAccountAsync(account, CancellationToken.None)).Should().Be(AccountCreation.Created);
        var nextToken = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));

        var rotation = await database.Store.RotateAsync(IndependentSha256(refreshToken), IndependentSha256(nextToken), _now, CancellationToken.None);

        rotation.Status.Should().Be(RotationStatus.Rotated);
        rotation.Parent.Should().Be(new StoredRefresh(account.Id, account.Refresh.FamilyId, account.Refresh.DeviceId));
        var documents = await database.Store.RefreshTokens.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        documents.Should().HaveCount(2, "el reemplazado se conserva hasta venceEn para detectar el reuso");
        var parent = documents.Single(d => d[IdentityStore.FingerprintField].AsBsonBinaryData.Bytes.AsSpan().SequenceEqual(IndependentSha256(refreshToken)));
        var child = documents.Single(d => d[IdentityStore.FingerprintField].AsBsonBinaryData.Bytes.AsSpan().SequenceEqual(IndependentSha256(nextToken)));
        parent[IdentityStore.ReplacedByField].AsObjectId.Should().Be(child[IdentityStore.IdField].AsObjectId);
        child[IdentityStore.FamilyIdField].Should().Be(parent[IdentityStore.FamilyIdField]);
        child[IdentityStore.DeviceIdField].Should().Be(parent[IdentityStore.DeviceIdField]);
        child[IdentityStore.UserIdField].Should().Be(parent[IdentityStore.UserIdField]);
        child[IdentityStore.ExpiresAtField].ToUniversalTime().Should().Be(_now.AddDays(30), "la vigencia se reinicia en cada renovación");
        child.GetValue(IdentityStore.ReplacedByField, BsonNull.Value).IsBsonNull.Should().BeTrue();
        child.GetValue(IdentityStore.RevokedAtField, BsonNull.Value).IsBsonNull.Should().BeTrue();
        foreach (var document in documents)
        {
            var text = document.ToJson();
            text.Should().NotContain(refreshToken).And.NotContain(nextToken, "nunca se guarda el token, solo su huella");
        }
    }

    [Fact]
    public async Task ReplacedTokenIsReused_RevokesTheWholeFamily_TC_HU_003_03()
    {
        await using var database = await IdentityDatabase.CreateAsync();
        var (account, refreshToken) = database.BuildAccount();
        await database.Store.CreateAccountAsync(account, CancellationToken.None);
        var nextToken = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        await database.Store.RotateAsync(IndependentSha256(refreshToken), IndependentSha256(nextToken), _now, CancellationToken.None);

        var reused = await database.Store.RotateAsync(IndependentSha256(refreshToken), IndependentSha256(Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32))), _now, CancellationToken.None);

        reused.Status.Should().Be(RotationStatus.Reused);
        var documents = await database.Store.RefreshTokens.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        documents.Should().HaveCount(2, "el reuso no crea ningún hijo");
        documents.Should().OnlyContain(d => d[IdentityStore.RevocationReasonField].AsString == "reuso");
        (await database.Store.RotateAsync(IndependentSha256(nextToken), IndependentSha256(refreshToken), _now, CancellationToken.None)).Status
            .Should().Be(RotationStatus.Reused, "el más nuevo también quedó revocado");
    }

    [Fact]
    public async Task UnknownOrExpiredToken_IsMissingAndWritesNothing_TC_HU_003_04()
    {
        await using var database = await IdentityDatabase.CreateAsync();
        var (account, refreshToken) = database.BuildAccount();
        await database.Store.CreateAccountAsync(account, CancellationToken.None);
        var future = _now.AddDays(31);

        var expired = await database.Store.RotateAsync(IndependentSha256(refreshToken), RandomNumberGenerator.GetBytes(32), future, CancellationToken.None);
        var unknown = await database.Store.RotateAsync(RandomNumberGenerator.GetBytes(32), RandomNumberGenerator.GetBytes(32), _now, CancellationToken.None);

        expired.Status.Should().Be(RotationStatus.Missing);
        unknown.Status.Should().Be(RotationStatus.Missing);
        var documents = await database.Store.RefreshTokens.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        documents.Should().ContainSingle();
        documents[0].GetValue(IdentityStore.ReplacedByField, BsonNull.Value).IsBsonNull.Should().BeTrue();
        documents[0].GetValue(IdentityStore.RevokedAtField, BsonNull.Value).IsBsonNull.Should().BeTrue();
    }
}
