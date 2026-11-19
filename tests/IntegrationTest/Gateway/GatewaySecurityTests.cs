using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using AwesomeAssertions;
using Knock.Gateway.Hosting;
using Knock.IntegrationTest.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using Xunit;

namespace Knock.IntegrationTest.Gateway;

/// <summary>
/// Spec 009 en el gateway: sesión obligatoria salvo en las rutas públicas, JWT con un solo algoritmo, sesiones canceladas en Redis real, encabezados,
/// CORS y límites por IP y por usuario con contadores en Redis. Solo datos ficticios.
/// </summary>
public sealed class GatewaySecurityTests
{
    private static readonly Uri _protectedUri = new("/api/v1/colonias/en-punto", UriKind.Relative);
    private static readonly Uri _publicCatalogUri = new("/api/v1/oficios", UriKind.Relative);
    private static readonly Uri _searchUri = new("/api/v1/busqueda?q=boiler", UriKind.Relative);

    private static async Task<GatewayHost> StartWithRedisAsync(Dictionary<string, string?>? settings = null)
    {
        var redis = await RedisTestFixture.SharedAsync();
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:KnockRedis"] = redis.ConnectionString,
        };
        foreach (var (key, value) in settings ?? [])
        {
            values[key] = value;
        }

        return await GatewayHost.StartAsync(values);
    }

    private static async Task<HttpResponseMessage> GetAsync(HttpClient client, Uri uri, string? token = null, string? origin = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (origin is not null)
        {
            request.Headers.Add("Origin", origin);
        }

        return await client.SendAsync(request);
    }

    [Fact]
    public async Task ProtectedRouteWithoutToken_Returns401_AndPublicRoutesStayOpen_CA_009_01()
    {
        await using var host = await StartWithRedisAsync();

        using var withoutToken = await GetAsync(host.Client, _protectedUri);
        using var catalog = await GetAsync(host.Client, _publicCatalogUri);
        using var firebase = await host.Client.PostAsync(new Uri("/api/v1/auth/firebase", UriKind.Relative), content: null);
        using var refresh = await host.Client.PostAsync(new Uri("/api/v1/auth/refresh", UriKind.Relative), content: null);
        using var logout = await host.Client.PostAsync(new Uri("/api/v1/auth/logout", UriKind.Relative), content: null);

        withoutToken.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        withoutToken.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        catalog.StatusCode.Should().Be(HttpStatusCode.OK);
        firebase.StatusCode.Should().Be(HttpStatusCode.OK, "el gateway deja pasar la ruta pública; la valida Identidad");
        refresh.StatusCode.Should().Be(HttpStatusCode.OK);
        logout.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "el logout exige sesión");
        host.PathsSeen.Should().NotContain("/api/v1/colonias/en-punto").And.NotContain("/api/v1/auth/logout");
    }

    [Fact]
    public async Task EveryResponse_CarriesTheSecurityHeaders_CA_009_02()
    {
        await using var host = await StartWithRedisAsync();

        using var ok = await GetAsync(host.Client, _protectedUri, GatewayTestSupport.IssueToken());
        using var unauthorized = await GetAsync(host.Client, _protectedUri);
        using var notFound = await GetAsync(host.Client, new Uri("/interno/secreto", UriKind.Relative));

        foreach (var response in new[] { ok, unauthorized, notFound })
        {
            response.Headers.GetValues("X-Content-Type-Options").Should().ContainSingle().Which.Should().Be("nosniff");
            response.Headers.GetValues("X-Frame-Options").Should().ContainSingle().Which.Should().Be("DENY");
            response.Headers.GetValues("Referrer-Policy").Should().ContainSingle().Which.Should().Be("no-referrer");
            response.Headers.Contains("Server").Should().BeFalse();
        }

        ok.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TokenWithAnotherAlgorithmKeyAudienceOrExpiry_Returns401_CA_009_04()
    {
        await using var host = await StartWithRedisAsync();
        var otherKey = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        var unsignedHeader = Base64UrlEncoder.Encode("""{"alg":"none","typ":"JWT"}""");
        var unsignedPayload = Base64UrlEncoder.Encode("""{"sub":"a","sid":"b","iss":"knock-api","aud":"knock-app","exp":4102444800}""");
        var tokens = new[]
        {
            GatewayTestSupport.IssueToken(key: new byte[64], algorithm: SecurityAlgorithms.HmacSha512),
            GatewayTestSupport.IssueToken(key: otherKey),
            GatewayTestSupport.IssueToken(audience: "otra-app"),
            GatewayTestSupport.IssueToken(lifetime: TimeSpan.FromMinutes(-5)),
            $"{unsignedHeader}.{unsignedPayload}.",
            "esto-no-es-un-jwt",
        };

        foreach (var token in tokens)
        {
            using var response = await GetAsync(host.Client, _protectedUri, token);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        host.PathsSeen.Should().BeEmpty();
    }

    [Fact]
    public async Task JwtOfACancelledSession_Returns401EvenIfItHasNotExpired_CA_009_06()
    {
        var redis = await RedisTestFixture.SharedAsync();
        await using var host = await StartWithRedisAsync();
        var cancelled = Guid.NewGuid().ToString("N")[..24];
        var alive = Guid.NewGuid().ToString("N")[..24];
        var cancelledToken = GatewayTestSupport.IssueToken(sid: cancelled);
        var aliveToken = GatewayTestSupport.IssueToken(sid: alive);
        using var before = await GetAsync(host.Client, _protectedUri, cancelledToken);

        await using (var connection = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString))
        {
            await connection.GetDatabase().StringSetAsync(RedisRevokedSessions.KeyPrefix + cancelled, "1", TimeSpan.FromMinutes(5));
        }
        using var after = await GetAsync(host.Client, _protectedUri, cancelledToken);
        using var other = await GetAsync(host.Client, _protectedUri, aliveToken);

        before.StatusCode.Should().Be(HttpStatusCode.OK);
        after.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        other.StatusCode.Should().Be(HttpStatusCode.OK, "solo se cancela esa sesión");
    }

    [Fact]
    public async Task RedisDown_ClosesThePassageWith503_CA_009_06()
    {
        await using var host = await GatewayHost.StartAsync(
            configureServices: services =>
            {
                services.AddSingleton<IRevokedSessions>(new UnavailableRevokedSessions());
                services.AddSingleton<IRequestCounter>(new NoLimitCounter());
            });

        using var response = await GetAsync(host.Client, _protectedUri, GatewayTestSupport.IssueToken());

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        host.PathsSeen.Should().BeEmpty("sin saber si la sesión vive no se deja pasar");
    }

    [Fact]
    public async Task CountersDown_RejectsWith503InsteadOfSkippingTheLimit_CA_009_07()
    {
        await using var host = await GatewayHost.StartAsync(
            configureServices: services =>
            {
                services.AddSingleton<IRevokedSessions>(new NoRevokedSessions());
                services.AddSingleton<IRequestCounter>(new UnavailableCounter());
            });

        using var response = await GetAsync(host.Client, _publicCatalogUri);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task MoreRequestsThanTheIpLimit_Returns429WithRetryAfterInSeconds_CA_009_07()
    {
        await using var host = await StartWithRedisAsync(new Dictionary<string, string?>
        {
            ["RateLimiting:Ip:PermitLimit"] = "3",
            ["RateLimiting:Ip:Window"] = "00:01:00",
            ["RateLimiting:Auth:PermitLimit"] = "1000",
            ["RateLimiting:User:PermitLimit"] = "1000",
        });
        var statuses = new List<HttpStatusCode>();
        HttpResponseMessage? last = null;

        for (var i = 0; i < 4; i++)
        {
            last = await GetAsync(host.Client, _publicCatalogUri);
            statuses.Add(last.StatusCode);
        }

        statuses.Take(3).Should().OnlyContain(status => status == HttpStatusCode.OK);
        statuses[3].Should().Be(HttpStatusCode.TooManyRequests);
        var retryAfter = last!.Headers.GetValues("Retry-After").Should().ContainSingle().Subject;
        int.Parse(retryAfter, System.Globalization.CultureInfo.InvariantCulture).Should().BeInRange(1, 60);
        last.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var raw = await last.Content.ReadAsStringAsync();
        raw.Should().NotContain("127.0.0.1").And.NotContain("ratelimit");
        last.Headers.Contains("X-Content-Type-Options").Should().BeTrue("el 429 también lleva los encabezados de seguridad");
        last.Dispose();
    }

    [Fact]
    public async Task AuthRoutes_HaveAStricterLimitThanTheRest_CA_009_07()
    {
        await using var host = await StartWithRedisAsync(new Dictionary<string, string?>
        {
            ["RateLimiting:Ip:PermitLimit"] = "1000",
            ["RateLimiting:Auth:PermitLimit"] = "2",
            ["RateLimiting:Auth:Window"] = "00:01:00",
            ["RateLimiting:User:PermitLimit"] = "1000",
        });
        var statuses = new List<HttpStatusCode>();

        for (var i = 0; i < 3; i++)
        {
            using var response = await host.Client.PostAsync(new Uri("/api/v1/auth/firebase", UriKind.Relative), content: null);
            statuses.Add(response.StatusCode);
        }

        using var catalog = await GetAsync(host.Client, _publicCatalogUri);
        statuses.Should().Equal(HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.TooManyRequests);
        catalog.StatusCode.Should().Be(HttpStatusCode.OK, "el límite estricto no toca las demás rutas");
    }

    [Fact]
    public async Task MoreRequestsThanTheUserLimit_Returns429ForThatUserOnly_CA_009_07()
    {
        await using var host = await StartWithRedisAsync(new Dictionary<string, string?>
        {
            ["RateLimiting:Ip:PermitLimit"] = "1000",
            ["RateLimiting:Auth:PermitLimit"] = "1000",
            ["RateLimiting:User:PermitLimit"] = "2",
            ["RateLimiting:User:Window"] = "00:01:00",
        });
        var busy = GatewayTestSupport.IssueToken(sub: Guid.NewGuid().ToString("D"));
        var other = GatewayTestSupport.IssueToken(sub: Guid.NewGuid().ToString("D"));
        var statuses = new List<HttpStatusCode>();

        for (var i = 0; i < 3; i++)
        {
            using var response = await GetAsync(host.Client, _protectedUri, busy);
            statuses.Add(response.StatusCode);
        }

        using var otherUser = await GetAsync(host.Client, _protectedUri, other);
        statuses.Should().Equal(HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.TooManyRequests);
        otherUser.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SearchRoute_HasItsOwnLimitPerPerson_AndNeedsASession_CA_027_16()
    {
        await using var host = await StartWithRedisAsync(new Dictionary<string, string?>
        {
            ["RateLimiting:Ip:PermitLimit"] = "1000",
            ["RateLimiting:Auth:PermitLimit"] = "1000",
            ["RateLimiting:User:PermitLimit"] = "1000",
            ["RateLimiting:Search:PermitLimit"] = "2",
            ["RateLimiting:Search:Window"] = "00:01:00",
        });
        var searcher = GatewayTestSupport.IssueToken(sub: Guid.NewGuid().ToString("D"));
        var other = GatewayTestSupport.IssueToken(sub: Guid.NewGuid().ToString("D"));
        var statuses = new List<HttpStatusCode>();
        HttpResponseMessage? limited = null;

        try
        {
            for (var i = 0; i < 3; i++)
            {
                var response = await GetAsync(host.Client, _searchUri, searcher);
                statuses.Add(response.StatusCode);
                if (i < 2)
                {
                    response.Dispose();
                }
                else
                {
                    limited = response;
                }
            }

            using var otherUser = await GetAsync(host.Client, _searchUri, other);
            using var elsewhere = await GetAsync(host.Client, _protectedUri, searcher);
            using var withoutToken = await GetAsync(host.Client, _searchUri);
            statuses.Should().Equal(HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.TooManyRequests);
            limited!.Headers.RetryAfter.Should().NotBeNull("el 429 dice cuándo reintentar");
            otherUser.StatusCode.Should().Be(HttpStatusCode.OK, "el contador es por persona");
            elsewhere.StatusCode.Should().Be(HttpStatusCode.OK, "la búsqueda no gasta el límite de las demás rutas");
            withoutToken.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        finally
        {
            limited?.Dispose();
        }
    }

    [Fact]
    public async Task Cors_AllowsOnlyTheConfiguredOrigins_CA_009_03()
    {
        await using var host = await StartWithRedisAsync(new Dictionary<string, string?>
        {
            ["Security:Cors:AllowedOrigins:0"] = "http://localhost:4200",
        });

        using var allowed = await GetAsync(host.Client, _publicCatalogUri, origin: "http://localhost:4200");
        using var denied = await GetAsync(host.Client, _publicCatalogUri, origin: "https://malicioso.example");

        allowed.Headers.GetValues("Access-Control-Allow-Origin").Should().ContainSingle().Which.Should().Be("http://localhost:4200");
        denied.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }

    [Fact]
    public async Task ProblemBody_HasTheSameShapeAndNoInternalReason_CA_009_01()
    {
        await using var host = await StartWithRedisAsync();
        var expired = GatewayTestSupport.IssueToken(lifetime: TimeSpan.FromMinutes(-5));

        using var withoutToken = await GetAsync(host.Client, _protectedUri);
        using var withExpired = await GetAsync(host.Client, _protectedUri, expired);

        var a = JsonElement.Parse(await withoutToken.Content.ReadAsStringAsync());
        var b = JsonElement.Parse(await withExpired.Content.ReadAsStringAsync());
        a.GetProperty("status").GetInt32().Should().Be(401);
        b.GetProperty("status").GetInt32().Should().Be(401);
        a.GetProperty("title").GetString().Should().Be(b.GetProperty("title").GetString());
        withExpired.Headers.WwwAuthenticate.ToString().Should().Be("Bearer", "sin el motivo del rechazo");
    }
}
