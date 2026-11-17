extern alias MarketApi;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using MarketApi::Knock.Market.Api.Modules.Catalog.Dtos;
using MarketApi::Knock.Market.Api.Modules.Catalog.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Knock.IntegrationTest.Requests;

/// <summary>
/// Spec 023 (T-01 y T-03) sobre el canal HTTP de <c>POST /api/v1/zonas/lista-espera</c>: 401, 413, 422, 400 y 429 en ProblemDetails con el <c>code</c> de la spec 002
/// (R-08, R-09 y R-11) y <c>Cache-Control: private, no-store</c> en todas las respuestas. Host sin contenedores y almacén de mentira: la persistencia se prueba en
/// <see cref="Spec023ListaEsperaTests"/>. Solo datos ficticios.
/// </summary>
public class Spec023WaitlistProblemTests
{
    private const string _route = "/api/v1/zonas/lista-espera";

    /// <summary>Almacén de mentira: cuenta las llamadas y guarda a quién se registró; nunca toca MongoDB.</summary>
    private sealed class RecordingWaitlistStore : IWaitlistStore
    {
        private readonly List<string> _users = [];

        public bool AlreadyRegistered { get; set; }

        public IReadOnlyList<string> Users
        {
            get
            {
                lock (_users)
                {
                    return [.. _users];
                }
            }
        }

        public Task<WaitlistResult> JoinAsync(string user, WaitlistRequest request, CancellationToken cancellationToken)
        {
            lock (_users)
            {
                _users.Add(user);
            }

            var entry = new WaitlistEntry("665f1b2c3d4e5f6071829304", request.Cp!, request.Municipio, DateTime.UtcNow);
            return Task.FromResult(new WaitlistResult(entry, AlreadyRegistered));
        }
    }

    private static (MarketApiFactory Factory, RecordingWaitlistStore Store) CreateHost()
    {
        var store = new RecordingWaitlistStore();
        return (MarketApiFactory.CreateOffline(services => services.AddSingleton<IWaitlistStore>(store)), store);
    }

    private static HttpClient SignedIn(MarketApiFactory factory, string? subject = null)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", GatewayTestSupport.IssueToken(sub: subject ?? Guid.NewGuid().ToString("D"), key: Convert.FromBase64String(MarketApiFactory.SigningKeyBase64)));
        return client;
    }

    private static async Task<JsonElement> ProblemAsync(HttpResponseMessage response, HttpStatusCode status, string? code)
    {
        response.StatusCode.Should().Be(status);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        response.Headers.CacheControl!.Private.Should().BeTrue();
        response.Headers.CacheControl.NoStore.Should().BeTrue();
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("status").GetInt32().Should().Be((int)status);
        if (code is null)
        {
            problem.TryGetProperty("code", out _).Should().BeFalse();
        }
        else
        {
            problem.GetProperty("code").GetString().Should().Be(code);
        }

        return problem;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("garbage")]
    public async Task WithoutAValidSession_Returns401_WithCode_AndNeverReachesTheStore_CA_023_10(string? token)
    {
        var (factory, store) = CreateHost();
        using var host = factory;
        using var client = factory.CreateClient();
        if (token is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await client.PostAsJsonAsync(new Uri(_route, UriKind.Relative), new { cp = "91000" });

        response.Headers.WwwAuthenticate.Should().ContainSingle().Which.Scheme.Should().Be("Bearer");
        await ProblemAsync(response, HttpStatusCode.Unauthorized, "unauthorized");
        store.Users.Should().BeEmpty();
    }

    [Fact]
    public async Task OversizedBody_Returns413_WithCode_AndNeverReachesTheStore_CA_023_11()
    {
        var (factory, store) = CreateHost();
        using var host = factory;
        using var client = SignedIn(factory);
        using var content = new StringContent("{\"cp\":\"91000\",\"extra\":\"" + new string('a', 2048) + "\"}", Encoding.UTF8, "application/json");

        using var response = await client.PostAsync(new Uri(_route, UriKind.Relative), content);

        await ProblemAsync(response, HttpStatusCode.RequestEntityTooLarge, "payload_too_large");
        store.Users.Should().BeEmpty();
    }

    [Theory]
    [InlineData("cp", "9100", null)]
    [InlineData("cp", "ZZleak", null)]
    [InlineData("cp", "910000", null)]
    [InlineData("municipio", "91000", "Tula 123")]
    [InlineData("municipio", "91000", "Tula<script>")]
    public async Task InvalidField_Returns422_WithFieldKeyInCamelCase_AndWithoutEchoingTheValue_CA_023_12(string field, string cp, string? municipio)
    {
        var (factory, store) = CreateHost();
        using var host = factory;
        using var client = SignedIn(factory);
        var sent = field == "cp" ? cp : municipio!;

        using var response = await client.PostAsJsonAsync(new Uri(_route, UriKind.Relative), new { cp, municipio });

        var body = await response.Content.ReadAsStringAsync();
        var problem = await ProblemAsync(response, HttpStatusCode.UnprocessableEntity, "validation_failed");
        problem.GetProperty("errors").EnumerateObject().Select(property => property.Name).Should().Equal(field);
        body.Should().NotContain(sent).And.NotContain(JsonSerializer.Serialize(sent));
        store.Users.Should().BeEmpty();
    }

    [Theory]
    [InlineData("{\"cp\":")]
    [InlineData("null")]
    [InlineData("")]
    public async Task UnreadableBody_Returns400_AndNeverReachesTheStore_CA_023_12(string body)
    {
        var (factory, store) = CreateHost();
        using var host = factory;
        using var client = SignedIn(factory);
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await client.PostAsync(new Uri(_route, UriKind.Relative), content);

        await ProblemAsync(response, HttpStatusCode.BadRequest, null);
        store.Users.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false, HttpStatusCode.Created)]
    [InlineData(true, HttpStatusCode.OK)]
    public async Task Success_UsesTheSubjectOfTheToken_AndIsNeverCached_CA_023_10(bool alreadyRegistered, HttpStatusCode expected)
    {
        var (factory, store) = CreateHost();
        using var host = factory;
        store.AlreadyRegistered = alreadyRegistered;
        var subject = Guid.NewGuid().ToString("D");
        using var client = SignedIn(factory, subject);

        using var response = await client.PostAsJsonAsync(new Uri(_route, UriKind.Relative), new { cp = "91000", usuarioId = "other" });

        response.StatusCode.Should().Be(expected);
        response.Headers.CacheControl!.Private.Should().BeTrue();
        response.Headers.CacheControl.NoStore.Should().BeTrue();
        store.Users.Should().Equal(subject);
    }

    [Fact]
    public async Task SixthRequestOfAPerson_Returns429_WithCode_AndRetryAfter_WhileAnotherPersonStillJoins_CA_023_16()
    {
        var (factory, store) = CreateHost();
        using var host = factory;
        var subject = Guid.NewGuid().ToString("D");
        using var client = SignedIn(factory, subject);
        var route = new Uri(_route, UriKind.Relative);
        for (var i = 0; i < 5; i++)
        {
            using var accepted = await client.PostAsJsonAsync(route, new { cp = "9100" + i });
            accepted.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        using var limited = await client.PostAsJsonAsync(route, new { cp = "91009" });
        using var other = SignedIn(factory);
        using var otherPerson = await other.PostAsJsonAsync(route, new { cp = "91009" });

        limited.Headers.RetryAfter.Should().NotBeNull();
        await ProblemAsync(limited, HttpStatusCode.TooManyRequests, "rate_limited");
        store.Users.Count(user => user == subject).Should().Be(5);
        otherPerson.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
