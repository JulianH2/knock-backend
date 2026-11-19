using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Knock.Contracts.Visits;
using Knock.Messaging.Api.Modules.Chat.Options;
using Microsoft.Extensions.Options;

namespace Knock.Messaging.Api.Modules.Chat.Data;

/// <summary>
/// La parte de la vista de la visita que Mensajería lee (spec 026, R-06): solo <c>rol</c> y <c>estado</c>. El resto de la vista del cliente, incluidos el código y la dirección,
/// se descarta al deserializar y el cuerpo nunca se registra.
/// </summary>
internal sealed record VisitaVista(string? Rol, string? Estado);

/// <summary>
/// Cliente HTTP tipado de Mercado: <c>GET /api/v1/visitas/{id}</c> reenviando la sesión de quien pide, con el tiempo límite de configuración (R-07, R-08). Cualquier resultado que no sea
/// un 200 entendible o un 401/403/404 es <see cref="VisitMembershipStatus.Unavailable"/>: nunca se asume pertenencia. Se registra <c>scoped</c> y no se captura en un singleton.
/// </summary>
internal sealed class MarketVisitaCatalog(HttpClient http) : IVisitaCatalog
{
    private static readonly string[] _roles = ["cliente", "tecnico"];

    public async Task<VisitMembership> CheckAsync(string visitId, string bearerToken, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(visitId);
        ArgumentException.ThrowIfNullOrWhiteSpace(bearerToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"api/v1/visitas/{Uri.EscapeDataString(visitId)}", UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        try
        {
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return response.StatusCode switch
            {
                HttpStatusCode.OK => await ReadAsync(response, cancellationToken),
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.BadRequest => new VisitMembership(VisitMembershipStatus.NotMember),
                _ => new VisitMembership(VisitMembershipStatus.Unavailable),
            };
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException or OperationCanceledException)
        {
            // Sin respuesta, con tiempo agotado o ilegible: se cierra el paso. No se registra nada (el mensaje podría traer la ruta con el identificador de la visita).
            cancellationToken.ThrowIfCancellationRequested();
            return new VisitMembership(VisitMembershipStatus.Unavailable);
        }
    }

    private static async Task<VisitMembership> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var view = await response.Content.ReadFromJsonAsync<VisitaVista>(cancellationToken);
        return view is { Rol: { } role, Estado: { } state } && _roles.Contains(role, StringComparer.Ordinal)
            ? new VisitMembership(VisitMembershipStatus.Member, role, state)
            : new VisitMembership(VisitMembershipStatus.Unavailable);
    }
}

/// <summary>Registro del cliente de Mercado con su dirección y tiempo límite de configuración y sin los registros HTTP por omisión (que escribirían la ruta con el identificador de la visita).</summary>
internal static class MarketVisitaCatalogRegistration
{
    public static IServiceCollection AddMarketVisitaCatalog(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddHttpClient<IVisitaCatalog, MarketVisitaCatalog>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<MarketServiceOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl!, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(options.TiempoLimiteSegundos);
            })
            .RemoveAllLoggers();
        return services;
    }
}
