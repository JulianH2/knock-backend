using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Knock.Api.Modules.Identity.Data;
using Knock.Api.Modules.Identity.Domain;
using Knock.Api.Modules.Identity.Dtos;
using Knock.IntegrationTest.Support;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.SecurityTest.Identity;

/// <summary>HU-002, seguridad negativa de <c>PUT /me</c>: campos de más, sin sesión y ninguna forma de apuntar a otra cuenta. Datos ficticios.</summary>
public class ProfileTests
{
    private const string _phoneA = "+525555550211";
    private const string _phoneB = "+525555550212";

    /// <summary>Doble que falla si algo toca la base: prueba que el 401 llega antes de cualquier lectura o escritura.</summary>
    private sealed class UntouchableStore : IProfileStore
    {
        public int Calls { get; private set; }

        public Task<UserProfile?> FindProfileAsync(string sub, CancellationToken cancellationToken)
        {
            Calls++;
            throw new InvalidOperationException("synthetic: the store must not be touched");
        }

        public Task<UserProfile?> SaveNameAsync(string sub, string name, CancellationToken cancellationToken)
        {
            Calls++;
            throw new InvalidOperationException("synthetic: the store must not be touched");
        }
    }

    private static string Jwt(TimeSpan expiresIn, bool tamper = false)
    {
        var now = DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = KnockApiFactory.TestIssuer,
            Audience = KnockApiFactory.TestAudience,
            Claims = new Dictionary<string, object> { ["sub"] = Guid.NewGuid().ToString(), ["jti"] = Guid.NewGuid().ToString() },
            NotBefore = now - TimeSpan.FromMinutes(20),
            IssuedAt = now - TimeSpan.FromMinutes(20),
            Expires = now + expiresIn,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(KnockApiFactory.TestSigningKey), SecurityAlgorithms.HmacSha256),
        };
        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        if (!tamper)
        {
            return token;
        }

        var lastDot = token.LastIndexOf('.');
        var signature = token[(lastDot + 1)..].ToCharArray();
        signature[0] = signature[0] == 'A' ? 'B' : 'A';
        return string.Concat(token.AsSpan(0, lastDot + 1), new string(signature));
    }

    private static string UnsignedAlgNone()
    {
        var header = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes("""{"alg":"none","typ":"JWT"}"""));
        var exp = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds();
        var payload = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(
            $$"""{"sub":"{{Guid.NewGuid()}}","jti":"{{Guid.NewGuid()}}","iss":"knock-api","aud":"knock-app","exp":{{exp}}}"""));
        return $"{header}.{payload}.";
    }

    public static TheoryData<string, string?> InvalidCredentials => new()
    {
        { "no-token", null },
        { "tampered-signature", Jwt(TimeSpan.FromMinutes(15), tamper: true) },
        { "expired-one-second-ago", Jwt(TimeSpan.FromSeconds(-1)) },
        { "alg-none", UnsignedAlgNone() },
        { "garbage", "not-a-jwt" },
    };

    /// <summary>Crea el host y una sesión ya canjeada, ejecuta la prueba y libera el host y el cliente.</summary>
    private static async Task WithSessionAsync(string phone, Func<KnockApiFactory, HttpClient, string, string, Task> test)
    {
        var (idToken, _) = await FirebaseEmulator.SignInWithPhoneAsync(phone);
        using var factory = await IdentityTestDatabase.CreateFactoryAsync();
        using var client = factory.CreateClient();
        using var exchange = await AuthHttp.ExchangeAsync(client, idToken);
        var (access, _) = await AuthHttp.ReadSessionAsync(exchange);
        await test(factory, client, access, new JsonWebTokenHandler().ReadJsonWebToken(access).GetClaim("sub").Value);
    }

    private static IMongoCollection<BsonDocument> Users(KnockApiFactory factory) =>
        IdentityTestDatabase.DatabaseOf(factory).GetCollection<BsonDocument>(IdentityStore.UsersCollection);

    private static Task<HttpResponseMessage> PutAsync(HttpClient client, string token, string json, string path = "/api/v1/me") =>
        TestHttp.SendAsync(client, HttpMethod.Put, path, bearerToken: token, jsonBody: json);

    /// <summary>El 401 es el mismo salvo los identificadores de la petición (<c>traceId</c> y <c>correlationId</c>), que cambian en cada una.</summary>
    private static string WithoutRequestIds(string problem)
    {
        using var document = JsonDocument.Parse(problem);
        return string.Join(
            "|",
            document.RootElement.EnumerateObject()
                .Where(p => p.Name is not ("traceId" or "correlationId"))
                .Select(p => p.Name + "=" + p.Value.GetRawText()));
    }

    [Fact]
    public async Task ExtraFieldsAreIgnored_TC_HU_002_06()
    {
        await WithSessionAsync(_phoneA, async (factory, client, access, sub) =>
        {
            var json = JsonSerializer.Serialize(new
            {
                nombre = "Ana Ruiz",
                id = "000000000000000000000000",
                sub = Guid.NewGuid().ToString(),
                estado = "suspendido",
                perfilCompleto = false,
                version = 99,
                celularHash = "AAAA",
            });

            using var response = await PutAsync(client, access, json);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            body.RootElement.GetProperty("id").GetString().Should().Be(sub, "el id sale del JWT, no del cuerpo");
            body.RootElement.GetProperty("perfilCompleto").GetBoolean().Should().BeTrue();
            var user = await Users(factory).Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
            user[IdentityStore.SubField].AsString.Should().Be(sub);
            user[IdentityStore.StateField].AsString.Should().Be("activo");
            user[IdentityStore.ProfileCompleteField].AsBoolean.Should().BeTrue();
            user[IdentityStore.VersionField].ToInt32().Should().Be(2, "el cliente no elige version");
            user[IdentityStore.NameField].AsString.Should().Be("Ana Ruiz");
        });
    }

    [Theory]
    [MemberData(nameof(InvalidCredentials))]
    public async Task Unauthenticated_Returns401_TC_HU_002_07(string label, string? token)
    {
        var store = new UntouchableStore();
        using var factory = new KnockApiFactory(configureServices: services => services.AddSingleton<IProfileStore>(store));
        using var client = factory.CreateClient();

        using var put = await TestHttp.SendAsync(client, HttpMethod.Put, "/api/v1/me", bearerToken: token, jsonBody: "{\"nombre\":\"Ana Ruiz\"}");
        using var get = await TestHttp.SendAsync(client, HttpMethod.Get, "/api/v1/me", bearerToken: token);

        foreach (var response in new[] { put, get })
        {
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, label);
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json", label);
        }

        WithoutRequestIds(await put.Content.ReadAsStringAsync()).Should().Be(WithoutRequestIds(await get.Content.ReadAsStringAsync()), "el cuerpo del 401 es uniforme");
        store.Calls.Should().Be(0, "el 401 llega antes de tocar la base: " + label);
    }

    [Fact]
    public async Task NoWayToTargetAnotherAccount_TC_HU_002_09()
    {
        await WithSessionAsync(_phoneA, async (factoryA, clientA, accessA, subA) =>
        {
            // La cuenta B vive en el mismo host: se crea con el mismo almacén para poder comparar las dos.
            var (idTokenB, _) = await FirebaseEmulator.SignInWithPhoneAsync(_phoneB);
            using var exchangeB = await AuthHttp.ExchangeAsync(clientA, idTokenB);
            var (_, _) = await AuthHttp.ReadSessionAsync(exchangeB);
            var accounts = await Users(factoryA).Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            accounts.Should().HaveCount(2);
            var subB = accounts.Single(a => a[IdentityStore.SubField].AsString != subA)[IdentityStore.SubField].AsString;

            foreach (var path in new[]
            {
                "/api/v1/me?sub=" + subB,
                "/api/v1/me?id=" + subB,
                "/api/v1/me?usuarioId=" + subB,
            })
            {
                using var response = await PutAsync(clientA, accessA, "{\"nombre\":\"Intruso Uno\",\"sub\":\"" + subB + "\",\"id\":\"" + subB + "\"}", path);
                response.StatusCode.Should().Be(HttpStatusCode.OK, path);
            }

            foreach (var path in new[] { "/api/v1/me/" + subB, "/api/v1/me/perfil", "/api/v1/usuarios/" + subB })
            {
                using var response = await PutAsync(clientA, accessA, "{\"nombre\":\"Intruso Dos\"}", path);
                response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
            }

            var users = await Users(factoryA).Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            var b = users.Single(a => a[IdentityStore.SubField].AsString == subB);
            b.Contains(IdentityStore.NameField).Should().BeFalse("la cuenta B nunca cambió");
            b[IdentityStore.ProfileCompleteField].AsBoolean.Should().BeFalse();
            b[IdentityStore.VersionField].ToInt32().Should().Be(1);
            users.Single(a => a[IdentityStore.SubField].AsString == subA)[IdentityStore.NameField].AsString.Should().Be("Intruso Uno");

            // Diseño: el cuerpo no tiene ningún campo de identidad y la ruta de PUT /me no recibe parámetros (salvo la versión de la API).
            typeof(ProfileRequest).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(p => p.DeclaringType == typeof(ProfileRequest) && p.Name != "EqualityContract")
                .Select(p => p.Name).Should().Equal("Nombre");
            var endpoints = factoryA.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()
                .Where(e => e.RoutePattern.RawText?.Contains("/me", StringComparison.Ordinal) == true
                    && e.Metadata.GetMetadata<Microsoft.AspNetCore.Routing.HttpMethodMetadata>()?.HttpMethods.Contains("PUT") == true)
                .ToList();
            endpoints.Should().ContainSingle();
            endpoints[0].RoutePattern.Parameters.Select(p => p.Name).Should().BeSubsetOf(["version"]);
        });
    }
}
