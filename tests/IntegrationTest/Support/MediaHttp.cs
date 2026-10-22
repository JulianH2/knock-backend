using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Knock.Api.Modules.Identity.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Knock.IntegrationTest.Support;

/// <summary>Llamadas de las pruebas al módulo Media (API) y directo a Blob con los SAS que emite.</summary>
internal static class MediaHttp
{
    public const string QuarantineContainer = "cuarentena";
    public const string FilesContainer = "archivos";

    private static readonly HttpClient _blob = new() { Timeout = TimeSpan.FromSeconds(30) };

    /// <summary>Host de pruebas conectado a Azurite y, si se pasa, con un reloj falso para los vencimientos.</summary>
    public static KnockApiFactory CreateFactory(AzuriteFixture azurite, TimeProvider? clock = null) => new(
        settings: new Dictionary<string, string?> { ["ConnectionStrings:KnockBlob"] = azurite.ConnectionString },
        configureServices: services =>
        {
            if (clock is not null)
            {
                services.AddSingleton(clock);
            }
        });

    /// <summary>JWT emitido por el emisor real de TT-API-02 para un usuario ficticio nuevo.</summary>
    public static string IssueAccessToken(KnockApiFactory factory) =>
        factory.Services.GetRequiredService<SessionTokenIssuer>().Issue(Guid.CreateVersion7()).AccessToken;

    public static Task<HttpResponseMessage> CreateUploadAsync(HttpClient client, string? accessToken, object body) =>
        SendAsync(client, HttpMethod.Post, "/api/v1/archivos/subidas", accessToken, JsonContent.Create(body));

    public static Task<HttpResponseMessage> ConfirmAsync(HttpClient client, string? accessToken, Guid id) =>
        SendAsync(client, HttpMethod.Post, $"/api/v1/archivos/{id}/confirmar", accessToken, content: null);

    public static Task<HttpResponseMessage> ReadAsync(HttpClient client, string? accessToken, Guid id, string version) =>
        SendAsync(client, HttpMethod.Get, $"/api/v1/archivos/{id}/lectura?version={version}", accessToken, content: null);

    public static object JpegUpload(long sizeBytes) => new { uso = "perfil", contentType = "image/jpeg", tamanoBytes = sizeBytes };

    /// <summary>Pide la subida y devuelve el id y el SAS de escritura.</summary>
    public static async Task<(Guid Id, Uri UploadUrl)> StartUploadAsync(HttpClient client, string accessToken, long declaredBytes)
    {
        using var response = await CreateUploadAsync(client, accessToken, JpegUpload(declaredBytes));
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return (body.RootElement.GetProperty("id").GetGuid(), new Uri(body.RootElement.GetProperty("urlSubida").GetString()!));
    }

    public static async Task<Uri> ReadLinkAsync(HttpClient client, string accessToken, Guid id, string version)
    {
        using var response = await ReadAsync(client, accessToken, id, version);
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return new Uri(body.RootElement.GetProperty("url").GetString()!);
    }

    /// <summary>PUT directo a Blob con un SAS (como haría la app), sin pasar por la API.</summary>
    public static async Task<HttpResponseMessage> PutBlobAsync(Uri sasUrl, byte[] content)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, sasUrl) { Content = new ByteArrayContent(content) };
        request.Headers.Add("x-ms-blob-type", "BlockBlob");
        return await _blob.SendAsync(request);
    }

    public static Task<HttpResponseMessage> GetBlobAsync(Uri sasUrl) => _blob.GetAsync(sasUrl);

    /// <summary>Espera la llamada, libera la respuesta y devuelve solo su código.</summary>
    public static async Task<HttpStatusCode> StatusAsync(this Task<HttpResponseMessage> call)
    {
        ArgumentNullException.ThrowIfNull(call);
        using var response = await call;
        return response.StatusCode;
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, string? accessToken, HttpContent? content)
    {
        ArgumentNullException.ThrowIfNull(client);
        using var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative)) { Content = content };
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await client.SendAsync(request);
    }
}
