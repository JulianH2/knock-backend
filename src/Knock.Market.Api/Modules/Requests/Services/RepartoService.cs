using Knock.Contracts.Catalog;
using Knock.Market.Api.Common.TareasProgramadas;
using Knock.Market.Api.Modules.Requests.Data;
using Knock.Market.Api.Modules.Requests.Domain;
using Microsoft.Extensions.Options;
using MongoDB.Bson;

namespace Knock.Market.Api.Modules.Requests.Services;

internal sealed class RepartoService(
    RepartoStore store,
    CandidatosStore candidates,
    INeighborhoodCenterCatalog catalog,
    IOptions<OleadasOptions> options,
    TimeProvider clock)
{
    public async Task DistributeAsync(TareaReservada task, CancellationToken token)
    {
        var (id, wave) = Parse(task.ClaveNegocio);
        var now = clock.GetUtcNow().UtcDateTime;
        var request = await store.FindAsync(id, token);
        if (request is null) return;
        var colonia = request["coloniaId"].AsString;
        var center = await catalog.FindNeighborhoodCenterAsync(colonia, token);
        var eligible = await candidates.FindAsync(id, request["clienteId"].AsString, request["oficioClave"].AsString, colonia,
            center?.Latitude, center?.Longitude, now, token);
        var selected = SeleccionDeOleada.Select(eligible, [], options.Value);
        await store.DistributeAsync(task, id, wave, eligible, selected, options.Value, now, token);
    }

    public Task ReviewAsync(TareaReservada task, CancellationToken token)
    {
        var (id, wave) = Parse(task.ClaveNegocio);
        return store.ReviewAsync(task, id, wave, options.Value, clock.GetUtcNow().UtcDateTime, token);
    }

    private static (ObjectId Id, int Wave) Parse(string key)
    {
        var parts = key.Split(':');
        if (parts.Length != 2 || !ObjectId.TryParse(parts[0], out var id) || !int.TryParse(parts[1], out var wave) || wave < 1)
            throw new FormatException("Invalid wave task key.");
        return (id, wave);
    }
}

internal sealed class RepartirOleada(RepartoService service) : IManejadorDeTarea
{
    public string Tipo => "RepartirOleada";
    public Task HandleAsync(TareaReservada task, CancellationToken cancellationToken) => service.DistributeAsync(task, cancellationToken);
}

internal sealed class RevisarOleada(RepartoService service) : IManejadorDeTarea
{
    public string Tipo => "RevisarOleada";
    public Task HandleAsync(TareaReservada task, CancellationToken cancellationToken) => service.ReviewAsync(task, cancellationToken);
}
