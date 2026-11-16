using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.IntegrationTest.Requests;

/// <summary>
/// Spec 023, T-01: <c>UseAuthentication</c> corre antes de <c>UseRateLimiter</c> en Market (el límite por persona necesita el <c>sub</c>). Comprueba el efecto en
/// el resto de las rutas: las públicas siguen anónimas, las protegidas dan el mismo 401, el límite global sigue siendo por IP y solo la lista de espera cambia
/// su <c>Cache-Control</c>. Host sin contenedores (MongoDB no se usa en estas rutas). Solo datos ficticios.
/// </summary>
public class Spec023MarketPipelineTests
{
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
