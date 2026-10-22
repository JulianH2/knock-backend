using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Knock.Api.Common.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Knock.IntegrationTest.Support;

/// <summary>Peticiones de las pruebas del pipeline: IP de conexión simulada, encabezados libres, tokens sintéticos.</summary>
internal static class TestHttp
{
    public static Uri Path(string path) => new(path, UriKind.Relative);

    public static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        string? remoteIp = null,
        IReadOnlyDictionary<string, string>? headers = null,
        string? bearerToken = null,
        string? jsonBody = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        using var request = new HttpRequestMessage(method, Path(path));
        if (remoteIp is not null)
        {
            request.Headers.Add(KnockApiFactory.RemoteIpHeader, remoteIp);
        }

        foreach (var (name, value) in headers ?? new Dictionary<string, string>())
        {
            request.Headers.TryAddWithoutValidation(name, value);
        }

        if (bearerToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        if (jsonBody is not null)
        {
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        }

        return await client.SendAsync(request);
    }

    public static Task<HttpResponseMessage> GetAsync(HttpClient client, string path, string? remoteIp = null, IReadOnlyDictionary<string, string>? headers = null, string? bearerToken = null) =>
        SendAsync(client, HttpMethod.Get, path, remoteIp, headers, bearerToken);

    public static Task<HttpResponseMessage> PostEmptyAsync(HttpClient client, string path, string? remoteIp = null) =>
        SendAsync(client, HttpMethod.Post, path, remoteIp, jsonBody: string.Empty);

    /// <summary>JWT propio con un <c>sub</c> sintético, emitido con la configuración de firma del host de pruebas.</summary>
    public static string IssueToken(KnockApiFactory factory, Guid subject)
    {
        ArgumentNullException.ThrowIfNull(factory);
        var issuer = new Knock.Api.Modules.Identity.Services.SessionTokenIssuer(
            factory.Services.GetRequiredService<JwtSettings>(),
            TimeProvider.System);
        return issuer.Issue(subject).AccessToken;
    }

    public static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    public static Dictionary<string, string?> Limits(int globalPermits, int authPermits, string window = "00:00:30") => new()
    {
        ["RateLimiting:Global:PermitLimit"] = globalPermits.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ["RateLimiting:Global:Window"] = window,
        ["RateLimiting:Auth:PermitLimit"] = authPermits.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ["RateLimiting:Auth:Window"] = window,
    };
}
