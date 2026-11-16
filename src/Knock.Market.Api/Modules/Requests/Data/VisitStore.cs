using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Requests.Domain;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Market.Api.Modules.Requests.Data;

/// <summary>Visita tal como se guarda (spec 021, §6, y spec 022). <c>CodigoCifrado</c> solo existe en domicilio y nunca sale de este módulo sin descifrarse para el cliente.</summary>
internal sealed record VisitRecord(
    ObjectId Id,
    ObjectId RequestId,
    ObjectId QuoteId,
    string ClientId,
    string TechnicianUserId,
    ObjectId TechnicianId,
    string Modality,
    string Day,
    string Time,
    int? Price,
    int Cap,
    string Estado,
    string? EncryptedCode,
    DateTime? CodeExpiresAtUtc,
    bool CodeUsed,
    int Attempts,
    bool Locked,
    DateTime? ArrivedAtUtc)
{
    public VisitState State => new(Estado, Modality, Day, Locked, CodeUsed, CodeExpiresAtUtc);
}

/// <summary>Resultado de registrar un intento fallido: si se aplicó (la visita seguía abierta a intentos), cuántos van y si quedó bloqueada.</summary>
internal sealed record FailedAttempt(bool Applied, int Attempts, bool Locked);

/// <summary>Razón por la que una visita se cierra (spec 022, R-17). <see cref="Blocked"/> deja el <c>estado</c> en <c>confirmada</c> y pone <c>bloqueada</c>.</summary>
internal enum VisitCloseReason
{
    Cancelled,
    Finished,
    NoShow,
    Blocked,
}

