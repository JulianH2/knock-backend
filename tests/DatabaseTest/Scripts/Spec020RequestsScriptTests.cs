using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.DatabaseTest.Support;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.DatabaseTest.Scripts;

/// <summary>
/// Spec 020, T-03 (R-17): <c>db/020-requests-solicitudes-cotizaciones.js</c> crea <c>solicitudes</c> y <c>cotizaciones</c> con sus índices en la base de Mercado,
/// reaplicarlo no cambia nada ni borra documentos, y el índice único impide dos cotizaciones del mismo técnico en la misma solicitud.
/// </summary>
public sealed class Spec020RequestsScriptTests
{
    [Fact]
    public async Task Transaction_CommitsQuoteAndOutbox_R_20()
    {
        var documentDb = await DocumentDbFixture.SharedAsync();
        var name = "s020_tx_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        using var client = new MongoClient(documentDb.ConnectionString);
        try
        {
            var db = client.GetDatabase(name);
            await db.CreateCollectionAsync("cotizaciones");
            await db.CreateCollectionAsync("outbox");
            using var session = await client.StartSessionAsync();
            session.StartTransaction();
            await db.GetCollection<BsonDocument>("cotizaciones").InsertOneAsync(session, new BsonDocument("estado", "enviada"));
            await db.GetCollection<BsonDocument>("outbox").InsertOneAsync(session, new BsonDocument("tipo", "CotizacionEnviada"));
            await session.CommitTransactionAsync();
            (await db.GetCollection<BsonDocument>("cotizaciones").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(1);
            (await db.GetCollection<BsonDocument>("outbox").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(1);
        }
        finally
        {
            await client.DropDatabaseAsync(name);
        }
    }

    [Fact]
    public async Task TechnicianIndex_IsCreatedIdempotently_R_23()
    {
        var documentDb = await DocumentDbFixture.SharedAsync();
        var name = "s020_tech_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        using var client = new MongoClient(documentDb.ConnectionString);
        try
        {
            await using var tools = await MongoToolsContainer.StartAsync(documentDb, new Dictionary<string, string> { ["KNOCK_DB_MARKET"] = name });
            var script = $"{MongoToolsContainer.DbDirectory}/021-tecnicos-usuario-index.js";
            (await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script)).ExitCode.Should().Be(0);
            (await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script)).ExitCode.Should().Be(0);
            (await MongoIndexes.DescribeAsync(client.GetDatabase(name), "tecnicos")).Should().Contain("usuarioId_1 usuarioId:1 - ttl:-");
        }
        finally
        {
            await client.DropDatabaseAsync(name);
        }
    }

    [Fact]
    public async Task RequestsScript_CreatesCollectionsAndIndexes_AndReapplyingKeepsThemAndTheirDocuments_R_17()
    {
        var documentDb = await DocumentDbFixture.SharedAsync();
        var name = "s020_market_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        using var client = new MongoClient(documentDb.ConnectionString);
        try
        {
            await using var tools = await MongoToolsContainer.StartAsync(documentDb, new Dictionary<string, string> { ["KNOCK_DB_MARKET"] = name });
            var script = $"{MongoToolsContainer.DbDirectory}/020-requests-solicitudes-cotizaciones.js";

            var first = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);

            first.ExitCode.Should().Be(0, first.Output);
            first.Output.Should().NotContain("Kn0ck", "la salida nunca muestra la contraseña");
            var market = client.GetDatabase(name);
            (await MongoIndexes.DescribeAsync(market, "solicitudes")).Should().Equal(
                "_id_ _id:1 - ttl:-",
                "clienteId_1_creadoEn_-1 clienteId:1,creadoEn:-1 - ttl:-",
                "estado_1_venceEn_1 estado:1,venceEn:1 - ttl:-",
                "oficioClave_1_estado_1 oficioClave:1,estado:1 - ttl:-");
            (await MongoIndexes.DescribeAsync(market, "cotizaciones")).Should().Equal(
                "_id_ _id:1 - ttl:-",
                "solicitudId_1_estado_1_creadoEn_1 solicitudId:1,estado:1,creadoEn:1 - ttl:-",
                "solicitudId_1_tecnicoId_1 solicitudId:1,tecnicoId:1 unique ttl:-",
                "tecnicoUsuarioId_1_creadoEn_-1 tecnicoUsuarioId:1,creadoEn:-1 - ttl:-");

            var kept = new BsonDocument { ["clienteId"] = "c-1", ["estado"] = "enviada" };
            await market.GetCollection<BsonDocument>("solicitudes").InsertOneAsync(kept);
            var second = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);

            second.ExitCode.Should().Be(0, $"reaplicar el script no falla: {second.Output}");
            (await market.GetCollection<BsonDocument>("solicitudes").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(1, "reaplicar no borra datos");
            (await MongoIndexes.DescribeAsync(market, "cotizaciones")).Should().HaveCount(4, "reaplicar no duplica ni cambia los índices");
        }
        finally
        {
            await client.DropDatabaseAsync(name);
        }
    }

    [Fact]
    public async Task UniqueIndex_RejectsASecondQuoteFromTheSameTechnicianOnTheSameRequest_R_17()
    {
        var documentDb = await DocumentDbFixture.SharedAsync();
        var name = "s020_unique_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        using var client = new MongoClient(documentDb.ConnectionString);
        try
        {
            await using var tools = await MongoToolsContainer.StartAsync(documentDb, new Dictionary<string, string> { ["KNOCK_DB_MARKET"] = name });
            (await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", $"{MongoToolsContainer.DbDirectory}/020-requests-solicitudes-cotizaciones.js")).ExitCode.Should().Be(0);
            var quotes = client.GetDatabase(name).GetCollection<BsonDocument>("cotizaciones");
            var request = ObjectId.GenerateNewId();
            var technician = ObjectId.GenerateNewId();
            await quotes.InsertOneAsync(new BsonDocument { ["solicitudId"] = request, ["tecnicoId"] = technician });

            var sameTechnician = () => quotes.InsertOneAsync(new BsonDocument { ["solicitudId"] = request, ["tecnicoId"] = technician });
            var otherTechnician = () => quotes.InsertOneAsync(new BsonDocument { ["solicitudId"] = request, ["tecnicoId"] = ObjectId.GenerateNewId() });
            var otherRequest = () => quotes.InsertOneAsync(new BsonDocument { ["solicitudId"] = ObjectId.GenerateNewId(), ["tecnicoId"] = technician });

            (await sameTechnician.Should().ThrowAsync<MongoWriteException>()).Which.WriteError.Category.Should().Be(ServerErrorCategory.DuplicateKey);
            await otherTechnician.Should().NotThrowAsync();
            await otherRequest.Should().NotThrowAsync();
        }
        finally
        {
            await client.DropDatabaseAsync(name);
        }
    }
}
