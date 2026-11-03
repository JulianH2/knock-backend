using System.Net;
using System.Net.Http.Headers;
using System.Text;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Knock.IntegrationTest.Support;

/// <summary>Ayudas de las pruebas del pipeline de seguridad (TT-API-06). Orígenes, hosts e IP ficticios.</summary>
internal static class SecurityHttp
{
    public const string PanelOrigin = "https://panel.example.test";
    public const string ApiHost = "https://api.example.test";
    public const string TrustedProxyIp = "10.0.0.1";
    public const string UntrustedIp = "203.0.113.99";

    public static readonly string[] SecurityHeaderNames = ["X-Content-Type-Options", "X-Frame-Options", "Content-Security-Policy", "Referrer-Policy"];

    /// <summary>Configuración con un origen permitido para CORS.</summary>
    public static Dictionary<string, string?> WithCors(params string[] origins)
    {
        var settings = new Dictionary<string, string?>();
        for (var i = 0; i < origins.Length; i++)
        {
            settings[$"Security:Cors:AllowedOrigins:{i}"] = origins[i];
        }

        return settings;
    }

    public static Dictionary<string, string?> WithTrustedProxy(string ip = TrustedProxyIp) => new() { ["ForwardedHeaders:KnownProxies:0"] = ip };

    public static Dictionary<string, string?> Merge(params Dictionary<string, string?>[] parts)
    {
        var all = new Dictionary<string, string?>();
        foreach (var part in parts)
        {
            foreach (var (key, value) in part)
            {
                all[key] = value;
            }
        }

        return all;
    }

    /// <summary>Cliente que no sigue redirecciones y, si se pide, llama a un host HTTPS (HSTS excluye <c>localhost</c>, así que no sirve para probarlo).</summary>
    public static HttpClient Client(KnockApiFactory factory, string baseAddress = ApiHost)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri(baseAddress) });
    }

    public static async Task<HttpResponseMessage> OversizedExchangeAsync(HttpClient client, string ip)
    {
        ArgumentNullException.ThrowIfNull(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, TestHttp.Path("/api/v1/auth/firebase"));
        request.Headers.Add(KnockApiFactory.RemoteIpHeader, ip);
        request.Content = new StringContent(new string('a', 9000), Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
        return await client.SendAsync(request);
    }

    /// <summary>
    /// Una respuesta de cada capa que escribe respuestas: 200, 401 (autenticación), 404 (ruta inexistente), 413 (límite de cuerpo), 422 (validación),
    /// 429 (limitador, con un límite global de 1 por IP), 500 (manejador de excepciones), <c>/health</c> y <c>/health/ready</c>. Cada capa usa su IP.
    /// </summary>
    public static async Task<List<(string Layer, HttpStatusCode Expected, HttpResponseMessage Response)>> LayersAsync(
        HttpClient client, IReadOnlyDictionary<string, string>? headers = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        var layers = new List<(string, HttpStatusCode, HttpResponseMessage)>
        {
            ("200 ok", HttpStatusCode.OK, await TestHttp.GetAsync(client, "/api/v1/test/ping", "203.0.113.31", headers)),
            ("401 authentication", HttpStatusCode.Unauthorized, await TestHttp.GetAsync(client, "/api/v1/me", "203.0.113.32", headers)),
            ("404 unknown route", HttpStatusCode.NotFound, await TestHttp.GetAsync(client, "/api/v1/does-not-exist", "203.0.113.33", headers)),
            ("413 body limit", HttpStatusCode.RequestEntityTooLarge, await OversizedExchangeAsync(client, "203.0.113.34")),
            ("422 validation", (HttpStatusCode)422, await TestHttp.SendAsync(client, HttpMethod.Post, "/api/v1/test/validated", "203.0.113.35", headers, jsonBody: """{"phone":"x"}""")),
            ("500 exception handler", HttpStatusCode.InternalServerError, await TestHttp.GetAsync(client, "/api/v1/test/boom", "203.0.113.36", headers)),
            ("health", HttpStatusCode.OK, await TestHttp.GetAsync(client, "/health", "203.0.113.37", headers)),
            ("health ready", HttpStatusCode.ServiceUnavailable, await TestHttp.GetAsync(client, "/health/ready", "203.0.113.39", headers)),
        };
        using var first = await TestHttp.GetAsync(client, "/api/v1/test/ping", "203.0.113.38", headers);
        layers.Add(("429 rate limiter", HttpStatusCode.TooManyRequests, await TestHttp.GetAsync(client, "/api/v1/test/ping", "203.0.113.38", headers)));
        return layers;
    }

    public static void ShouldCarrySecurityHeaders(HttpResponseMessage response, string because)
    {
        ArgumentNullException.ThrowIfNull(response);
        HeaderValues(response, "X-Content-Type-Options").Should().Equal(["nosniff"], because);
        HeaderValues(response, "X-Frame-Options").Should().Equal(["DENY"], because);
        HeaderValues(response, "Content-Security-Policy").Should().Equal(["default-src 'none'; frame-ancestors 'none'"], because);
        HeaderValues(response, "Referrer-Policy").Should().Equal(["no-referrer"], because);
        HeaderValues(response, "Permissions-Policy").Should().BeEmpty(because);
        HeaderValues(response, "X-XSS-Protection").Should().BeEmpty(because);
    }

    public static string[] HeaderValues(HttpResponseMessage response, string name)
    {
        ArgumentNullException.ThrowIfNull(response);
        return response.Headers.TryGetValues(name, out var values) || response.Content.Headers.TryGetValues(name, out values) ? [.. values] : [];
    }

    public static void Dispose(IEnumerable<(string Layer, HttpStatusCode Expected, HttpResponseMessage Response)> layers)
    {
        ArgumentNullException.ThrowIfNull(layers);
        foreach (var (_, _, response) in layers)
        {
            response.Dispose();
        }
    }
}
