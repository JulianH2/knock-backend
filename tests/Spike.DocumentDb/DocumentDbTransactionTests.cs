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

        var (committed, aborted) = await SpikeQueries.CommitAndAbortAsync(spike);

        committed.Should().Be((1, 1));
        aborted.Should().Be((0, 0));
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
        (await SpikeQueries.CountRequestAndEventsAsync(spike.Database, requestId)).Should().Be((0, 0));
    }

    private static async Task<SpikeDatabase> PreparedAsync(string target)
    {
        var spike = await SpikePool.CreateDatabaseAsync(target);
        await SpikeQueries.CreateTransactionCollectionsAsync(spike.Database);
        return spike;
    }
}
