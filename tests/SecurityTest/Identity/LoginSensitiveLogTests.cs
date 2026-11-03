using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Knock.Api.Modules.Identity.Data;
using Knock.Api.Modules.Identity.Domain;
using Knock.Api.Modules.Identity.Services;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.SecurityTest.Identity;

/// <summary>HU-001 caso 18: ningún dato sensible del canje llega a los logs. Solo celulares y datos ficticios.</summary>
public class LoginSensitiveLogTests
{
    private const string _phone = "+525555550118";
    private const string _otherPhone = "+525555550119";
    private const string _ipOfSequence = "203.0.113.60";
    private const string _ipOfFailure = "203.0.113.61";

    private static readonly string[] _allowedLogProperties =
    [
        "@t", "@m", "@i", "@l", "@tr", "@sp", "EventId", "SourceContext", "ActionId", "ActionName", "RequestId", "CorrelationId",
    ];

    private sealed class FailingStore : IIdentityStore
    {
        public Task<UserAccount?> FindBySubAsync(string sub, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("synthetic store failure");

        public Task<UserAccount?> FindByPhoneHashAsync(byte[] phoneHash, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("synthetic store failure");

        public Task<AccountCreation> CreateAccountAsync(NewAccount account, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("synthetic store failure");

        public Task AddRefreshTokenAsync(RefreshTokenRecord refresh, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("synthetic store failure");
    }

    private static Task<HttpResponseMessage> ExchangeFromAsync(HttpClient client, string idToken, string ip) =>
        TestHttp.SendAsync(
            client,
            HttpMethod.Post,
            "/api/v1/auth/firebase",
            ip,
            jsonBody: JsonSerializer.Serialize(new { firebaseIdToken = idToken, deviceId = Guid.NewGuid().ToString("D"), aceptaAviso = true }));

    private static string Forge(string uid, string phone)
    {
        var now = DateTimeOffset.UtcNow;
        return FirebaseEmulator.ForgeUnsignedIdToken(uid, phone, FirebaseEmulator.ProjectId, now.AddMinutes(-1), now.AddMinutes(59));
    }

    private static IEnumerable<string> PhoneForms(string phone)
    {
        var national = phone[3..];
        yield return phone;
        yield return phone[1..];
        yield return national;
        yield return $"{national[..2]} {national[2..6]} {national[6..]}";
        yield return $"{national[..2]}-{national[2..6]}-{national[6..]}";
        yield return $"+52 {national[..2]} {national[2..6]} {national[6..]}";
    }

    [Fact]
    public async Task Logs_DoNotContainPhoneUidSubTokensOrFingerprints_TC_HU_001_18()
    {
        var (idToken, uid) = await FirebaseEmulator.SignInWithPhoneAsync(_phone);
        var rejectedToken = idToken[..^25];
        var conflictToken = Forge("uid-prueba-0118-otro", _phone);
        var sub = UserIdDerivation.Derive(uid).ToString("D");
        var secrets = new List<string> { uid, sub, sub.Replace("-", string.Empty, StringComparison.Ordinal), idToken, rejectedToken, conflictToken };
        secrets.AddRange(PhoneForms(_phone));
        secrets.AddRange(PhoneForms(_otherPhone));
        secrets.Add(Convert.ToBase64String(KnockApiFactory.TestPhoneEncryptionKey));
        secrets.Add(Convert.ToBase64String(KnockApiFactory.TestPhoneHmacKey));
        secrets.Add(Convert.ToHexStringLower(KnockApiFactory.TestPhoneEncryptionKey));
        secrets.Add(Convert.ToHexStringLower(KnockApiFactory.TestPhoneHmacKey));
        var statuses = new List<HttpStatusCode>();

        var logs = await ConsoleCapture.RunAsync(async () =>
        {
            // 429: el límite de /auth se agota con las cinco peticiones de la secuencia y la sexta lo supera.
            using var factory = await IdentityTestDatabase.CreateFactoryAsync(settings: TestHttp.Limits(globalPermits: 1000, authPermits: 5));
            using var client = factory.CreateClient();

            using var created = await ExchangeFromAsync(client, idToken, _ipOfSequence);
            var (access, refresh) = await AuthHttp.ReadSessionAsync(created);
            secrets.Add(access);
            secrets.Add(refresh);
            secrets.Add(Convert.ToBase64String(RefreshTokenFingerprint.Compute(refresh)));
            secrets.Add(Convert.ToHexStringLower(RefreshTokenFingerprint.Compute(refresh)));
            using var existing = await ExchangeFromAsync(client, idToken, _ipOfSequence);
            using var rejected = await ExchangeFromAsync(client, rejectedToken, _ipOfSequence);
            using var conflict = await ExchangeFromAsync(client, conflictToken, _ipOfSequence);
            await IdentityTestDatabase.DatabaseOf(factory).GetCollection<BsonDocument>(IdentityStore.UsersCollection)
                .UpdateOneAsync(FilterDefinition<BsonDocument>.Empty, Builders<BsonDocument>.Update.Set(IdentityStore.StateField, "suspendido"));
            using var suspended = await ExchangeFromAsync(client, idToken, _ipOfSequence);
            using var limited = await ExchangeFromAsync(client, idToken, _ipOfSequence);
            statuses.AddRange([created.StatusCode, existing.StatusCode, rejected.StatusCode, conflict.StatusCode, suspended.StatusCode, limited.StatusCode]);

            // 500: la base falla (doble que lanza) con un token válido de otra IP.
            using var failing = new KnockApiFactory(configureServices: services => services.AddSingleton<IIdentityStore>(new FailingStore()));
            using var failingClient = failing.CreateClient();
            using var failed = await ExchangeFromAsync(failingClient, idToken, _ipOfFailure);
            statuses.Add(failed.StatusCode);

            // El sub del JWT emitido es el derivado del uid: así los valores vigilados son los reales.
            new JsonWebTokenHandler().ReadJsonWebToken(access).GetClaim("sub").Value.Should().Be(sub);
        });

        statuses.Should().Equal(
            HttpStatusCode.Created,
            HttpStatusCode.OK,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Conflict,
            HttpStatusCode.Forbidden,
            HttpStatusCode.TooManyRequests,
            HttpStatusCode.InternalServerError);
        logs.Should().Contain("/api/v1/auth/firebase", "la captura debe incluir los eventos de las peticiones (si no, la prueba sería vacía)");
        secrets.Where(secret => logs.Contains(secret, StringComparison.Ordinal)).Select(secret => secret.Length > 12 ? secret[..12] + "…" : secret)
            .Should().BeEmpty("ningún log del canje lleva celular, uid, sub, tokens, huella ni llaves");

        // Los eventos propios del canje (2001 a 2005) solo llevan el mensaje, el evento y los datos de correlación y de la petición.
        var exchangeEvents = logs.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.Contains("\"SourceContext\":\"" + typeof(FirebaseSessionService).FullName + "\"", StringComparison.Ordinal))
            .ToList();
        exchangeEvents.Should().NotBeEmpty("los eventos del canje deben haberse registrado");
        foreach (var line in exchangeEvents)
        {
            using var entry = JsonDocument.Parse(line);
            entry.RootElement.EnumerateObject().Select(p => p.Name).Should().BeSubsetOf(_allowedLogProperties, line);
        }
    }

    [Fact]
    public void FirebaseIdentity_CannotBePrintedWithThePhone_TC_HU_001_18()
    {
        var identity = new FirebaseIdentity("uid-prueba-0118", _phone);

        $"{identity}".Should().NotContain(_phone).And.NotContain("uid-prueba-0118");
        identity.ToString().Should().Be(nameof(FirebaseIdentity));
    }
}
