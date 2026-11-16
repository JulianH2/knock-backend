using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Market.Api.Modules.Requests.Data;

/// <summary>
/// Mensaje del buzón de salida (spec 003 y spec 020, §7). El <c>Id</c> es determinista (tipo y agregado), así que un reintento de la misma operación no lo duplica.
/// El <c>Payload</c> solo lleva identificadores y datos del oficio y la colonia: nunca la descripción, la dirección ni datos de la persona.
/// </summary>
internal sealed record OutboxMessage(string Id, string Type, string AggregateId, BsonDocument Payload, DateTime CreatedAtUtc)
{
    public static OutboxMessage Create(string type, string aggregateId, BsonDocument payload, DateTime createdAtUtc) =>
        new($"{type}:{aggregateId}", type, aggregateId, payload, createdAtUtc);
}

/// <summary>
/// Escribe en el buzón de salida dentro de la misma transacción que el cambio de negocio (versión mínima de Mercado, KNOCK-50). El despachador y el transporte
/// los cubre la spec 037; aquí solo se deja el mensaje en estado <c>pendiente</c>.
/// </summary>
internal interface IOutbox
{
    Task AddAsync(IClientSessionHandle session, OutboxMessage message, CancellationToken cancellationToken);
}

internal sealed class MongoOutbox([FromKeyedServices(RequestsModuleExtensions.ModuleKey)] IMongoDatabase database) : IOutbox
{
    public const string CollectionName = "outbox";

    public Task AddAsync(IClientSessionHandle session, OutboxMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(message);
        return database.GetCollection<BsonDocument>(CollectionName).InsertOneAsync(
            session,
            new BsonDocument
            {
                { "_id", message.Id },
                { "tipo", message.Type },
                { "agregadoId", message.AggregateId },
                { "datos", message.Payload },
                { "estado", "pendiente" },
                { "intentos", 0 },
                { "creadoEn", message.CreatedAtUtc },
            },
            cancellationToken: cancellationToken);
    }
}
