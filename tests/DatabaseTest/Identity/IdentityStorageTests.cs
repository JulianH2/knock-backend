using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Knock.Api.Modules.Identity.Data;
using Knock.Api.Modules.Identity.Domain;
using Knock.DatabaseTest.Support;
using Knock.UnitTest.Identity;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.DatabaseTest.Identity;

public sealed partial class IdentityStorageTests
{
    private static readonly string[] _collections =
        [IdentityStore.UsersCollection, IdentityStore.ConsentsCollection, IdentityStore.AuditCollection, IdentityStore.RefreshTokensCollection];

    [GeneratedRegex("^[a-z]+(_[a-z]+)*$")]
    private static partial Regex SnakeCasePlural();

    [GeneratedRegex("^[a-z][A-Za-z0-9]*$")]
    private static partial Regex CamelCase();

    [Fact]
    public async Task StoredUserDocumentNeverContainsPlainPhone_TC_HU_001_13()
    {
        await using var database = await IdentityDatabase.CreateAsync();
        var (account, _) = database.BuildAccount();
        await database.Store.CreateAccountAsync(account, CancellationToken.None);

        var stored = await database.Store.Users.Find(Builders<BsonDocument>.Filter.Eq(IdentityStore.IdField, ObjectId.Parse(account.Id)))
            .SingleAsync();

        var text = stored.ToJson();
        text.Should().NotContain("5512345678").And.NotContain(IdentityDatabase.Phone);
        text.Should().NotContain(Convert.ToBase64String(Encoding.UTF8.GetBytes(IdentityDatabase.Phone)));
        stored[IdentityStore.PhoneCiphertextVersionField].ToInt32().Should().Be(1, "la versión vigente de la llave de cifrado");
        stored[IdentityStore.PhoneHashVersionField].ToInt32().Should().Be(1);
        Decrypt(stored[IdentityStore.PhoneCiphertextField].AsBsonBinaryData.Bytes, account.Id).Should().Be(IdentityDatabase.Phone);
    }

    [Fact]
    public async Task UserIsFoundByPhoneHashWithoutDecrypting_TC_HU_001_14()
    {
        await using var database = await IdentityDatabase.CreateAsync();
        var (account, _) = database.BuildAccount();
        await database.Store.CreateAccountAsync(account, CancellationToken.None);

        var byHash = await database.Store.FindByPhoneHashAsync(database.Protector.Hash(IdentityDatabase.Phone).Value, CancellationToken.None);
        var bySub = await database.Store.FindBySubAsync(account.Sub, CancellationToken.None);
        var unknown = await database.Store.FindByPhoneHashAsync(database.Protector.Hash(IdentityDatabase.OtherPhone).Value, CancellationToken.None);

        byHash.Should().NotBeNull();
        byHash.Sub.Should().Be(account.Sub);
        byHash.Id.Should().Be(account.Id);
        byHash.State.Should().Be("activo");
        byHash.ProfileComplete.Should().BeFalse();
        bySub.Should().NotBeNull();
        bySub.Id.Should().Be(account.Id);
        unknown.Should().BeNull();
    }

    [Fact]
    public async Task OnlyTheFingerprintIsStoredNeverTheToken_TC_HU_001_16()
    {
        await using var database = await IdentityDatabase.CreateAsync();
        var (account, refreshToken) = database.BuildAccount();
        await database.Store.CreateAccountAsync(account, CancellationToken.None);
        var raw = Base64UrlRaw(refreshToken);
        var forms = new[]
        {
            refreshToken, Convert.ToBase64String(raw), Convert.ToHexStringLower(raw), Convert.ToHexString(raw),
        };

        foreach (var name in _collections)
        {
            var documents = await database.Database.GetCollection<BsonDocument>(name).Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            documents.Should().NotBeEmpty(name);
            documents.Select(d => d.ToJson()).Should().OnlyContain(json => forms.All(form => !json.Contains(form, StringComparison.Ordinal)),
                $"ninguna forma del refresh token aparece en {name}");
        }

        var refresh = await database.Store.RefreshTokens.Find(Builders<BsonDocument>.Filter.Empty).SingleAsync();
        refresh[IdentityStore.FingerprintField].AsBsonBinaryData.Bytes.Should().Equal(SHA256.HashData(raw), "SHA-256 de los 32 bytes del token");
        refresh[IdentityStore.ExpiresAtField].ToUniversalTime().Should().Be(refresh[IdentityStore.CreatedAtField].ToUniversalTime().AddDays(30));
        refresh.Contains("revocadoEn").Should().BeFalse("HU-003 agrega revocadoEn y reemplazadoPor");
        refresh.Contains("reemplazadoPor").Should().BeFalse();
    }

