using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Catalog.Dtos;
using Knock.Market.Api.Modules.Catalog.Services;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Market.Api.Modules.Catalog.Data;

internal sealed class WaitlistStore(
    [FromKeyedServices(CatalogModuleExtensions.ModuleKey)] IMongoDatabase database,
    IOptions<KnockMongoOptions> options) : IWaitlistStore
{
    public async Task<WaitlistResult> JoinAsync(string user, WaitlistRequest request, CancellationToken cancellationToken)
    {
        var collection = database.GetCollection<BsonDocument>("listas_espera");
        var filter = Builders<BsonDocument>.Filter.Eq("usuarioId", user) & Builders<BsonDocument>.Filter.Eq("cp", request.Cp);
        var id = ObjectId.GenerateNewId();
        var update = Builders<BsonDocument>.Update.SetOnInsert("_id", id)
            .SetOnInsert("usuarioId", user).SetOnInsert("cp", request.Cp)
            .SetOnInsert("municipio", (BsonValue)(request.Municipio is null ? BsonNull.Value : new BsonString(request.Municipio)))
            .SetOnInsert("creadoEn", DateTime.UtcNow);
        var inserted = false;
        try
        {
            var result = await collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, cancellationToken);
            inserted = result.UpsertedId is not null;
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // A concurrent upsert inserted the same person and postal code.
        }
        var document = await collection.Find(filter, new FindOptions { MaxTime = options.Value.QueryMaxTime }).SingleAsync(cancellationToken);
        return new WaitlistResult(new WaitlistEntry(document["_id"].AsObjectId.ToString(), document["cp"].AsString,
            document.GetValue("municipio", BsonNull.Value).IsBsonNull ? null : document["municipio"].AsString,
            document["creadoEn"].ToUniversalTime()), !inserted);
    }
}
