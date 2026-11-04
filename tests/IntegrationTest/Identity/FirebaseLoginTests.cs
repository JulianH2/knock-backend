using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Knock.Api.Modules.Identity.Data;
using Knock.IntegrationTest.Support;
using Microsoft.IdentityModel.JsonWebTokens;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Identity;

/// <summary>HU-001: el canje de punta a punta con el emulador de Firebase y documentdb-local. Solo celulares y datos ficticios.</summary>
public class FirebaseLoginTests
{
    private const string _phone = "+525555550111";
    private const string _otherPhone = "+525555550112";

    private static async Task<Dictionary<string, long>> CountsAsync(KnockApiFactory factory)
    {
        var database = IdentityTestDatabase.DatabaseOf(factory);
        var counts = new Dictionary<string, long>();
        foreach (var name in new[] { IdentityStore.UsersCollection, IdentityStore.ConsentsCollection, IdentityStore.AuditCollection, IdentityStore.RefreshTokensCollection })
        {
            counts[name] = await database.GetCollection<BsonDocument>(name).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        }

        return counts;
    }

    private static string Forge(string uid, string phone)
    {
        var now = DateTimeOffset.UtcNow;
        return FirebaseEmulator.ForgeUnsignedIdToken(uid, phone, FirebaseEmulator.ProjectId, now.AddMinutes(-1), now.AddMinutes(59));
    }

    [Fact]
    public async Task NewPhone_Returns201AndPersistsAllDocuments_TC_HU_001_04()
    {
        var (idToken, uid) = await FirebaseEmulator.SignInWithPhoneAsync(_phone);
        using var factory = await IdentityTestDatabase.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var deviceId = Guid.NewGuid().ToString("D");

        using var response = await AuthHttp.ExchangeAsync(client, idToken, deviceId);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.CacheControl?.NoStore.Should().BeTrue("las respuestas con tokens no se guardan en cachés");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("esNuevo").GetBoolean().Should().BeTrue();
        body.RootElement.GetProperty("perfilCompleto").GetBoolean().Should().BeFalse();
        var counts = await CountsAsync(factory);
        counts[IdentityStore.UsersCollection].Should().Be(1);
        counts[IdentityStore.ConsentsCollection].Should().Be(2);
        counts[IdentityStore.AuditCollection].Should().Be(1);
        counts[IdentityStore.RefreshTokensCollection].Should().Be(1);
        var database = IdentityTestDatabase.DatabaseOf(factory);
        var user = await database.GetCollection<BsonDocument>(IdentityStore.UsersCollection).Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        var sub = new JsonWebTokenHandler().ReadJsonWebToken(body.RootElement.GetProperty("accessToken").GetString()).GetClaim("sub").Value;
        user[IdentityStore.SubField].AsString.Should().Be(sub);
        user[IdentityStore.StateField].AsString.Should().Be("activo");
        user[IdentityStore.ProfileCompleteField].AsBoolean.Should().BeFalse();
        user[IdentityStore.VersionField].ToInt32().Should().Be(1);
        user.ToJson().Should().NotContain(_phone).And.NotContain(uid);
        var consents = await database.GetCollection<BsonDocument>(IdentityStore.ConsentsCollection).Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        consents.Select(c => c[IdentityStore.DocumentField].AsString).Should().BeEquivalentTo("aviso_privacidad", "terminos");
        consents.Select(c => c[IdentityStore.DeviceIdField].AsString).Should().OnlyContain(id => id == deviceId);
        var audit = await database.GetCollection<BsonDocument>(IdentityStore.AuditCollection).Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        audit[IdentityStore.EventField].AsString.Should().Be("USER_REGISTERED");
        audit[IdentityStore.CorrelationIdField].AsString.Should().NotBeNullOrEmpty();
        audit.ToJson().Should().NotContain(_phone);
    }

    [Fact]
    public async Task ExistingPhone_Returns200WithSameSubAndOnlyNewRefresh_TC_HU_001_03()
    {
        var (idToken, _) = await FirebaseEmulator.SignInWithPhoneAsync(_phone);
        using var factory = await IdentityTestDatabase.CreateFactoryAsync();
        using var client = factory.CreateClient();
        using var first = await AuthHttp.ExchangeAsync(client, idToken);
        var before = await CountsAsync(factory);

        using var second = await AuthHttp.ExchangeAsync(client, idToken);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("esNuevo").GetBoolean().Should().BeFalse();
        body.RootElement.GetProperty("perfilCompleto").GetBoolean().Should().BeFalse();
        var handler = new JsonWebTokenHandler();
        var firstSession = await AuthHttp.ReadSessionAsync(first);
        var secondSession = await AuthHttp.ReadSessionAsync(second);
        var firstJwt = handler.ReadJsonWebToken(firstSession.AccessToken);
        var secondJwt = handler.ReadJsonWebToken(secondSession.AccessToken);
        secondJwt.GetClaim("sub").Value.Should().Be(firstJwt.GetClaim("sub").Value);
        secondJwt.GetClaim("jti").Value.Should().NotBe(firstJwt.GetClaim("jti").Value);
        var after = await CountsAsync(factory);
        after[IdentityStore.UsersCollection].Should().Be(before[IdentityStore.UsersCollection]);
        after[IdentityStore.ConsentsCollection].Should().Be(before[IdentityStore.ConsentsCollection]);
        after[IdentityStore.AuditCollection].Should().Be(before[IdentityStore.AuditCollection]);
        after[IdentityStore.RefreshTokensCollection].Should().Be(before[IdentityStore.RefreshTokensCollection] + 1);
    }

