using Knock.Market.Api.Common.TareasProgramadas;
using Knock.Market.Api.Modules.Requests.Domain;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Market.Api.Modules.Requests.Data;

internal sealed class RepartoStore(
    [FromKeyedServices(RequestsModuleExtensions.ModuleKey)] IMongoDatabase database,
    ITareasProgramadasStore leases,
    EnviosStore deliveries,
    IOutbox outbox)
{
    private static readonly string[] _quoteStates = ["enviada", "aceptada"];
    private static readonly string[] _openStates = ["enviada", "cotizando"];
    private IMongoCollection<BsonDocument> Requests => database.GetCollection<BsonDocument>("solicitudes");
    private IMongoCollection<BsonDocument> Quotes => database.GetCollection<BsonDocument>("cotizaciones");

    public async Task<BsonDocument?> FindAsync(ObjectId id, CancellationToken token) =>
        await Requests.Find(Builders<BsonDocument>.Filter.Eq("_id", id)).FirstOrDefaultAsync(token);

    public async Task DistributeAsync(TareaReservada task, ObjectId id, int wave, IReadOnlyList<WaveCandidate> candidates,
        IReadOnlyList<WaveSelection> selection, OleadasOptions options, DateTime now, CancellationToken token)
    {
        using var session = await database.Client.StartSessionAsync(cancellationToken: token);
        await session.WithTransactionAsync(async (tx, ct) =>
        {
            if (!await leases.ConfirmLeaseAsync(tx, task, now, ct)) return false;
            var f = Builders<BsonDocument>.Filter;
            var filter = Open(id, now) & f.Eq("oleadaActual", wave - 1);
            var updated = await Requests.UpdateOneAsync(tx, filter, Builders<BsonDocument>.Update.Set("oleadaActual", wave), cancellationToken: ct);
            if (updated.ModifiedCount != 1) return false;
            foreach (var selected in selection)
            {
                var candidate = candidates.Single(x => x.TechnicianId == selected.TechnicianId);
                var technicianId = ObjectId.Parse(candidate.TechnicianId);
                await deliveries.InsertAsync(tx, id, technicianId, candidate.TechnicianUserId!, wave, selected.Reason, now, ct);
                var payload = new BsonDocument { ["solicitudId"] = id.ToString(), ["tecnicoId"] = technicianId.ToString(), ["oleada"] = wave };
                await outbox.AddAsync(tx, OutboxMessage.Create("SolicitudEnviadaATecnico", $"{id}:{technicianId}", payload, now), ct);
            }
            if (wave == options.MaximoOleadas && selection.Count == 0
                && !await deliveries.AnyAsync(tx, id, ct))
            {
                await outbox.AddAsync(tx, OutboxMessage.Create("SolicitudSinTecnicos", id.ToString(), new BsonDocument("solicitudId", id.ToString()), now), ct);
            }
            await new TareasProgramadasStore(database).ScheduleAsync(tx, "RevisarOleada", id, wave, now.AddMinutes(options.MinutosRevision), now, ct);
            return true;
        }, cancellationToken: token);
    }

    public async Task ReviewAsync(TareaReservada task, ObjectId id, int wave, OleadasOptions options, DateTime now, CancellationToken token)
    {
        using var session = await database.Client.StartSessionAsync(cancellationToken: token);
        await session.WithTransactionAsync(async (tx, ct) =>
        {
            if (!await leases.ConfirmLeaseAsync(tx, task, now, ct)) return false;
            var f = Builders<BsonDocument>.Filter;
            var filter = Open(id, now) & f.Eq("oleadaActual", wave);
            var locked = await Requests.UpdateOneAsync(tx, filter, Builders<BsonDocument>.Update.Inc("revisionOleada", 1), cancellationToken: ct);
            if (locked.ModifiedCount != 1 || wave >= options.MaximoOleadas) return false;
            var quoteFilter = f.Eq("solicitudId", id) & f.In("estado", _quoteStates);
            var count = await Quotes.CountDocumentsAsync(tx, quoteFilter, cancellationToken: ct);
            if (count < options.MinimoCotizaciones)
                await new TareasProgramadasStore(database).ScheduleAsync(tx, "RepartirOleada", id, wave + 1, now, now, ct);
            return true;
        }, cancellationToken: token);
    }

    private static FilterDefinition<BsonDocument> Open(ObjectId id, DateTime now)
    {
        var f = Builders<BsonDocument>.Filter;
        return f.Eq("_id", id) & f.In("estado", _openStates) & f.Gt("venceEn", now);
    }
}
