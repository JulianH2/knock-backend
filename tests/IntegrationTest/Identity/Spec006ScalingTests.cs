using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Knock.IntegrationTest.Identity;

/// <summary>
/// Spec 006: dos copias de Identity compartiendo MongoDB y Redis de Testcontainers. CA-01: al apagarse una copia, la otra atiende y la
/// sesión sigue válida. CA-02: el token emitido por una copia lo acepta la otra (misma clave de Key Vault). CA-03: con MongoDB
/// inaccesible, <c>/health/ready</c> responde no listo. Solo datos ficticios; requiere el emulador de Firebase Auth (como el resto de
/// las pruebas de Identity con canje).
/// </summary>
public sealed class Spec006ScalingTests
{
    private const int _rateLimitFallbackEventId = 247;

    private static readonly Uri _meUri = new("/api/v1/me", UriKind.Relative);

    [Fact]
    public async Task ConcurrentRequests_TwoInstances_DoNotExceedSharedLimit_R_006_05()
    {
        var settings = new Dictionary<string, string?> { ["RateLimiting:Global:PermitLimit"] = "2" };
        using var logs = new CapturingLoggerProvider();
        void Capture(IServiceCollection services) => services.AddLogging(logging => logging.AddProvider(logs));
        using var first = await IdentityApiFactory.CreateAsync(settings, Capture);
        using var second = await IdentityApiFactory.CreateAsync(settings, Capture);
        using var client1 = first.CreateClient();
        using var client2 = second.CreateClient();

        // Calentamiento: una petición por copia con otra IP, para que la conexión a Redis y el script ya estén listos antes de la ráfaga.
        foreach (var client in new[] { client1, client2 })
        {
            using var warmUp = new HttpRequestMessage(HttpMethod.Get, _meUri);
            warmUp.Headers.Add(IdentityApiFactory.RemoteIpHeader, $"198.19.{RandomNumberGenerator.GetInt32(1, 255)}.{RandomNumberGenerator.GetInt32(1, 255)}");
            using var warmUpResponse = await client.SendAsync(warmUp);
        }

        var ip = $"198.20.{RandomNumberGenerator.GetInt32(1, 255)}.{RandomNumberGenerator.GetInt32(1, 255)}";
        client1.DefaultRequestHeaders.Add(IdentityApiFactory.RemoteIpHeader, ip);
        client2.DefaultRequestHeaders.Add(IdentityApiFactory.RemoteIpHeader, ip);
        var summary = await ResponseSummary.ReadAsync(await Task.WhenAll(Enumerable.Range(0, 20).Select(i => (i % 2 == 0 ? client1 : client2).GetAsync(_meUri))));

        var fallbacks = logs.Count(_rateLimitFallbackEventId);
        var because = $"respuestas: {summary}; respaldo por copia (evento {_rateLimitFallbackEventId}): {fallbacks} {logs.Messages(_rateLimitFallbackEventId)}. Con 4 o 6 respuestas 401 y respaldo > 0, Redis no estaba disponible y cada copia contó por su cuenta";
        summary.Count(HttpStatusCode.Unauthorized).Should().Be(2, because);
        summary.Count(HttpStatusCode.TooManyRequests).Should().Be(18, because);
        fallbacks.Should().Be(0, because);
    }

