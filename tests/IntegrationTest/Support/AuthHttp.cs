using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Knock.IntegrationTest.Support;

/// <summary>Llamadas HTTP de las pruebas a los endpoints de sesión.</summary>
internal static class AuthHttp
{
    public static readonly Uri ExchangeUri = new("/api/v1/auth/firebase", UriKind.Relative);
    public static readonly Uri MeUri = new("/api/v1/me", UriKind.Relative);

    public static Task<HttpResponseMessage> ExchangeAsync(HttpClient client, string firebaseIdToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.PostAsJsonAsync(ExchangeUri, new { firebaseIdToken });
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
