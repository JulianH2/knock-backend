using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Knock.IntegrationTest.RateLimiting;

/// <summary>API4: cada endpoint público está bajo un límite de peticiones; la única exención documentada es <c>/health</c>.</summary>
public class PublicEndpointsRateLimitTests
{
    private const int _permits = 3;

    /// <summary>Endpoints públicos de la API en Development (los de <c>tests/IntegrationTest/Support</c> no cuentan). Uno nuevo hace fallar la prueba hasta que se documente.</summary>
    private static readonly string[] _documentedPublicEndpoints =
    [
        "ANY /health",
        "ANY /health/ready",
        "POST /api/v{version:apiVersion}/auth/firebase",
        "POST /api/v{version:apiVersion}/auth/refresh",
        "POST /api/v{version:apiVersion}/auth/logout",
        "GET /api/v{version:apiVersion}/oficios",
        "GET /openapi/{documentName}.json",
        "GET /scalar/{documentName?}",
        "GET /scalar/scalar.js",
        "GET /scalar/scalar.aspnetcore.js",
        "GET /scalar/favicon.svg",
    ];

    private static Dictionary<string, string?> Settings() => new(TestHttp.Limits(globalPermits: _permits, authPermits: _permits))
    {
        ["RateLimiting:Ready:PermitLimit"] = _permits.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ["RateLimiting:Ready:Window"] = "00:00:30",
        ["Mongo:ServerSelectionTimeoutSeconds"] = "1",
    };

    private static List<string> PublicEndpoints(KnockApiFactory factory) =>
        factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()
            .Where(e => e.Metadata.GetMetadata<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()?.ControllerTypeInfo.Namespace != typeof(TestController).Namespace)
            .Where(e => e.Metadata.GetMetadata<IAllowAnonymous>() is not null || e.Metadata.GetMetadata<IAuthorizeData>() is null)
            .SelectMany(e => (e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["ANY"]).Where(m => m != "OPTIONS").Select(m => $"{m} /{e.RoutePattern.RawText?.TrimStart('/')}"))
            .Distinct()
            .Order(StringComparer.Ordinal)
            .ToList();

    [Fact]
    public async Task EveryPublicEndpointIsRateLimitedExceptTheDocumentedExemptions_TC_TT_API_06_16()
    {
        using var factory = new KnockApiFactory(settings: Settings());
        using var client = factory.CreateClient();

        PublicEndpoints(factory).Should().BeEquivalentTo(_documentedPublicEndpoints, "la lista de endpoints públicos es la documentada");

        // Ruta concreta de cada endpoint; /health/ready tiene su política, /auth la estricta y el resto la global.
        var requests = new (string Name, HttpMethod Method, string Path)[]
        {
            ("auth (estricto)", HttpMethod.Post, "/api/v1/auth/firebase"),
            ("auth refresh (estricto)", HttpMethod.Post, "/api/v1/auth/refresh"),
            ("auth logout (estricto)", HttpMethod.Post, "/api/v1/auth/logout"),
            ("ready (propio)", HttpMethod.Get, "/health/ready"),
            ("oficios (global)", HttpMethod.Get, "/api/v1/oficios"),
            ("openapi (global)", HttpMethod.Get, "/openapi/v1.json"),
            ("scalar (global)", HttpMethod.Get, "/scalar/v1"),
            ("ruta inexistente (global)", HttpMethod.Get, "/api/v1/does-not-exist"),
        };
        for (var i = 0; i < requests.Length; i++)
        {
            var (name, method, path) = requests[i];
            var ip = $"203.0.113.{100 + i}";
            for (var n = 1; n <= _permits; n++)
            {
                using var allowed = await TestHttp.SendAsync(client, method, path, ip, jsonBody: method == HttpMethod.Post ? string.Empty : null);
                allowed.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests, $"{name}: la petición {n} está dentro del límite");
            }

            using var limited = await TestHttp.SendAsync(client, method, path, ip, jsonBody: method == HttpMethod.Post ? string.Empty : null);
            limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests, $"{name}: la siguiente supera el límite");
        }

        // Única exención documentada: /health.
        for (var n = 1; n <= _permits * 4; n++)
        {
            using var health = await TestHttp.GetAsync(client, "/health", "203.0.113.120");
            health.StatusCode.Should().Be(HttpStatusCode.OK, $"/health está exento (petición {n})");
        }
    }
}
