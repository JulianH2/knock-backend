using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Knock.IntegrationTest.Support;

/// <summary>Llamadas HTTP de las pruebas a los endpoints de sesión.</summary>
internal static class AuthHttp
{
    public static readonly Uri ExchangeUri = new("/api/v1/auth/firebase", UriKind.Relative);
    public static readonly Uri MeUri = new("/api/v1/me", UriKind.Relative);
    public static readonly Uri RefreshUri = new("/api/v1/auth/refresh", UriKind.Relative);
    public static readonly Uri LogoutUri = new("/api/v1/auth/logout", UriKind.Relative);

    /// <summary>Canje válido en su forma: con <c>deviceId</c> (UUID) y <c>aceptaAviso: true</c>.</summary>
    public static Task<HttpResponseMessage> ExchangeAsync(HttpClient client, string firebaseIdToken, string? deviceId = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.PostAsJsonAsync(ExchangeUri, new { firebaseIdToken, deviceId = deviceId ?? Guid.NewGuid().ToString("D"), aceptaAviso = true });
    }

    /// <summary>Renovación en su forma: refresh token y <c>deviceId</c> del canje.</summary>
    public static Task<HttpResponseMessage> RefreshAsync(HttpClient client, string refreshToken, string deviceId)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.PostAsJsonAsync(RefreshUri, new { refreshToken, deviceId });
    }

    /// <summary>Cierre de sesión: refresh token en el cuerpo y, si se da, el access token en <c>Authorization</c>.</summary>
    public static async Task<HttpResponseMessage> LogoutAsync(HttpClient client, string refreshToken, string? accessToken = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, LogoutUri) { Content = JsonContent.Create(new { refreshToken }) };
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await client.SendAsync(request);
    }

    public static async Task<HttpResponseMessage> MeAsync(HttpClient client, string? accessToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        using var request = new HttpRequestMessage(HttpMethod.Get, MeUri);
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await client.SendAsync(request);
    }

    public static async Task<(string AccessToken, string RefreshToken)> ReadSessionAsync(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return (body.RootElement.GetProperty("accessToken").GetString()!, body.RootElement.GetProperty("refreshToken").GetString()!);
    }
}