/// <summary>
/// La colección <c>visitas</c> de la spec 022: lectura, llegada con código, aviso y cierre. Cada cambio de estado se escribe junto con su evento en el buzón en la misma
/// transacción (R-17). Todas las actualizaciones llevan su condición, así que dos peticiones simultáneas no pueden ganar las dos. Único punto del módulo con acceso a
/// MongoDB para estas operaciones (constitución, principio 10).
/// </summary>
internal sealed class VisitStore(
    [FromKeyedServices(RequestsModuleExtensions.ModuleKey)] IMongoDatabase database,
    IOptions<KnockMongoOptions> mongoOptions,
    IOutbox outbox)
{
    private const string _stateField = "estado";
    private const string _updatedField = "actualizadoEn";
    private const int _maxConflictAttempts = 5;

    private static readonly string[] _openStates = [VisitRules.Confirmed, VisitRules.InProgress];

    private readonly FindOptions _findOptions = new() { MaxTime = mongoOptions.Value.QueryMaxTime };

    private IMongoCollection<BsonDocument> Visits => database.GetCollection<BsonDocument>(ChoiceStore.VisitsCollection);

    public async Task<VisitRecord?> FindAsync(ObjectId id, CancellationToken cancellationToken)
    {
        var found = await Visits.Find(Builders<BsonDocument>.Filter.Eq("_id", id), _findOptions).Limit(1).ToListAsync(cancellationToken);
        return found.Count == 0 ? null : ToRecord(found[0]);
    }

    /// <summary>
    /// La visita pasa a <c>en_curso</c> con el código ya comprobado: una sola actualización condicionada (<c>confirmada</c>, sin bloqueo, sin usar y con menos de tres
    /// intentos) y el evento <c>VisitaEnCurso</c> en la misma transacción. <see langword="false"/> si otra petición ya la cambió (R-10).
    /// </summary>
    public async Task<bool> MarkArrivedAsync(VisitRecord visit, DateTime nowUtc, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(visit);
        return await InTransactionAsync(async (transaction, token) =>
        {
            var changed = await Visits.UpdateOneAsync(
                transaction,
                OpenToAttempts(visit.Id),
                Builders<BsonDocument>.Update.Set(_stateField, VisitRules.InProgress).Set("codigoUsado", true).Set("llegadaEn", nowUtc).Set(_updatedField, nowUtc),
                cancellationToken: token);
            if (changed.MatchedCount != 1)
            {
                return false;
            }

            await outbox.AddAsync(transaction, OutboxMessage.Create("VisitaEnCurso", visit.Id.ToString(), Identifiers(visit), nowUtc), token);
            return true;
        }, false, cancellationToken);
    }

    /// <summary>
    /// Suma un intento fallido con la misma condición de la llegada. Al llegar al tercero, en la misma transacción pone <c>bloqueada</c> y escribe <c>VisitaBloqueada</c> y
    /// <c>VisitaCerrada</c> (R-08 y R-17). <c>Applied = false</c> si la visita dejó de estar abierta a intentos entre la lectura y el cambio.
    /// </summary>
    public async Task<FailedAttempt> RegisterFailedAttemptAsync(VisitRecord visit, DateTime nowUtc, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(visit);
        return await InTransactionAsync(async (transaction, token) =>
        {
            var after = await Visits.FindOneAndUpdateAsync(
                transaction,
                OpenToAttempts(visit.Id),
                Builders<BsonDocument>.Update.Inc("intentos", 1).Set(_updatedField, nowUtc),
                new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After },
                token);
            if (after is null)
            {
                return new FailedAttempt(false, visit.Attempts, visit.Locked);
            }

            var attempts = after["intentos"].ToInt32();
            if (attempts < VisitRules.MaxAttempts)
            {
                return new FailedAttempt(true, attempts, false);
            }

            await Visits.UpdateOneAsync(transaction, Builders<BsonDocument>.Filter.Eq("_id", visit.Id), Builders<BsonDocument>.Update.Set("bloqueada", true), cancellationToken: token);
            await outbox.AddAsync(transaction, OutboxMessage.Create("VisitaBloqueada", visit.Id.ToString(), Identifiers(visit), nowUtc), token);
            await outbox.AddAsync(transaction, ClosedEvent(visit, VisitCloseReason.Blocked, nowUtc), token);
            return new FailedAttempt(true, attempts, true);
        }, new FailedAttempt(false, visit.Attempts, visit.Locked), cancellationToken);
    }

    /// <summary>
    /// Cierra la visita por <paramref name="reason"/> y escribe <c>VisitaCerrada</c> en la misma transacción (R-17): si el cambio no se aplica, no queda evento. Cancelar y
    /// «no se presentó» salen de <c>confirmada</c>; terminar, de <c>en_curso</c>; bloquear deja el estado y pone <c>bloqueada</c>. <see langword="false"/> si la visita no
    /// estaba en un estado que admita ese cierre. Lo usan las specs de cancelar y terminar (HU-034, HU-039, HU-041).
    /// </summary>
    public async Task<bool> CloseAsync(VisitRecord visit, VisitCloseReason reason, DateTime nowUtc, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(visit);
        return await InTransactionAsync(async (transaction, token) =>
        {
            var filter = Builders<BsonDocument>.Filter;
            var update = Builders<BsonDocument>.Update;
            var (condition, change) = reason switch
            {
                VisitCloseReason.Cancelled => (filter.Eq(_stateField, VisitRules.Confirmed), update.Set(_stateField, "cancelada")),
                VisitCloseReason.NoShow => (filter.Eq(_stateField, VisitRules.Confirmed), update.Set(_stateField, "no_presentado")),
                VisitCloseReason.Finished => (filter.Eq(_stateField, VisitRules.InProgress), update.Set(_stateField, "terminada")),
                _ => (filter.Eq(_stateField, VisitRules.Confirmed) & filter.Eq("bloqueada", false), update.Set("bloqueada", true)),
            };
            var changed = await Visits.UpdateOneAsync(transaction, filter.Eq("_id", visit.Id) & condition, change.Set(_updatedField, nowUtc), cancellationToken: token);
            if (changed.MatchedCount != 1)
            {
                return false;
            }

            await outbox.AddAsync(transaction, ClosedEvent(visit, reason, nowUtc), token);
            return true;
        }, false, cancellationToken);
    }

    /// <summary>
    /// Cuenta un aviso de confianza preparado y escribe <c>AvisoConfianzaPreparado</c> con identificadores, sin texto ni teléfonos (R-13). El contador es solo informativo.
    /// <see langword="false"/> si la visita ya no está <c>confirmada</c> ni <c>en_curso</c>.
    /// </summary>
    public async Task<bool> CountNoticeAsync(VisitRecord visit, DateTime nowUtc, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(visit);
        return await InTransactionAsync(async (transaction, token) =>
        {
            var filter = Builders<BsonDocument>.Filter;
            var after = await Visits.FindOneAndUpdateAsync(
                transaction,
                filter.Eq("_id", visit.Id) & filter.In(_stateField, _openStates),
                Builders<BsonDocument>.Update.Inc("avisosEnviados", 1).Set(_updatedField, nowUtc),
                new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After },
                token);
            if (after is null)
            {
                return false;
            }

            await outbox.AddAsync(transaction, OutboxMessage.Create("AvisoConfianzaPreparado", $"{visit.Id}:{after["avisosEnviados"].ToInt32()}", Identifiers(visit), nowUtc), token);
            return true;
        }, false, cancellationToken);
    }

    /// <summary>
    /// Corre la transacción y reintenta los conflictos de escritura entre peticiones simultáneas (código 112 o la etiqueta <c>TransientTransactionError</c>) hasta cinco veces con una espera
    /// corta. Si el conflicto no se resuelve, devuelve <paramref name="conflictResult"/> (la petición «no se aplicó») y el llamador explica la visita con su estado actual: un conflicto
    /// nunca sale como 500.
    /// </summary>
    private async Task<T> InTransactionAsync<T>(Func<IClientSessionHandle, CancellationToken, Task<T>> body, T conflictResult, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= _maxConflictAttempts; attempt++)
        {
            using var session = await database.Client.StartSessionAsync(cancellationToken: cancellationToken);
            try
            {
                return await session.WithTransactionAsync(body, cancellationToken: cancellationToken);
            }
            catch (MongoException exception) when (IsWriteConflict(exception))
            {
                await Task.Delay(10 * attempt, cancellationToken);
            }
        }

        return conflictResult;
    }

    private static bool IsWriteConflict(MongoException exception) =>
        exception.HasErrorLabel("TransientTransactionError")
        || exception is MongoCommandException { Code: 112 }
        || exception is MongoWriteException { WriteError.Code: 112 };

    private static FilterDefinition<BsonDocument> OpenToAttempts(ObjectId id)
    {
        var filter = Builders<BsonDocument>.Filter;
        return filter.Eq("_id", id) & filter.Eq(_stateField, VisitRules.Confirmed) & filter.Eq("bloqueada", false) & filter.Eq("codigoUsado", false) & filter.Lt("intentos", VisitRules.MaxAttempts);
    }

    private static OutboxMessage ClosedEvent(VisitRecord visit, VisitCloseReason reason, DateTime nowUtc) => OutboxMessage.Create(
        "VisitaCerrada",
        visit.Id.ToString(),
        new BsonDocument { { "visitaId", visit.Id.ToString() }, { "motivo", Reason(reason) } },
        nowUtc);

    private static string Reason(VisitCloseReason reason) => reason switch
    {
        VisitCloseReason.Cancelled => "cancelada",
        VisitCloseReason.Finished => "terminada",
        VisitCloseReason.NoShow => "no_presentado",
        _ => "bloqueada",
    };

    /// <summary>Solo identificadores: nunca el código, la dirección, el nombre ni el teléfono de nadie (R-13 y R-17).</summary>
    private static BsonDocument Identifiers(VisitRecord visit) => new()
    {
        { "visitaId", visit.Id.ToString() },
        { "solicitudId", visit.RequestId.ToString() },
        { "clienteId", visit.ClientId },
        { "tecnicoUsuarioId", visit.TechnicianUserId },
    };

    private static VisitRecord ToRecord(BsonDocument d) => new(
        d["_id"].AsObjectId,
        d["solicitudId"].AsObjectId,
        d["cotizacionId"].AsObjectId,
        d["clienteId"].AsString,
        d["tecnicoUsuarioId"].AsString,
        d["tecnicoId"].AsObjectId,
        d["modalidad"].AsString,
        d["dia"].AsString,
        d["hora"].AsString,
        d.TryGetValue("precio", out var price) && price.IsNumeric ? price.ToInt32() : null,
        d["tope"].ToInt32(),
        d[_stateField].AsString,
        d.TryGetValue("codigoCifrado", out var code) && code.IsString ? code.AsString : null,
        d.TryGetValue("codigoVenceEn", out var expires) && expires.IsBsonDateTime ? expires.ToUniversalTime() : null,
        d.GetValue("codigoUsado", false).AsBoolean,
        d.GetValue("intentos", 0).ToInt32(),
        d.GetValue("bloqueada", false).AsBoolean,
        d.TryGetValue("llegadaEn", out var arrived) && arrived.IsBsonDateTime ? arrived.ToUniversalTime() : null);
}
