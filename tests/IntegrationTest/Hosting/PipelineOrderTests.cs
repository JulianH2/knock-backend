using System.Net;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Knock.Api.Modules.Identity.Services;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Knock.IntegrationTest.Hosting;

public class PipelineOrderTests
{
    private static readonly TimeSpan _regexTimeout = TimeSpan.FromSeconds(2);

    [Fact]
    public async Task Pipeline_FollowsDocumentedOrder_TC_TT_API_03_16()
    {
        // Correlación antes del manejador de excepciones: el 500 ya trae el id en encabezado y cuerpo.
        using (var factory = new KnockApiFactory())
        using (var client = factory.CreateClient())
        using (var boom = await TestHttp.GetAsync(client, "/api/v1/test/boom"))
        {
            boom.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            boom.Headers.Contains("X-Correlation-Id").Should().BeTrue();
        }

        // El limitador responde antes de autenticar: sin token, 401, 401 y después 429 (no 401).
        using (var factory = new KnockApiFactory(settings: TestHttp.Limits(globalPermits: 2, authPermits: 1000)))
        using (var client = factory.CreateClient())
        {
            var statuses = new List<HttpStatusCode>();
            for (var i = 0; i < 3; i++)
            {
                using var response = await TestHttp.GetAsync(client, "/api/v1/me", "203.0.113.40");
                statuses.Add(response.StatusCode);
            }

            statuses.Should().Equal(HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.TooManyRequests);
        }

        // Autenticación antes de autorización: sin sesión 401 con desafío Bearer; con JWT propio 200.
        using (var factory = new KnockApiFactory())
        using (var client = factory.CreateClient())
        {
            using var anonymous = await TestHttp.GetAsync(client, "/api/v1/test/secure");
            anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            anonymous.Headers.WwwAuthenticate.Should().ContainSingle().Which.Scheme.Should().Be("Bearer");

            using var signedIn = await TestHttp.GetAsync(client, "/api/v1/test/secure", bearerToken: TestHttp.IssueToken(factory, Guid.NewGuid()));
            signedIn.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task RateLimiter_RespondsBeforeFirebaseIsConsulted_TC_TT_API_03_16()
    {
        var verifier = new CountingVerifier();
        using var factory = new KnockApiFactory(
            settings: TestHttp.Limits(globalPermits: 1000, authPermits: 2),
            configureServices: services =>
            {
                services.RemoveAll<IFirebaseIdTokenVerifier>();
                services.AddSingleton<IFirebaseIdTokenVerifier>(verifier);
            });
        using var client = factory.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 3; i++)
        {
            using var response = await TestHttp.SendAsync(
                client, HttpMethod.Post, "/api/v1/auth/firebase", "203.0.113.41", jsonBody: """{"firebaseIdToken":"synthetic-token"}""");
            statuses.Add(response.StatusCode);
        }

        statuses.Should().Equal(HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.TooManyRequests);
        verifier.Calls.Should().Be(2, "la tercera petición se rechaza antes de consultar a Firebase ni emitir sesión");
    }

    [Fact]
    public void DocumentedOrder_MatchesUseKnockApi_TC_TT_API_03_16()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root, "src", "Knock.Api", "Hosting", "KnockApplicationExtensions.cs"));
        var useKnockApi = source[source.IndexOf("UseKnockApi", StringComparison.Ordinal)..];
        var actual = Regex.Matches(useKnockApi, @"app\.(?:UseMiddleware<(\w+)>|(\w+))\(", RegexOptions.None, _regexTimeout)
            .Select(m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value)
            .ToList();

        var documented = File.ReadAllLines(Path.Combine(root, "docs", "MIDDLEWARES.md"))
            .Select(line => Regex.Match(line, @"^\d+\.\s+`(\w+)`", RegexOptions.None, _regexTimeout))
            .Where(m => m.Success)
            .Select(m => m.Groups[1].Value)
            .ToList();

        actual.Should().NotBeEmpty();
        documented.Should().Equal(actual, "docs/MIDDLEWARES.md lista los middlewares en el orden efectivo de UseKnockApi");
        actual.IndexOf("CorrelationIdMiddleware").Should().BeLessThan(actual.IndexOf("UseExceptionHandler"));
        actual.IndexOf("UseRateLimiter").Should().BeLessThan(actual.IndexOf("UseAuthentication"));
        actual.IndexOf("UseAuthentication").Should().BeLessThan(actual.IndexOf("UseAuthorization"));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Knock.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Knock.slnx no se encontró subiendo desde el directorio de pruebas.");
    }

    private sealed class CountingVerifier : IFirebaseIdTokenVerifier
    {
        private int _calls;

        public int Calls => Volatile.Read(ref _calls);

        public Task<FirebaseIdentity?> VerifyAsync(string idToken, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _calls);
            return Task.FromResult<FirebaseIdentity?>(null);
        }
    }
}
