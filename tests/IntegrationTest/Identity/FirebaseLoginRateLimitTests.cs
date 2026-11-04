using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Knock.Api.Modules.Identity.Data;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Identity;

/// <summary>HU-001 CA9 (desviación documentada: por IP): el límite estricto de <c>/auth</c> corta antes de verificar el token. Solo datos ficticios.</summary>
public class FirebaseLoginRateLimitTests
{
    private const string _phone = "+525555550121";
    private const string _ipOfAttacker = "203.0.113.50";
    private const string _ipOfOther = "203.0.113.51";
    private const int _permits = 10;

    private static async Task<long> StoredDocumentsAsync(KnockApiFactory factory)
    {
        var database = IdentityTestDatabase.DatabaseOf(factory);
        long total = 0;
        foreach (var name in new[] { IdentityStore.UsersCollection, IdentityStore.ConsentsCollection, IdentityStore.AuditCollection, IdentityStore.RefreshTokensCollection })
        {
            total += await database.GetCollection<BsonDocument>(name).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        }

        return total;
    }

    private static Task<HttpResponseMessage> ExchangeFromAsync(HttpClient client, string idToken, string ip, string deviceId) =>
        TestHttp.SendAsync(
            client,
            HttpMethod.Post,
            "/api/v1/auth/firebase",
            ip,
            jsonBody: JsonSerializer.Serialize(new { firebaseIdToken = idToken, deviceId, aceptaAviso = true }));

    [Fact]
    public async Task EleventhExchangeFromSameIp_Returns429BeforeAnyVerification_TC_HU_001_09()
    {
        var (idToken, _) = await FirebaseEmulator.SignInWithPhoneAsync(_phone);
        using var factory = await IdentityTestDatabase.CreateFactoryAsync(settings: TestHttp.Limits(globalPermits: 1000, authPermits: _permits));
        using var client = factory.CreateClient();

        for (var i = 1; i <= _permits; i++)
        {
            using var accepted = await ExchangeFromAsync(client, idToken, _ipOfAttacker, Guid.NewGuid().ToString("D"));
            accepted.StatusCode.Should().BeOneOf([HttpStatusCode.Created, HttpStatusCode.OK], $"la petición {i} está dentro del límite");
        }

        var before = await StoredDocumentsAsync(factory);

        // Token válido y otro deviceId: si el límite mirara el dispositivo o el token, esta petición pasaría.
        using var limited = await ExchangeFromAsync(client, idToken, _ipOfAttacker, Guid.NewGuid().ToString("D"));

        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        limited.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        limited.Headers.RetryAfter?.Delta?.TotalSeconds.Should().BeGreaterThan(0);
        var raw = await limited.Content.ReadAsStringAsync();
        raw.Should().NotContain("accessToken").And.NotContain(_phone).And.NotContain(_ipOfAttacker);
        (await StoredDocumentsAsync(factory)).Should().Be(before, "el 429 llega antes de tocar Firebase o la base: ni una huella nueva");

        using var otherIp = await ExchangeFromAsync(client, idToken, _ipOfOther, Guid.NewGuid().ToString("D"));

        otherIp.StatusCode.Should().BeOneOf([HttpStatusCode.Created, HttpStatusCode.OK], "otra IP tiene su propio contador");
    }
}