    [Fact]
    public async Task SuspendedAccount_Returns403WithCodeAndNoSession_TC_HU_001_08()
    {
        var (idToken, uid) = await FirebaseEmulator.SignInWithPhoneAsync(_phone);
        using var factory = await IdentityTestDatabase.CreateFactoryAsync();
        using var client = factory.CreateClient();
        using var created = await AuthHttp.ExchangeAsync(client, idToken);
        var sub = new JsonWebTokenHandler().ReadJsonWebToken((await AuthHttp.ReadSessionAsync(created)).AccessToken).GetClaim("sub").Value;
        await IdentityTestDatabase.DatabaseOf(factory).GetCollection<BsonDocument>(IdentityStore.UsersCollection)
            .UpdateOneAsync(FilterDefinition<BsonDocument>.Empty, Builders<BsonDocument>.Update.Set(IdentityStore.StateField, "suspendido"));
        var before = await CountsAsync(factory);

        using var response = await AuthHttp.ExchangeAsync(client, idToken);
        using var again = await AuthHttp.ExchangeAsync(client, idToken);

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        foreach (var rejected in new[] { response, again })
        {
            rejected.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            rejected.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
            rejected.Headers.CacheControl?.NoStore.Should().BeTrue();
            var raw = await rejected.Content.ReadAsStringAsync();
            using var body = JsonDocument.Parse(raw);
            body.RootElement.GetProperty("code").GetString().Should().Be("account_suspended");
            body.RootElement.TryGetProperty("accessToken", out _).Should().BeFalse();
            raw.Should().NotContain(_phone).And.NotContain(uid).And.NotContain(sub);
        }

        (await CountsAsync(factory)).Should().BeEquivalentTo(before, "no cambia ninguna colección");
    }

    [Fact]
    public async Task PhoneOrUidConflict_Returns409AndCreatesNothing_TC_HU_001_11()
    {
        var (idToken, uid) = await FirebaseEmulator.SignInWithPhoneAsync(_phone);
        using var factory = await IdentityTestDatabase.CreateFactoryAsync();
        using var client = factory.CreateClient();
        using var created = await AuthHttp.ExchangeAsync(client, idToken);
        var before = await CountsAsync(factory);

        using var samePhoneOtherUid = await AuthHttp.ExchangeAsync(client, Forge("uid-prueba-hu001-otro", _phone));
        using var sameUidOtherPhone = await AuthHttp.ExchangeAsync(client, Forge(uid, _otherPhone));

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        foreach (var conflict in new[] { samePhoneOtherUid, sameUidOtherPhone })
        {
            conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
            conflict.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
            var raw = await conflict.Content.ReadAsStringAsync();
            using var body = JsonDocument.Parse(raw);
            body.RootElement.GetProperty("code").GetString().Should().Be("account_conflict");
            raw.Should().NotContain("accessToken").And.NotContain(_phone).And.NotContain(_otherPhone).And.NotContain(uid);
        }

        (await CountsAsync(factory)).Should().BeEquivalentTo(before, "ni usuarios, ni consentimientos, ni huellas nuevas");
    }

    [Fact]
    public async Task ConcurrentFirstExchanges_CreateOneAccountAndNeverFail_TC_HU_001_07()
    {
        var (idToken, _) = await FirebaseEmulator.SignInWithPhoneAsync(_phone);
        using var factory = await IdentityTestDatabase.CreateFactoryAsync();
        using var client = factory.CreateClient();

        var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => AuthHttp.ExchangeAsync(client, idToken)));

        responses.Select(r => r.StatusCode).Should().OnlyContain(status => status == HttpStatusCode.Created || status == HttpStatusCode.OK);
        responses.Count(r => r.StatusCode == HttpStatusCode.Created).Should().Be(1, "solo un canje crea la cuenta");
        var counts = await CountsAsync(factory);
        counts[IdentityStore.UsersCollection].Should().Be(1);
        counts[IdentityStore.ConsentsCollection].Should().Be(2);
        counts[IdentityStore.AuditCollection].Should().Be(1);
        counts[IdentityStore.RefreshTokensCollection].Should().Be(6, "cada canje exitoso guarda su huella");
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }
}
