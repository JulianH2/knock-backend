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

/// <summary>HU-002: <c>PUT /me</c> y <c>GET /me</c> de punta a punta con el emulador de Firebase y documentdb-local. Solo celulares y nombres ficticios.</summary>
public class ProfileTests
{
    private const string _phoneSave = "+525555550201";
    private const string _phoneIdempotent = "+525555550202";
    private const string _phoneSuspended = "+525555550203";
    private const string _phoneState = "+525555550204";
    private const string _phoneBody = "+525555550205";
    private const string _phoneLimit = "+525555550206";
    private const string _ipOfLimit = "203.0.113.70";

    /// <summary>Un host con base propia y una sesión recién canjeada con el emulador.</summary>
    private sealed class Session(KnockApiFactory factory, HttpClient client, string accessToken, string sub)
    {
        public KnockApiFactory Factory { get; } = factory;

        public string Sub { get; } = sub;

        public Task<HttpResponseMessage> PutAsync(string json, string? remoteIp = null) =>
            TestHttp.SendAsync(client, HttpMethod.Put, "/api/v1/me", remoteIp, bearerToken: accessToken, jsonBody: json);

        public Task<HttpResponseMessage> GetAsync() => AuthHttp.MeAsync(client, accessToken);

        public async Task<BsonDocument> UserAsync() =>
            await IdentityTestDatabase.DatabaseOf(Factory).GetCollection<BsonDocument>(IdentityStore.UsersCollection)
                .Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
    }

    /// <summary>Crea el host y la sesión, ejecuta la prueba y libera el host y el cliente (son de la prueba, no del auxiliar).</summary>
    private static async Task WithSessionAsync(string phone, Func<Session, Task> test, IReadOnlyDictionary<string, string?>? settings = null)
    {
        var (idToken, _) = await FirebaseEmulator.SignInWithPhoneAsync(phone);
        using var factory = await IdentityTestDatabase.CreateFactoryAsync(settings: settings);
        using var client = factory.CreateClient();
        using var exchange = await AuthHttp.ExchangeAsync(client, idToken);
        var (access, _) = await AuthHttp.ReadSessionAsync(exchange);
        var sub = new JsonWebTokenHandler().ReadJsonWebToken(access).GetClaim("sub").Value;
        await test(new Session(factory, client, access, sub));
    }

    private static string Name(string value) => JsonSerializer.Serialize(new { nombre = value });

    [Fact]
    public async Task ValidName_SavesAndCompletesProfile_TC_HU_002_01()
    {
        await WithSessionAsync(_phoneSave, async session =>
        {
            using var response = await session.PutAsync(Name("José Pérez"));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            body.RootElement.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("id", "nombre", "perfilCompleto");
            body.RootElement.GetProperty("id").GetString().Should().Be(session.Sub);
            body.RootElement.GetProperty("nombre").GetString().Should().Be("José Pérez");
            body.RootElement.GetProperty("perfilCompleto").GetBoolean().Should().BeTrue();
            var user = await session.UserAsync();
            user[IdentityStore.NameField].AsString.Should().Be("José Pérez");
            user[IdentityStore.ProfileCompleteField].AsBoolean.Should().BeTrue();
            user[IdentityStore.VersionField].ToInt32().Should().Be(2, "se creó con version 1 y el nombre cambió el contenido");
            using var read = await session.GetAsync();
            read.StatusCode.Should().Be(HttpStatusCode.OK);
            using var readBody = JsonDocument.Parse(await read.Content.ReadAsStringAsync());
            readBody.RootElement.GetProperty("id").GetString().Should().Be(session.Sub);
            readBody.RootElement.GetProperty("nombre").GetString().Should().Be("José Pérez");
            readBody.RootElement.GetProperty("perfilCompleto").GetBoolean().Should().BeTrue();
        });
    }

    [Fact]
    public async Task SamePutTwice_IsIdempotent_TC_HU_002_05()
    {
        await WithSessionAsync(_phoneIdempotent, async session =>
        {
            using var first = await session.PutAsync(Name("Ana Ruiz"));
            var afterFirst = await session.UserAsync();
            using var second = await session.PutAsync(Name("Ana Ruiz"));
            using var respaced = await session.PutAsync(Name("  Ana   Ruiz "));
            var afterRepeats = await session.UserAsync();
            using var other = await session.PutAsync(Name("Ana Luisa Ruiz"));
            var afterChange = await session.UserAsync();

            first.StatusCode.Should().Be(HttpStatusCode.OK);
            second.StatusCode.Should().Be(HttpStatusCode.OK);
            respaced.StatusCode.Should().Be(HttpStatusCode.OK);
            (await second.Content.ReadAsStringAsync()).Should().Be(await first.Content.ReadAsStringAsync(), "el mismo cuerpo se repite con el mismo resultado");
            afterFirst[IdentityStore.VersionField].ToInt32().Should().Be(2);
            afterRepeats[IdentityStore.VersionField].ToInt32().Should().Be(2, "version solo sube si el contenido cambió; el mismo nombre con otros espacios es el mismo contenido");
            other.StatusCode.Should().Be(HttpStatusCode.OK);
            afterChange[IdentityStore.NameField].AsString.Should().Be("Ana Luisa Ruiz");
            afterChange[IdentityStore.VersionField].ToInt32().Should().Be(3);
        });
    }

