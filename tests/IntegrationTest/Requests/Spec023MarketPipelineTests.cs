using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using DotNet.Testcontainers.Builders;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Knock.IntegrationTest.Requests;

/// <summary>
/// Spec 023, T-01: <c>UseAuthentication</c> corre antes de <c>UseRateLimiter</c> en Market (el límite por persona necesita el <c>sub</c>). Comprueba el efecto en
/// el resto de las rutas: las públicas siguen anónimas, las protegidas dan el mismo 401, el límite global sigue siendo por IP y solo la lista de espera cambia
/// su <c>Cache-Control</c>. Host sin contenedores (MongoDB no se usa en estas rutas). Solo datos ficticios.
/// </summary>
public class Spec023MarketPipelineTests
{
    [Fact]
    public async Task RedisOutage_UsesPerInstanceLimit_ThenReturnsToSharedLimit_R_006_05()
    {
        var password = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));
        using var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var hostPort = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        await using var container = new ContainerBuilder(RedisTestFixture.Image)
            .WithPortBinding(hostPort, RedisTestFixture.Port)
            .WithCommand("redis-server", "--requirepass", password)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(RedisTestFixture.Port))
            .Build();
        await container.StartAsync();
        var options = new StackExchange.Redis.ConfigurationOptions { Password = password, AbortOnConnectFail = false, AsyncTimeout = 1000, ConnectTimeout = 1000 };
        options.EndPoints.Add(container.Hostname, container.GetMappedPublicPort(RedisTestFixture.Port));
        using var connection = await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(options);
        var settings = new Dictionary<string, string?> { ["RateLimiting:Global:PermitLimit"] = "2", ["RateLimiting:Global:Window"] = "00:01:00" };
        using var first = await MarketApiFactory.CreateAsync(settings, services => services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(connection));
        using var second = await MarketApiFactory.CreateAsync(settings, services => services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(connection));
        using var identity1 = await IdentityApiFactory.CreateAsync(settings, services => services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(connection));
        using var identity2 = await IdentityApiFactory.CreateAsync(settings, services => services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(connection));
        using var client1 = first.CreateClient();
        using var client2 = second.CreateClient();
        using var identityClient1 = identity1.CreateClient();
        using var identityClient2 = identity2.CreateClient();
        var ip = $"198.19.{System.Security.Cryptography.RandomNumberGenerator.GetInt32(1, 255)}.{System.Security.Cryptography.RandomNumberGenerator.GetInt32(1, 255)}";
        identityClient1.DefaultRequestHeaders.Add(IdentityApiFactory.RemoteIpHeader, ip);
        identityClient2.DefaultRequestHeaders.Add(IdentityApiFactory.RemoteIpHeader, ip);
        var route = new Uri("/api/v1/no-existe", UriKind.Relative);
        await container.StopAsync();
        foreach (var client in new[] { client1, client2 })
        {
            for (var i = 0; i < 2; i++)
            {
                using var accepted = await client.GetAsync(route);
                accepted.StatusCode.Should().Be(HttpStatusCode.NotFound);
            }

            using var rejected = await client.GetAsync(route);
            rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        }

        foreach (var client in new[] { identityClient1, identityClient2 })
        {
            for (var i = 0; i < 2; i++)
            {
                using var accepted = await client.GetAsync(new Uri("/api/v1/me", UriKind.Relative));
                accepted.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            }

            using var rejected = await client.GetAsync(new Uri("/api/v1/me", UriKind.Relative));
            rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        }

        await container.StartAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        while (!connection.IsConnected)
        {
            await Task.Delay(100, timeout.Token);
        }

        using var shared1 = await client1.GetAsync(route);
        using var shared2 = await client2.GetAsync(route);
        using var shared3 = await client1.GetAsync(route);
        shared1.StatusCode.Should().Be(HttpStatusCode.NotFound);
        shared2.StatusCode.Should().Be(HttpStatusCode.NotFound);
        shared3.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        using var identityShared1 = await identityClient1.GetAsync(new Uri("/api/v1/me", UriKind.Relative));
        using var identityShared2 = await identityClient2.GetAsync(new Uri("/api/v1/me", UriKind.Relative));
        using var identityShared3 = await identityClient1.GetAsync(new Uri("/api/v1/me", UriKind.Relative));
        identityShared1.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        identityShared2.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        identityShared3.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task TwoInstances_ShareSubjectLimit_R_006_05()
    {
        var fixture = await RedisTestFixture.SharedAsync();
        using var connection = await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(fixture.ConnectionString);
        using var first = await MarketApiFactory.CreateAsync(configureServices: services => services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(connection));
        using var second = await MarketApiFactory.CreateAsync(configureServices: services => services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(connection));
        var subject = Guid.NewGuid().ToString("D");
        var token = GatewayTestSupport.IssueToken(sub: subject, key: Convert.FromBase64String(MarketApiFactory.SigningKeyBase64));
        using var client1 = WithBearer(first, token);
        using var client2 = WithBearer(second, token);
        var route = new Uri("/api/v1/zonas/lista-espera", UriKind.Relative);
        for (var i = 0; i < 5; i++)
        {
            using var response = await (i % 2 == 0 ? client1 : client2).PostAsJsonAsync(route, new { cp = "91000" });
            response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }

        var redis = first.Services.GetService(typeof(StackExchange.Redis.IConnectionMultiplexer)) as StackExchange.Redis.IConnectionMultiplexer;
        var key = "ratelimit:market:" + Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"waitlist:{subject}")));
        (await redis!.GetDatabase().StringGetAsync(key)).ToString().Should().Be("5");
        using var limited = await client2.PostAsJsonAsync(route, new { cp = "91000" });
        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        limited.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        (await limited.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString().Should().Be("rate_limited");
        (await redis.GetDatabase().KeyExistsAsync(key)).Should().BeTrue();
        key.Should().NotContain(subject);
    }

    private static MarketApiFactory CreateHost(IReadOnlyDictionary<string, string?>? overrides = null) =>
        MarketApiFactory.CreateOffline(_ => { }, overrides);

    private static HttpClient WithBearer(MarketApiFactory factory, string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Theory]
    [InlineData("garbage")]
    [InlineData("")]
    public async Task PublicRoutes_StayAnonymous_EvenWithAnInvalidToken(string token)
    {
        using var factory = CreateHost();
        using var client = token.Length == 0 ? factory.CreateClient() : WithBearer(factory, token);

        using var health = await client.GetAsync(new Uri("/health", UriKind.Relative));
        using var oficios = await client.GetAsync(new Uri("/api/v1/oficios", UriKind.Relative));
        using var unknown = await client.GetAsync(new Uri("/api/v1/esta-ruta-no-existe", UriKind.Relative));

        health.StatusCode.Should().Be(HttpStatusCode.OK);
        oficios.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized, "el catálogo de oficios es público (AllowAnonymous)");
        unknown.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("GET", "/api/v1/colonias/existe")]
    [InlineData("GET", "/api/v1/colonias/en-punto")]
    [InlineData("POST", "/api/v1/zonas/lista-espera")]
    public async Task ProtectedRoutes_Return_TheSameUniform401_WithoutOrWithABadToken(string method, string path)
    {
        using var factory = CreateHost();
        using var anonymous = factory.CreateClient();
        using var garbage = WithBearer(factory, "garbage");
        var results = new List<string>();

        foreach (var client in new[] { anonymous, garbage })
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), new Uri(path, UriKind.Relative));
            using var response = await client.SendAsync(request);
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            response.Headers.WwwAuthenticate.Should().ContainSingle().Which.ToString().Should().Be("Bearer");
            problem.GetProperty("code").GetString().Should().Be("unauthorized");
            results.Add(problem.GetProperty("title").GetString() + "|" + problem.GetProperty("status").GetInt32());
        }

        results.Distinct().Should().ContainSingle("el 401 no distingue entre sin token y token inválido");
    }

    [Fact]
    public async Task OnlyTheWaitlistRoute_ChangesItsCacheControl()
    {
        using var factory = CreateHost();
        using var client = factory.CreateClient();

        using var health = await client.GetAsync(new Uri("/health", UriKind.Relative));
        using var colonias = await client.GetAsync(new Uri("/api/v1/colonias/existe", UriKind.Relative));
        using var waitlist = await client.PostAsJsonAsync(new Uri("/api/v1/zonas/lista-espera", UriKind.Relative), new { cp = "91000" });

        foreach (var other in new[] { health, colonias })
        {
            other.Headers.CacheControl!.NoStore.Should().BeTrue();
            other.Headers.CacheControl.Private.Should().BeFalse("solo la lista de espera lleva datos de una persona");
        }

        waitlist.Headers.CacheControl!.NoStore.Should().BeTrue();
        waitlist.Headers.CacheControl.Private.Should().BeTrue();
    }

    [Fact]
    public async Task GlobalLimit_StaysPerIp_AndAppliesToAnonymousAndAuthenticatedRequestsAlike_ButNeverToHealth()
    {
        var global = new Dictionary<string, string?> { ["RateLimiting:Global:PermitLimit"] = "3", ["RateLimiting:Global:Window"] = "00:10:00" };
        using var factory = CreateHost(global);
        using var anonymous = factory.CreateClient();
        using var signedIn = WithBearer(factory, GatewayTestSupport.IssueToken(key: Convert.FromBase64String(MarketApiFactory.SigningKeyBase64)));
        var route = new Uri("/api/v1/esta-ruta-no-existe", UriKind.Relative);

        using var first = await anonymous.GetAsync(route);
        using var second = await signedIn.GetAsync(route);
        using var third = await anonymous.GetAsync(route);
        using var limited = await signedIn.GetAsync(route);
        using var health = await anonymous.GetAsync(new Uri("/health", UriKind.Relative));

        new[] { first, second, third }.Select(response => response.StatusCode).Should().AllBeEquivalentTo(HttpStatusCode.NotFound);
        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        limited.Headers.RetryAfter.Should().NotBeNull();
        (await limited.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString().Should().Be("rate_limited");
        health.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
