using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Knock.Contracts.Conversations;

namespace Knock.Messaging.Api.Modules.Conversaciones.Data;

/// <summary>
/// Cliente HTTP tipado de Mercado: <c>GET /api/v1/solicitudes/{id}/conversacion</c> reenviando la sesión de quien pide (spec 030, R-09) y con el tiempo límite de configuración
/// (<c>Services:Market</c>, el mismo de la spec 026). Con 200 entendible y completo concede; con 400, 401, 403 o 404 no se distingue (no encontrado); cualquier otro caso (error, tiempo agotado,
/// cuerpo ilegible o incompleto) es <see cref="EstadoPermisoConversacion.NoDisponible"/>: nunca se asume permiso. Un permiso concedido de una conversación conocida se guarda 60 segundos
/// y, mientras dure, responde aunque Mercado no conteste. No se registra nada de la petición: la ruta lleva el identificador de la solicitud. Se registra <c>scoped</c>.
/// </summary>
internal sealed class MarketPermisoDeConversacion(HttpClient http, IPermisoCache cache) : IPermisoDeConversacion
{
    private static readonly string[] _roles = ["cliente", "tecnico"];

    public async Task<ResultadoPermisoConversacion> ConsultarAsync(
        string solicitudId,
        string? tecnicoId,
        string? conversacionId,
        string subject,
        string bearerToken,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(solicitudId);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(bearerToken);

        if (conversacionId is not null && await cache.GetAsync(conversacionId, subject, cancellationToken) is { } cached)
        {
            return new ResultadoPermisoConversacion(EstadoPermisoConversacion.Concedido, cached);
        }

        var result = await AskMarketAsync(solicitudId, tecnicoId, bearerToken, cancellationToken);
        if (conversacionId is not null && result.Permiso is { } permission)
        {
            await cache.SetAsync(conversacionId, subject, permission, cancellationToken);
        }

        return result;
    }

    private static ResultadoPermisoConversacion Unavailable() => new(EstadoPermisoConversacion.NoDisponible);

    private static bool IsComplete(PermisoConversacion? permission) =>
        permission is not null
        && !string.IsNullOrWhiteSpace(permission.SolicitudId)
        && !string.IsNullOrWhiteSpace(permission.TecnicoId)
        && !string.IsNullOrWhiteSpace(permission.ClienteId)
        && !string.IsNullOrWhiteSpace(permission.TecnicoUsuarioId)
        && _roles.Contains(permission.Rol, StringComparer.Ordinal)
        && permission.TecnicoPublico is not null;

    private async Task<ResultadoPermisoConversacion> AskMarketAsync(string solicitudId, string? tecnicoId, string bearerToken, CancellationToken cancellationToken)
    {
        var path = $"api/v1/solicitudes/{Uri.EscapeDataString(solicitudId)}/conversacion";
        if (!string.IsNullOrWhiteSpace(tecnicoId))
        {
            path += $"?tecnicoId={Uri.EscapeDataString(tecnicoId)}";
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        try
        {
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return response.StatusCode switch
            {
                HttpStatusCode.OK => await ReadAsync(response, cancellationToken),
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.BadRequest => new ResultadoPermisoConversacion(EstadoPermisoConversacion.NoEncontrado),
                _ => Unavailable(),
            };
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException or OperationCanceledException)
        {
            // Sin respuesta, con tiempo agotado o ilegible: se cierra el paso. No se registra nada (el mensaje podría traer la ruta con los identificadores).
            cancellationToken.ThrowIfCancellationRequested();
            return Unavailable();
        }
    }

    private static async Task<ResultadoPermisoConversacion> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var permission = await response.Content.ReadFromJsonAsync<PermisoConversacion>(cancellationToken);
        return IsComplete(permission)
            ? new ResultadoPermisoConversacion(EstadoPermisoConversacion.Concedido, permission)
            : Unavailable();
    }
}