    [Fact]
    public async Task TwoInstances_ShareIpLimit_R_006_05()
    {
        var settings = new Dictionary<string, string?> { ["RateLimiting:Global:PermitLimit"] = "2" };
        using var first = await IdentityApiFactory.CreateAsync(settings);
        using var second = await IdentityApiFactory.CreateAsync(settings);
        using var client1 = first.CreateClient();
        using var client2 = second.CreateClient();
        var ip = $"198.18.{RandomNumberGenerator.GetInt32(1, 255)}.{RandomNumberGenerator.GetInt32(1, 255)}";
        client1.DefaultRequestHeaders.Add(IdentityApiFactory.RemoteIpHeader, ip);
        client2.DefaultRequestHeaders.Add(IdentityApiFactory.RemoteIpHeader, ip);

        using var one = await client1.GetAsync(_meUri);
        using var two = await client2.GetAsync(_meUri);
        using var three = await client1.GetAsync(_meUri);

        one.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        two.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        three.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        three.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var multiplexer = first.Services.GetService(typeof(StackExchange.Redis.IConnectionMultiplexer)) as StackExchange.Redis.IConnectionMultiplexer;
        var key = "ratelimit:identity:" + Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"global:{ip}")));
        (await multiplexer!.GetDatabase().KeyExistsAsync(key)).Should().BeTrue();
        key.Should().NotContain(ip);
    }

    [Fact]
    public async Task OneInstanceDown_TheOtherServesAndTheSessionStaysValid_CA_006_01()
    {
        var database = "identity_s006_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        var first = await IdentityApiFactory.CreateAsync(new Dictionary<string, string?> { ["Mongo:Databases:Identity"] = database });
        string access;
        using (first)
        {
            using var client = first.CreateClient();
            access = await SignInAsync(client);
            using var before = await GetMeAsync(client, access);
            before.StatusCode.Should().Be(HttpStatusCode.OK, "la primera copia atiende mientras vive");
        }

        // La primera copia se apaga; la sesión solo vive en MongoDB/Redis, así que la segunda la sigue viendo.
        var second = await IdentityApiFactory.CreateAsync(new Dictionary<string, string?> { ["Mongo:Databases:Identity"] = database });
        using (second)
        {
            using var client = second.CreateClient();
            using var after = await GetMeAsync(client, access);
            after.StatusCode.Should().Be(HttpStatusCode.OK, "la copia superviviente atiende y la sesión no se pierde");
        }
    }

    [Fact]
    public async Task TokenIssuedByOneInstance_IsAcceptedByTheOther_CA_006_02()
    {
        var database = "identity_s006_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        using var first = await IdentityApiFactory.CreateAsync(new Dictionary<string, string?> { ["Mongo:Databases:Identity"] = database });
        using var second = await IdentityApiFactory.CreateAsync(new Dictionary<string, string?> { ["Mongo:Databases:Identity"] = database });
        using var firstClient = first.CreateClient();
        using var secondClient = second.CreateClient();

        var access = await SignInAsync(firstClient);
        using var response = await GetMeAsync(secondClient, access);

        response.StatusCode.Should().Be(HttpStatusCode.OK, "la misma clave compartida de Key Vault hace que cualquier copia acepte el token");
    }

    [Fact]
    public async Task MongoUnreachable_ReadyIsNotReadyButTheOtherInstanceIs_CA_006_03()
    {
        var redis = await RedisTestFixture.SharedAsync();
        var mongo = await DocumentDbFixture.SharedAsync();
        using var healthy = new IdentityApiFactory(Settings(mongo.ConnectionString, redis.ConnectionString));
        using var broken = new IdentityApiFactory(Settings("mongodb://127.0.0.1:9/?serverSelectionTimeoutMS=500&connectTimeoutMS=500", redis.ConnectionString));

        using var up = await healthy.CreateClient().GetAsync(new Uri("/health/ready", UriKind.Relative));
        using var down = await broken.CreateClient().GetAsync(new Uri("/health/ready", UriKind.Relative));

        up.StatusCode.Should().Be(HttpStatusCode.OK, "con MongoDB y Redis vivos la copia está lista y recibe tráfico");
        down.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable, "sin MongoDB la copia no está lista y el balanceador la saca de rotación");
    }

    private static async Task<string> SignInAsync(HttpClient client)
    {
        var phone = "+5255" + RandomNumberGenerator.GetInt32(10_000_000, 100_000_000).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var (idToken, _) = await FirebaseEmulator.SignInWithPhoneAsync(phone);
        using var response = await AuthHttp.ExchangeAsync(client, idToken);
        response.EnsureSuccessStatusCode();
        var (access, _) = await AuthHttp.ReadSessionAsync(response);
        return access;
    }

    private static async Task<HttpResponseMessage> GetMeAsync(HttpClient client, string? accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, _meUri);
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await client.SendAsync(request);
    }

    private static Dictionary<string, string?> Settings(string mongo, string redis) => new()
    {
        ["ConnectionStrings:KnockMongo"] = mongo,
        ["ConnectionStrings:KnockRedis"] = redis,
        ["Mongo:Databases:Identity"] = "identity_s006_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6)),
    };
}
