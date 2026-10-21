using AwesomeAssertions;
using Knock.Spike.DocumentDb.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.Spike.DocumentDb;

/// <summary>P4: solicitud + evento de outbox en una transacción multi-documento.</summary>
public sealed class DocumentDbTransactionTests
{
    [Theory]
    [LocalTargets]
    [AzureTarget]
    public async Task RequestAndOutboxCommitTogetherAndAbortTogether_TC_TT_BD_05_04(string target)
    {
        await using var spike = await PreparedAsync(target);
        var committed = ObjectId.GenerateNewId();
        var aborted = ObjectId.GenerateNewId();

        using (var session = await spike.Client.StartSessionAsync())
        {
            session.StartTransaction();
            await InsertRequestAndEventAsync(spike.Database, session, committed);
            await session.CommitTransactionAsync();
        }

        using (var session = await spike.Client.StartSessionAsync())
        {
            session.StartTransaction();
            await InsertRequestAndEventAsync(spike.Database, session, aborted);
            await session.AbortTransactionAsync();
        }

        (await CountAsync(spike.Database, committed)).Should().Be((1, 1));
        (await CountAsync(spike.Database, aborted)).Should().Be((0, 0));
    }

    [Theory]
    [LocalTargets]
    [AzureTarget]
    public async Task WithTransaction_ExceptionAfterFirstInsert_LeavesNoDocuments_TC_TT_BD_05_04(string target)
    {
        await using var spike = await PreparedAsync(target);
        var requestId = ObjectId.GenerateNewId();
        using var session = await spike.Client.StartSessionAsync();

        var act = () => session.WithTransactionAsync<bool>(async (s, ct) =>
        {
            await spike.Database.GetCollection<BsonDocument>(SpikeSeeds.Solicitudes)
                .InsertOneAsync(s, new BsonDocument { { "_id", requestId }, { "estado", "Creada" } }, cancellationToken: ct);
            throw new InvalidOperationException("Falla simulada antes de escribir el evento de outbox.");
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
        (await CountAsync(spike.Database, requestId)).Should().Be((0, 0));
    }

    private static async Task<SpikeDatabase> PreparedAsync(string target)
    {
        var spike = await SpikePool.CreateDatabaseAsync(target);
        // Las colecciones se crean antes, como lo harán los scripts de índices del producto.
        await spike.Database.CreateCollectionAsync(SpikeSeeds.Solicitudes);
        await spike.Database.CreateCollectionAsync(SpikeSeeds.Outbox);
        return spike;
    }

    private static async Task InsertRequestAndEventAsync(IMongoDatabase db, IClientSessionHandle session, ObjectId requestId)
    {
        await db.GetCollection<BsonDocument>(SpikeSeeds.Solicitudes)
            .InsertOneAsync(session, new BsonDocument { { "_id", requestId }, { "estado", "Creada" } });
        await db.GetCollection<BsonDocument>(SpikeSeeds.Outbox)
            .InsertOneAsync(session, new BsonDocument { { "tipo", "SolicitudCreada" }, { "solicitudId", requestId } });
    }

    private static async Task<(long Requests, long Events)> CountAsync(IMongoDatabase db, ObjectId requestId)
    {
        var requests = await db.GetCollection<BsonDocument>(SpikeSeeds.Solicitudes)
            .CountDocumentsAsync(new BsonDocument("_id", requestId));
        var events = await db.GetCollection<BsonDocument>(SpikeSeeds.Outbox)
            .CountDocumentsAsync(new BsonDocument("solicitudId", requestId));
        return (requests, events);
    }
}