    [Fact]
    public async Task ExistingAccountExchangeOnlyAddsAFingerprint_TC_HU_001_16()
    {
        await using var database = await IdentityDatabase.CreateAsync();
        var (account, _) = database.BuildAccount();
        await database.Store.CreateAccountAsync(account, CancellationToken.None);
        var (next, _) = database.BuildAccount();

        await database.Store.AddRefreshTokenAsync(next.Refresh with { UserId = account.Id }, CancellationToken.None);

        var counts = await database.CountsAsync();
        counts[IdentityStore.UsersCollection].Should().Be(1);
        counts[IdentityStore.ConsentsCollection].Should().Be(2);
        counts[IdentityStore.AuditCollection].Should().Be(1);
        counts[IdentityStore.RefreshTokensCollection].Should().Be(2, "cada canje agrega otra familia de sesión");
        var families = await (await database.Store.RefreshTokens.DistinctAsync<ObjectId>(IdentityStore.FamilyIdField, Builders<BsonDocument>.Filter.Empty)).ToListAsync();
        families.Should().HaveCount(2);
    }

    [Fact]
    public async Task FailureOfTheLastWriteLeavesNoDocuments_TC_HU_001_17()
    {
        await using var database = await IdentityDatabase.CreateAsync();
        var (taken, _) = database.BuildAccount();
        await database.Store.CreateAccountAsync(taken, CancellationToken.None);
        var before = await database.CountsAsync();
        // La última escritura (la huella) choca con el índice único: las tres anteriores deben deshacerse.
        var (colliding, _) = database.BuildAccount(IdentityDatabase.OtherPhone, fingerprint: taken.Refresh.Fingerprint);

        var result = await database.Store.CreateAccountAsync(colliding, CancellationToken.None);

        result.Should().Be(AccountCreation.AlreadyExists);
        (await database.CountsAsync()).Should().BeEquivalentTo(before, "ni usuario, ni consentimientos, ni auditoría de la cuenta fallida");
        (await database.Store.FindBySubAsync(colliding.Sub, CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task CollectionsFollowNamingConventions_TC_HU_001_22()
    {
        await using var database = await IdentityDatabase.CreateAsync();
        var (account, _) = database.BuildAccount();
        await database.Store.CreateAccountAsync(account, CancellationToken.None);

        var names = await (await database.Database.ListCollectionNamesAsync()).ToListAsync();

        names.Should().BeEquivalentTo(_collections, "las cuatro colecciones de la spec, en snake_case plural");
        names.Should().OnlyContain(name => SnakeCasePlural().IsMatch(name));
        foreach (var name in _collections)
        {
            var document = await database.Database.GetCollection<BsonDocument>(name).Find(FilterDefinition<BsonDocument>.Empty).FirstAsync();
            document[IdentityStore.IdField].BsonType.Should().Be(BsonType.ObjectId, $"{name}._id es ObjectId");
            document.Names.Where(field => field != IdentityStore.IdField).Should().OnlyContain(field => CamelCase().IsMatch(field), $"campos de {name} en camelCase");
            document.Where(element => element.Name.EndsWith("En", StringComparison.Ordinal)).Should()
                .OnlyContain(element => element.Value.BsonType == BsonType.DateTime, $"las fechas de {name} son fechas UTC");
        }
    }

    private static byte[] Base64UrlRaw(string token) => Microsoft.IdentityModel.Tokens.Base64UrlEncoder.DecodeBytes(token);

    /// <summary>Descifrado independiente de la prueba (no usa PhoneProtector): nonce ‖ texto cifrado ‖ etiqueta, con el dato asociado del <c>_id</c>.</summary>
    private static string Decrypt(byte[] value, string userIdHex)
    {
        var key = PhoneProtectionTestKeys.Bytes(PhoneProtectionTestKeys.EncryptionStart1);
        var plaintext = new byte[value.Length - 12 - 16];
        using var aes = new AesGcm(key, 16);
        aes.Decrypt(value.AsSpan(0, 12), value.AsSpan(12, plaintext.Length), value.AsSpan(value.Length - 16, 16), plaintext,
            Encoding.UTF8.GetBytes($"usuarios|celular|{userIdHex}"));
        return Encoding.UTF8.GetString(plaintext);
    }
}
