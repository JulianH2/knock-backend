using Knock.Market.Api.Modules.Requests.Domain;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Market.Api.Modules.Requests.Data;

internal sealed record CierreResult(string Status, string Id, DateTime? CancelledAt = null, string? Reason = null);

internal sealed class CierreDeSolicitudStore(
    [FromKeyedServices(RequestsModuleExtensions.ModuleKey)] IMongoDatabase database,
    IOutbox outbox)
{
    private static readonly string[] _open = ["enviada", "cotizando"];
    private static readonly string[] _deliverable = ["activo", "cotizado"];

    public async Task<CierreResult> CloseAsync(ObjectId id, string? clientId, string? reason, bool expire, DateTime now, CancellationToken cancellationToken)
    {
        using var session = await database.Client.StartSessionAsync(cancellationToken: cancellationToken);
        return await session.WithTransactionAsync(async (transaction, token) =>
        {
            var requests = database.GetCollection<BsonDocument>("solicitudes");
            var f = Builders<BsonDocument>.Filter;
            var owner = clientId is null ? f.Empty : f.Eq("clienteId", clientId);
            var request = await requests.Find(transaction, f.Eq("_id", id) & owner).FirstOrDefaultAsync(token);
            if (request is null)
            {
                return new CierreResult("not_found", id.ToString());
            }

            var state = RequestStates.ParseRequest(request["estado"].AsString);
            var destination = expire ? RequestState.Vencida : RequestState.Cancelada;
            if (Classify(request, state, destination, expire) is { } blocked)
            {
                return blocked;
            }

            var condition = CloseFilter(id, owner, expire, now);
            var update = CloseUpdate(destination, reason, expire, now);
            var changed = await requests.UpdateOneAsync(transaction, condition, update, cancellationToken: token);
            if (changed.MatchedCount == 0)
            {
                return new CierreResult("unchanged", id.ToString());
            }

            await CascadeAsync(transaction, request, id, expire, now, token);
            return new CierreResult(RequestStates.ToStored(destination), id.ToString(), expire ? null : now, reason);
        }, cancellationToken: cancellationToken);
    }

    private static CierreResult? Classify(BsonDocument request, RequestState state, RequestState destination, bool expire)
    {
        var id = request["_id"].ToString()!;
        if (!expire && state == RequestState.Cancelada)
        {
            return new CierreResult("cancelada", id, request["canceladaEn"].ToUniversalTime(),
                request.GetValue("motivoCancelacion", BsonNull.Value).IsString ? request["motivoCancelacion"].AsString : null);
        }

        if (TransicionesDeSolicitud.CanClose(state, destination))
        {
            return null;
        }

        return new CierreResult(expire ? "unchanged" : TransicionesDeSolicitud.CancellationError(state)!, id);
    }

    private static FilterDefinition<BsonDocument> CloseFilter(ObjectId id, FilterDefinition<BsonDocument> owner, bool expire, DateTime now)
    {
        var f = Builders<BsonDocument>.Filter;
        var condition = f.Eq("_id", id) & owner & f.In("estado", _open);
        return expire ? condition & f.Lte("venceEn", now) : condition;
    }

    private static UpdateDefinition<BsonDocument> CloseUpdate(RequestState destination, string? reason, bool expire, DateTime now)
    {
        var update = Builders<BsonDocument>.Update.Set("estado", RequestStates.ToStored(destination))
            .Set("actualizadoEn", now).Set(expire ? "vencidaEn" : "canceladaEn", now);
        if (expire)
        {
            return update;
        }

        BsonValue value = reason is null ? BsonNull.Value : new BsonString(reason);
        return update.Set("motivoCancelacion", value);
    }

    private async Task CascadeAsync(IClientSessionHandle transaction, BsonDocument request, ObjectId id, bool expire, DateTime now, CancellationToken token)
    {
        var f = Builders<BsonDocument>.Filter;
        var quotes = database.GetCollection<BsonDocument>("cotizaciones");
        var deliveries = database.GetCollection<BsonDocument>("envios");
        var quoteFilter = f.Eq("solicitudId", id) & f.Eq("estado", "enviada");
        var sent = await quotes.Find(transaction, quoteFilter).ToListAsync(token);
        var code = expire ? "request_expired" : "request_cancelled";
        await quotes.UpdateManyAsync(transaction, quoteFilter,
            Builders<BsonDocument>.Update.Set("estado", "rechazada").Set("motivoRechazo", code).Set("actualizadoEn", now), cancellationToken: token);
        await deliveries.UpdateManyAsync(transaction, f.Eq("solicitudId", id) & f.In("estado", _deliverable),
            Builders<BsonDocument>.Update.Set("estado", "cerrado").Set("cerradoMotivo", code), cancellationToken: token);
        var payload = new BsonDocument
        {
            { "solicitudId", id.ToString() },
            { "tecnicosAvisar", new BsonArray(sent.Select(q => q["tecnicoId"].ToString())) },
        };
        if (expire)
        {
            payload.Add("clienteId", request["clienteId"]);
        }

        await outbox.AddAsync(transaction, OutboxMessage.Create(expire ? "SolicitudVencida" : "SolicitudCancelada", id.ToString(), payload, now), token);
    }
}
