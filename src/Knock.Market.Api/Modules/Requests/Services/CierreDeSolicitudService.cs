using Knock.Market.Api.Modules.Requests.Data;
using MongoDB.Bson;

namespace Knock.Market.Api.Modules.Requests.Services;

internal sealed class CierreDeSolicitudService(CierreDeSolicitudStore store, TimeProvider clock)
{
    public Task<CierreResult> CancelAsync(ObjectId id, string clientId, string? reason, CancellationToken cancellationToken) =>
        store.CloseAsync(id, clientId, reason, false, clock.GetUtcNow().UtcDateTime, cancellationToken);

    public Task<CierreResult> ExpireAsync(ObjectId id, CancellationToken cancellationToken) =>
        store.CloseAsync(id, null, null, true, clock.GetUtcNow().UtcDateTime, cancellationToken);
}
