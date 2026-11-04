using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Knock.Api.Modules.Identity.Data;
using Knock.Api.Modules.Identity.Services;
using Knock.IntegrationTest.Support;
using Microsoft.IdentityModel.JsonWebTokens;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.SecurityTest.Identity;

/// <summary>HU-002 caso 12: ni el nombre, ni el sub, ni los tokens llegan a los registros, ni en los éxitos ni en los errores. Datos ficticios.</summary>
public class ProfileLeakTests
{
    private const string _phone = "+525555550213";
    private const string _name = "Zoila Quintanilla";
    private const string _invalidName = "Zoila1234Secreta";
    private const string _suspendedName = "Ernesto Valdivia";
    private const string _oversizedName = "Valentina";
    private const string _ipOfSequence = "203.0.113.71";

    private static readonly string[] _allowedLogProperties =
    [
        "@t", "@m", "@i", "@l", "@tr", "@sp", "EventId", "SourceContext", "ActionId", "ActionName", "RequestId", "CorrelationId",
    ];

    private static Task<HttpResponseMessage> PutAsync(HttpClient client, string token, string json, string? ip = null) =>
        TestHttp.SendAsync(client, HttpMethod.Put, "/api/v1/me", ip, bearerToken: token, jsonBody: json);

    [Fact]
    public async Task NoPersonalDataInLogs_TC_HU_002_12()
    {
        var (idToken, _) = await FirebaseEmulator.SignInWithPhoneAsync(_phone);
        var statuses = new List<HttpStatusCode>();
        var secrets = new List<string> { _name, _invalidName, _suspendedName, _oversizedName, idToken };

        var logs = await ConsoleCapture.RunAsync(async () =>
        {
            using var factory = await IdentityTestDatabase.CreateFactoryAsync(settings: TestHttp.Limits(globalPermits: 6, authPermits: 50));
            using var client = factory.CreateClient();
            using var exchange = await AuthHttp.ExchangeAsync(client, idToken);
            var (access, refresh) = await AuthHttp.ReadSessionAsync(exchange);
            var sub = new JsonWebTokenHandler().ReadJsonWebToken(access).GetClaim("sub").Value;
            secrets.AddRange([access, refresh, sub, sub.Replace("-", string.Empty, StringComparison.Ordinal)]);

            using var saved = await PutAsync(client, access, JsonSerializer.Serialize(new { nombre = _name }), _ipOfSequence);
            using var invalid = await PutAsync(client, access, JsonSerializer.Serialize(new { nombre = _invalidName }), _ipOfSequence);
            using var read = await TestHttp.SendAsync(client, HttpMethod.Get, "/api/v1/me", _ipOfSequence, bearerToken: access);
            using var oversized = await PutAsync(client, access, JsonSerializer.Serialize(new { nombre = _oversizedName + new string('a', 3000) }), _ipOfSequence);
            using var unauthenticated = await TestHttp.SendAsync(client, HttpMethod.Put, "/api/v1/me", _ipOfSequence, jsonBody: JsonSerializer.Serialize(new { nombre = _name }));
            await IdentityTestDatabase.DatabaseOf(factory).GetCollection<BsonDocument>(IdentityStore.UsersCollection)
                .UpdateOneAsync(FilterDefinition<BsonDocument>.Empty, Builders<BsonDocument>.Update.Set(IdentityStore.StateField, "suspendido"));
            using var suspended = await PutAsync(client, access, JsonSerializer.Serialize(new { nombre = _suspendedName }), _ipOfSequence);
            using var limited = await PutAsync(client, access, JsonSerializer.Serialize(new { nombre = _name }), _ipOfSequence);
            statuses.AddRange([saved.StatusCode, invalid.StatusCode, read.StatusCode, oversized.StatusCode, unauthenticated.StatusCode, suspended.StatusCode, limited.StatusCode]);
        });

        statuses.Should().Equal(
            HttpStatusCode.OK,
            HttpStatusCode.UnprocessableEntity,
            HttpStatusCode.OK,
            HttpStatusCode.RequestEntityTooLarge,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden,
            HttpStatusCode.TooManyRequests);
        logs.Should().Contain("/api/v1/me", "la captura debe incluir los eventos de las peticiones (si no, la prueba sería vacía)");
        secrets.Where(secret => logs.Contains(secret, StringComparison.Ordinal)).Select(secret => secret.Length > 12 ? secret[..12] + "…" : secret)
            .Should().BeEmpty("ningún log del perfil lleva el nombre, el sub ni los tokens");

        // Los eventos propios del perfil (2201 y 2202) solo llevan el mensaje, el evento y los datos de correlación y de la petición.
        var profileEvents = logs.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.Contains("\"SourceContext\":\"" + typeof(ProfileService).FullName + "\"", StringComparison.Ordinal))
            .ToList();
        profileEvents.Should().NotBeEmpty("los eventos del perfil deben haberse registrado");
        foreach (var line in profileEvents)
        {
            using var entry = JsonDocument.Parse(line);
            entry.RootElement.EnumerateObject().Select(p => p.Name).Should().BeSubsetOf(_allowedLogProperties, line);
        }
    }
}
