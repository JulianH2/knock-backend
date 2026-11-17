using Knock.Market.Api.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Market.Api.Modules.Requests.Data;

/// <summary>
/// Escribe las solicitudes y cotizaciones de demostración (KNOCK-246, spec 020 T-13) en una sola transacción: o quedan las dos colecciones o ninguna. Solo toca documentos con
/// <c>demo = true</c>; si encuentra otros, no escribe nada. Reutiliza la guarda del cargador de técnicos (solo hosts locales).
/// </summary>
internal sealed class DemoRequestStore(
    [FromKeyedServices(RequestsModuleExtensions.ModuleKey)] IMongoDatabase database,
    IOptions<KnockMongoOptions> options)
{
    private IMongoCollection<BsonDocument> Requests => database.GetCollection<BsonDocument>(RequestsStore.RequestsCollection);

    private IMongoCollection<BsonDocument> Quotes => database.GetCollection<BsonDocument>(RequestsStore.QuotesCollection);

    public async Task ValidateAsync(CancellationToken cancellationToken)
    {
        await DemoTechnicianGuard.ValidateAsync(options.Value.ConnectionString, Requests, cancellationToken);
        await DemoTechnicianGuard.ValidateAsync(options.Value.ConnectionString, Quotes, cancellationToken);
    }

    public async Task SaveAsync(IReadOnlyList<BsonDocument> requests, IReadOnlyList<BsonDocument> quotes, CancellationToken cancellationToken)
    {
        using var session = await database.Client.StartSessionAsync(cancellationToken: cancellationToken);
        await session.WithTransactionAsync(async (transaction, token) =>
        {
            await ReplaceAsync(Requests, transaction, requests, token);
            await ReplaceAsync(Quotes, transaction, quotes, token);
            return true;
        }, cancellationToken: cancellationToken);
    }

    /// <summary>Reemplaza cada documento por su <c>_id</c> y solo si ya era de demo (upsert): repetir la carga no duplica ni pisa datos que no son de demostración.</summary>
    private static async Task ReplaceAsync(IMongoCollection<BsonDocument> collection, IClientSessionHandle transaction, IReadOnlyList<BsonDocument> documents, CancellationToken token)
    {
        if (await collection.Find(transaction, Builders<BsonDocument>.Filter.Ne("demo", true)).Limit(1).AnyAsync(token))
        {
            throw new InvalidOperationException("Demo seed cannot run with non-demo requests or quotes.");
        }

        foreach (var document in documents)
        {
            var match = Builders<BsonDocument>.Filter.Eq("_id", document["_id"]) & Builders<BsonDocument>.Filter.Eq("demo", true);
            await collection.ReplaceOneAsync(transaction, match, document, new ReplaceOptions { IsUpsert = true }, token);
        }
    }
}
