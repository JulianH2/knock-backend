using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;

namespace Knock.SecurityTest.Support;

/// <summary>Llamadas HTTP y comprobaciones comunes de las pruebas de seguridad.</summary>
internal static class SecureHttp
{
    /// <summary>Encabezados que ningún error ni respuesta debe llevar (revelan la pila o la versión del servidor).</summary>
    public static readonly string[] LeakyHeaders = ["Server", "X-Powered-By", "X-AspNet-Version"];

    /// <summary>Texto que nunca debe aparecer en el cuerpo de un error: pistas de pila, motivos de rechazo y mensajes de la librería de JWT.</summary>
    public static readonly string[] ForbiddenInErrors =
    [
        "stackTrace", "StackTrace", "System.", "Microsoft.", "MongoDB", "   at ", "Exception", "IDX1", "IDX2", "invalid_token",
        "expired", "signature", "revoked", "reuse", "not found", "no existe", "mongodb://", "password",
    ];

    public static Task<HttpResponseMessage> PostJsonAsync(HttpClient client, string path, object body, string? bearer = null, string? remoteIp = null) =>
        SendAsync(client, HttpMethod.Post, path, JsonContent.Create(body), bearer, remoteIp);

    /// <summary>Envía el texto tal cual como <c>application/json</c> (para JSON mal formado o con tipos incorrectos).</summary>
    public static Task<HttpResponseMessage> SendRawJsonAsync(HttpClient client, HttpMethod method, string path, string json, string? bearer = null, string? remoteIp = null) =>
        SendAsync(client, method, path, new StringContent(json, Encoding.UTF8, "application/json"), bearer, remoteIp);

    public static async Task<HttpResponseMessage> SendAsync(
        HttpClient client, HttpMethod method, string path, HttpContent? content = null, string? bearer = null, string? remoteIp = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        using var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative)) { Content = content };
        if (bearer is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        }

        if (remoteIp is not null)
        {
            request.Headers.Add(SecurityIdentityFactory.RemoteIpHeader, remoteIp);
        }

        return await client.SendAsync(request);
    }

    /// <summary>Petición con el encabezado <c>Authorization</c> exactamente como se indica (esquema distinto, valor vacío…).</summary>
    public static async Task<HttpResponseMessage> SendWithRawAuthorizationAsync(HttpClient client, HttpMethod method, string path, string? authorization, HttpContent? content = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        using var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative)) { Content = content };
        if (authorization is not null)
        {
            request.Headers.TryAddWithoutValidation("Authorization", authorization);
        }

        return await client.SendAsync(request);
    }

    public static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return JsonElement.Parse(await response.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// Forma de un error: <c>application/problem+json</c> con <c>status</c> igual al código, sin pila, sin motivos internos y sin los textos
    /// indicados (celular, tokens…). Los textos se buscan sin el <c>traceId</c> ni el <c>correlationId</c>, aleatorios, que a veces los contienen.
    /// </summary>
    public static async Task<string> AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, params string[] mustNotContain)
    {
        ArgumentNullException.ThrowIfNull(response);
        response.StatusCode.Should().Be(status);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var raw = await response.Content.ReadAsStringAsync();
        var body = JsonElement.Parse(raw);
        body.GetProperty("status").GetInt32().Should().Be((int)status);
        body.GetProperty("title").GetString().Should().NotBeNullOrWhiteSpace();
        body.TryGetProperty("stackTrace", out _).Should().BeFalse("un error nunca lleva la pila");
        var comparable = WithoutCorrelationId(raw);
        foreach (var forbidden in ForbiddenInErrors.Concat(mustNotContain).Where(s => s.Length > 0))
        {
            comparable.Should().NotContain(forbidden, $"el error no debe revelar '{forbidden}'");
        }

        foreach (var header in LeakyHeaders)
        {
            response.Headers.Contains(header).Should().BeFalse($"no debe salir el encabezado {header}");
        }

        return raw;
    }

    /// <summary>El cuerpo de un error sin el <c>correlationId</c>, el <c>traceId</c> (distintos en cada petición) ni el <c>instance</c> (la ruta de la petición, con el id del recurso): sirve para comparar dos errores.</summary>
    public static string WithoutCorrelationId(string problemJson)
    {
        var body = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(problemJson) ?? [];
        body.Remove("correlationId");
        body.Remove("traceId");
        body.Remove("instance");
        return JsonSerializer.Serialize(body.OrderBy(p => p.Key, StringComparer.Ordinal).ToDictionary(p => p.Key, p => p.Value));
    }

    /// <summary>Encabezados de una respuesta con token o dato personal: nada de caché, sin sniffing.</summary>
    public static void AssertNotCacheable(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);
        response.Headers.CacheControl?.NoStore.Should().BeTrue();
        response.Headers.CacheControl.Should().NotBeNull();
        response.Headers.GetValues("X-Content-Type-Options").Should().ContainSingle().Which.Should().Be("nosniff");
        response.Content.Headers.ContentEncoding.Should().BeEmpty("las respuestas con tokens no se comprimen");
    }
}