    [Fact]
    public async Task SuspendedAccount_Returns403_TC_HU_002_08()
    {
        await WithSessionAsync(_phoneSuspended, async session =>
        {
            await IdentityTestDatabase.DatabaseOf(session.Factory).GetCollection<BsonDocument>(IdentityStore.UsersCollection)
                .UpdateOneAsync(FilterDefinition<BsonDocument>.Empty, Builders<BsonDocument>.Update.Set(IdentityStore.StateField, "suspendido"));
            var before = await session.UserAsync();

            using var response = await session.PutAsync(Name("Nombre Valido"));

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
            var raw = await response.Content.ReadAsStringAsync();
            using var body = JsonDocument.Parse(raw);
            body.RootElement.GetProperty("code").GetString().Should().Be("account_suspended");
            raw.Should().NotContain("Nombre Valido").And.NotContain(session.Sub);
            var after = await session.UserAsync();
            after.Should().BeEquivalentTo(before, "la cuenta suspendida no cambia nada");
            after.Contains(IdentityStore.NameField).Should().BeFalse();
        });
    }

    [Fact]
    public async Task GetMeShowsProfileState_TC_HU_002_10()
    {
        await WithSessionAsync(_phoneState, async session =>
        {
            using var empty = await session.GetAsync();
            using var put = await session.PutAsync(Name("Luis Núñez"));
            using var filled = await session.GetAsync();

            foreach (var response in new[] { empty, put, filled })
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                response.Headers.CacheControl?.NoStore.Should().BeTrue("el perfil no se guarda en cachés");
            }

            using var emptyBody = JsonDocument.Parse(await empty.Content.ReadAsStringAsync());
            emptyBody.RootElement.GetProperty("id").GetString().Should().Be(session.Sub);
            emptyBody.RootElement.GetProperty("nombre").ValueKind.Should().Be(JsonValueKind.Null);
            emptyBody.RootElement.GetProperty("perfilCompleto").GetBoolean().Should().BeFalse();
            using var filledBody = JsonDocument.Parse(await filled.Content.ReadAsStringAsync());
            filledBody.RootElement.GetProperty("nombre").GetString().Should().Be("Luis Núñez");
            filledBody.RootElement.GetProperty("perfilCompleto").GetBoolean().Should().BeTrue();
        });
    }

    [Fact]
    public async Task OversizedOrUnreadableBody_413Or400_TC_HU_002_11()
    {
        await WithSessionAsync(_phoneBody, async session =>
        {
            var before = await session.UserAsync();

            using var oversized = await session.PutAsync(Name(new string('a', 3000)));
            using var unreadable = await session.PutAsync("{\"nombre\":");
            using var notJson = await session.PutAsync("nombre=Ana");

            oversized.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
            unreadable.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            foreach (var response in new[] { oversized, unreadable })
            {
                response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
                using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                body.RootElement.TryGetProperty("traceId", out _).Should().BeTrue();
                body.RootElement.TryGetProperty("correlationId", out _).Should().BeTrue();
            }

            notJson.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnsupportedMediaType);
            (await session.UserAsync()).Should().BeEquivalentTo(before, "ningún cuerpo rechazado guarda nada");
        });
    }

    [Fact]
    public async Task GlobalRateLimitApplies_TC_HU_002_13()
    {
        await WithSessionAsync(_phoneLimit, async session =>
        {
            using var first = await session.PutAsync(Name("Rosa Gil"), _ipOfLimit);
            using var second = await session.PutAsync(Name("Rosa Gil"), _ipOfLimit);
            using var limited = await session.PutAsync(Name("Rosa Gil"), _ipOfLimit);

            first.StatusCode.Should().Be(HttpStatusCode.OK);
            second.StatusCode.Should().Be(HttpStatusCode.OK);
            limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
            limited.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
            limited.Headers.RetryAfter?.Delta?.TotalSeconds.Should().BeGreaterThan(0);
        }, TestHttp.Limits(globalPermits: 2, authPermits: 10));
    }
}
