using Knock.Market.Api.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Market.Api.Modules.Requests.Data;

internal sealed class DemoTechnicianStore(
    [FromKeyedServices(RequestsModuleExtensions.ModuleKey)] IMongoDatabase database,
    IOptions<KnockMongoOptions> options)
{
    private IMongoCollection<BsonDocument> Technicians => database.GetCollection<BsonDocument>("tecnicos");

    public Task ValidateAsync(CancellationToken cancellationToken) =>
        DemoTechnicianGuard.ValidateAsync(options.Value.ConnectionString, Technicians, cancellationToken);

    public async Task SaveAsync(IReadOnlyList<BsonDocument> documents, CancellationToken cancellationToken)
    {
        using var session = await database.Client.StartSessionAsync(cancellationToken: cancellationToken);
        await session.WithTransactionAsync(async (transaction, token) =>
        {
            var filter = Builders<BsonDocument>.Filter;
            if (await Technicians.Find(transaction, filter.Ne("demo", true)).Limit(1).AnyAsync(token))
                throw new InvalidOperationException("Demo seed cannot run with non-demo technicians.");
            foreach (var document in documents)
            {
                var match = filter.Eq("_id", document["_id"]) & filter.Eq("demo", true);
                await Technicians.ReplaceOneAsync(transaction, match, document, new ReplaceOptions { IsUpsert = true }, token);
            }
            return true;
        }, cancellationToken: cancellationToken);
    }
}
