using Knock.Market.Api.Modules.Requests.Data;
using Knock.Market.Api.Modules.Requests.Dtos;
using MongoDB.Bson;

namespace Knock.Market.Api.Modules.Requests.Services;

internal sealed record InboxNeighborhood(string Nombre, string Cp);
internal sealed record InboxItem(string SolicitudId, RequestTrade Oficio, InboxNeighborhood Colonia, string Descripcion,
    IReadOnlyList<RequestPhoto> Fotos, string Dia, string Franja, string Modalidad, DateTime VenceEn,
    int Oleada, DateTime RecibidaEn, bool YaCotizo);
internal sealed record InboxResponse(IReadOnlyList<InboxItem> Items, string? NextCursor);

internal sealed class BandejaService(ITechnicianDirectory technicians, EnviosStore sends, RequestsStore requests,
    IRequestPhotos photos, TimeProvider clock)
{
    public Task<bool> IsTechnicianAsync(string sub, CancellationToken token) => technicians.ExistsAsync(sub, token);

    public async Task<InboxResponse> ListAsync(string sub, string? cursor, int limit, CancellationToken token)
    {
        var page = await sends.ListAsync(sub, cursor, limit, clock.GetUtcNow().UtcDateTime, token);
        var items = new List<InboxItem>();
        foreach (var entry in page.Items)
        {
            var r = entry.Request;
            items.Add(new InboxItem(r.Id.ToString(), new RequestTrade(r.OficioClave, r.OficioNombre),
                new InboxNeighborhood(r.ColoniaNombre, r.ColoniaCp), r.Descripcion,
                await photos.ReadLinksAsync(r.Fotos, token), r.Dia, r.Franja, r.Modalidad, r.VenceEnUtc,
                entry.Wave, entry.ReceivedAt, await requests.HasQuotedAsync(sub, r.Id, token)));
        }
        return new InboxResponse(items, page.NextCursor);
    }

    public async Task<string?> DiscardAsync(string sub, ObjectId id, CancellationToken token)
    {
        var technician = await technicians.FindBySubAsync(sub, token);
        if (technician is null || !ObjectId.TryParse(technician.Id, out var technicianId)) return null;
        return await sends.DiscardAsync(id, technicianId, clock.GetUtcNow().UtcDateTime, token);
    }
}
