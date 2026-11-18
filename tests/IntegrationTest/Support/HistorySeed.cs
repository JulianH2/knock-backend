using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.IntegrationTest.Support;

/// <summary>Datos ficticios del historial de «Mi casa y mi auto» (spec 041) en la base de Mercado: una solicitud del cliente, con o sin <c>elementoId</c>, y su visita en el estado dado.</summary>
internal static class HistorySeed
{
    public static async Task<ObjectId> JobAsync(IMongoDatabase market, string client, string? element, string trade, string? visitState, string day, ObjectId technician)
    {
        var request = new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "clienteId", client },
            { "oficioClave", "mec" },
            { "oficioNombre", trade },
            { "descripcion", "Texto privado de la solicitud" },
            { "direccionCifrada", "1.cifrada" },
            { "estado", visitState == "terminada" ? "terminada" : "cita_confirmada" },
            { "creadoEn", DateTime.UtcNow },
        };
        if (element is not null)
        {
            request.Add("elementoId", element);
        }

        await market.GetCollection<BsonDocument>("solicitudes").InsertOneAsync(request);
        if (visitState is not null)
        {
            await market.GetCollection<BsonDocument>("visitas").InsertOneAsync(new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() },
                { "solicitudId", request["_id"] },
                { "clienteId", client },
                { "tecnicoUsuarioId", Guid.NewGuid().ToString("D") },
                { "tecnicoId", technician },
                { "dia", day },
                { "hora", "10:00" },
                { "precio", 1234 },
                { "estado", visitState },
            });
        }

        return request["_id"].AsObjectId;
    }

    public static Task AddCarAsync(IMongoDatabase identity, string owner, ObjectId id) =>
        identity.GetCollection<BsonDocument>("mi_casa_y_auto").InsertOneAsync(
            new BsonDocument { { "_id", id }, { "usuarioId", owner }, { "tipo", "auto" }, { "nombre", "Auto de prueba" }, { "creadoEn", DateTime.UtcNow } });
}
