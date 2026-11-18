using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Knock.Identity.Api.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Knock.Identity.Api.Modules.HomeAndCar.Services;

/// <summary>Sección <c>Services:Market</c> vista por «Mi casa y mi auto»: dirección interna de Mercado y la clave del token de servicio (<c>interno-key-identity</c>). Sin ellas el historial responde 503.</summary>
internal sealed class MarketHistoryOptions
{
    public const string SectionName = "Services:Market";

    public string? BaseUrl { get; set; }

    /// <summary>Clave HMAC (Base64, 32 bytes o más) con la que Identidad firma el token de servicio hacia Mercado. Es un secreto: nunca se registra ni se devuelve.</summary>
    public string? InternalKey { get; set; }

    [System.ComponentModel.DataAnnotations.Range(1, 30)]
    public int TimeoutSeconds { get; set; } = 3;
}

/// <summary>Un trabajo terminado tal como lo devuelve Mercado (spec 041, R-17): día de la visita, oficio y técnico.</summary>
internal sealed record HistoryEntry(string Fecha, string Oficio, string Tecnico);

internal enum HistoryStatus
{
    Ok,
    InvalidCursor,
    Unavailable,
}

/// <summary>Resultado de pedir el historial a Mercado: la página, o por qué no hay.</summary>
internal sealed record HistoryResult(HistoryStatus Status, IReadOnlyList<HistoryEntry> Items, string? NextCursor)
{
    public static HistoryResult Failure(HistoryStatus status) => new(status, [], null);
}

/// <summary>Lo que «Mi casa y mi auto» necesita de Mercado (spec 041, R-17); existe para probar el servicio con dobles.</summary>
internal interface IMarketHistory
{
    /// <summary>Visitas terminadas del cliente ligadas al elemento, de la más reciente a la más antigua, por cursor. Nunca lanza por un fallo de Mercado: lo devuelve en el resultado.</summary>
    Task<HistoryResult> ListAsync(string clientId, string elementId, string? cursor, int limit, CancellationToken cancellationToken);
}

/// <summary>
/// Cliente de la ruta interna de Mercado <c>GET /api/v1/interno/historial</c> (spec 041, R-17). Firma un token de servicio HS256 de un minuto (<c>iss</c> <c>knock-identity</c>, <c>aud</c> <c>knock-market</c>,
/// <c>jti</c> único y el claim <c>permisos</c>) que además lleva el cliente y el elemento pedidos, para que no sirva con otros. No reenvía la sesión de la persona ni crea otra credencial. Cualquier fallo —sin
/// dirección o sin clave, tiempo agotado, respuesta de error o cuerpo ilegible— sale como <see cref="HistoryStatus.Unavailable"/>: no se inventa un historial.
/// </summary>
internal sealed class MarketHistoryClient(HttpClient http, IOptions<MarketHistoryOptions> options, IHttpContextAccessor accessor, TimeProvider clock) : IMarketHistory
{
    public const string Issuer = "knock-identity";
    public const string Audience = "knock-market";
    public const string HistoryPermission = "historial";
    public const string InvalidCursorProblem = "invalid_cursor";
    public const int MinKeyBytes = 32;
    public static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(1);

    public async Task<HistoryResult> ListAsync(string clientId, string elementId, string? cursor, int limit, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(elementId);
        if (http.BaseAddress is null || ReadKey(options.Value.InternalKey) is not { } key)
        {
            return HistoryResult.Failure(HistoryStatus.Unavailable);
        }

        var query = $"usuarioId={Uri.EscapeDataString(clientId)}&elementoId={Uri.EscapeDataString(elementId)}&limite={limit}"
            + (string.IsNullOrEmpty(cursor) ? string.Empty : "&cursor=" + Uri.EscapeDataString(cursor));
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("api/v1/interno/historial?" + query, UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(key, clientId, elementId));
        if (accessor.HttpContext is { } context && CorrelationId.Current(context) is { } correlationId)
        {
            request.Headers.TryAddWithoutValidation(CorrelationId.HeaderName, correlationId);
        }

        try
        {
            using var response = await http.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.BadRequest && await IsInvalidCursorAsync(response, cancellationToken))
            {
                return HistoryResult.Failure(HistoryStatus.InvalidCursor);
            }

            if (!response.IsSuccessStatusCode)
            {
                return HistoryResult.Failure(HistoryStatus.Unavailable);
            }

            var body = await response.Content.ReadFromJsonAsync<MarketPage>(cancellationToken);
            return body?.Items is { } items && items.All(item => item is { Fecha.Length: > 0, Oficio.Length: > 0, Tecnico.Length: > 0 })
                ? new HistoryResult(HistoryStatus.Ok, [.. items.Select(item => new HistoryEntry(item.Fecha!, item.Oficio!, item.Tecnico!))], body.NextCursor)
                : HistoryResult.Failure(HistoryStatus.Unavailable);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException or NotSupportedException or InvalidOperationException)
        {
            // Mercado no responde a tiempo, la conexión falló o el cuerpo no se entiende: nunca se repite lo que llegó. Si cancela quien llama, la cancelación sigue su camino.
            cancellationToken.ThrowIfCancellationRequested();
            return HistoryResult.Failure(HistoryStatus.Unavailable);
        }
    }

    internal static byte[]? ReadKey(string? configured)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            return null;
        }

        try
        {
            var key = Convert.FromBase64String(configured);
            return key.Length >= MinKeyBytes ? key : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private string IssueToken(byte[] key, string clientId, string elementId)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Claims = new Dictionary<string, object>
            {
                ["jti"] = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16)),
                ["permisos"] = new[] { HistoryPermission },
                ["usuarioId"] = clientId,
                ["elementoId"] = elementId,
            },
            IssuedAt = now,
            NotBefore = now,
            Expires = now + TokenLifetime,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256),
        });
    }

    private static async Task<bool> IsInvalidCursorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<MarketProblem>(cancellationToken);
            return string.Equals(problem?.Code, InvalidCursorProblem, StringComparison.Ordinal);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            return false;
        }
    }

    private sealed record MarketPage(IReadOnlyList<MarketEntry>? Items, string? NextCursor);

    private sealed record MarketEntry(string? Fecha, string? Oficio, string? Tecnico);

    private sealed record MarketProblem(string? Code);
}
