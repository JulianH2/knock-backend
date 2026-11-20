using Knock.Market.Api.Common.TareasProgramadas;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Market.Api.Modules.Requests.Data;

internal sealed class TareasProgramadasStore(IMongoDatabase database) : ITareasProgramadasStore
{
    private readonly IMongoCollection<BsonDocument> _tasks = database.GetCollection<BsonDocument>("tareas_programadas");

    public async Task<TareaReservada?> ReserveAsync(DateTime now, CancellationToken cancellationToken, IReadOnlyCollection<string>? types = null)
    {
        var filter = Builders<BsonDocument>.Filter;
        var due = (filter.Eq("estado", "pendiente") & filter.Lte("programadaPara", now))
            | (filter.Eq("estado", "en_proceso") & filter.Lt("reservadaHasta", now));
        if (types is not null)
        {
            due &= filter.In("tipo", types);
        }

        var token = Guid.NewGuid().ToString("N");
        var update = Builders<BsonDocument>.Update.Set("estado", "en_proceso")
            .Set("reservadaHasta", now.AddMinutes(2)).Set("reservaId", token).Set("actualizadoEn", now);
        var task = await _tasks.FindOneAndUpdateAsync(due, update,
            new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After, Sort = Builders<BsonDocument>.Sort.Ascending("programadaPara") },
            cancellationToken);
        return task is null ? null : new TareaReservada(task["_id"].AsObjectId, task["tipo"].AsString, task["claveNegocio"].AsString, token, task.GetValue("intentos", 0).ToInt32());
    }

    public async Task<bool> CompleteAsync(TareaReservada task, DateTime now, CancellationToken cancellationToken) =>
        (await _tasks.UpdateOneAsync(Owned(task), Builders<BsonDocument>.Update.Set("estado", "hecha")
            .Set("actualizadoEn", now).Unset("reservadaHasta").Unset("reservaId"), cancellationToken: cancellationToken)).ModifiedCount == 1;

    public async Task<bool> FailAsync(TareaReservada task, DateTime now, CancellationToken cancellationToken)
    {
        var attempts = task.Intentos + 1;
        var update = Builders<BsonDocument>.Update.Set("estado", attempts >= 5 ? "fallida" : "pendiente")
            .Set("intentos", attempts).Set("actualizadoEn", now).Unset("reservadaHasta").Unset("reservaId");
        if (attempts < 5)
        {
            int[] delays = [5, 30, 120, 600];
            update = update.Set("programadaPara", now.AddSeconds(delays[attempts - 1]));
        }

        return (await _tasks.UpdateOneAsync(Owned(task), update, cancellationToken: cancellationToken)).ModifiedCount == 1;
    }

    // Must be called in the same transaction as the business writes. A write, not a snapshot read,
    // forces a conflict with a concurrent takeover of the lease.
    public async Task<bool> ConfirmLeaseAsync(IClientSessionHandle session, TareaReservada task, DateTime now, CancellationToken cancellationToken)
    {
        var filter = Owned(task) & Builders<BsonDocument>.Filter.Gt("reservadaHasta", now);
        var result = await _tasks.UpdateOneAsync(session, filter, Builders<BsonDocument>.Update.Inc("confirmacionesReserva", 1), cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    private static FilterDefinition<BsonDocument> Owned(TareaReservada task) =>
        Builders<BsonDocument>.Filter.Eq("_id", task.Id) & Builders<BsonDocument>.Filter.Eq("estado", "en_proceso")
        & Builders<BsonDocument>.Filter.Eq("reservaId", task.ReservaId);
}
