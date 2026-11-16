using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.DatabaseTest.Support;
using Knock.IntegrationTest.Support;
using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Catalog.Data;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.DatabaseTest.Scripts;

/// <summary>Spec 003, CA-01 (R-05, R-06): <c>db/001_indices.js</c> sobre una instancia limpia crea los índices y reaplicarlo no falla.</summary>
public sealed class Spec003IndexScriptTests
{
    [Fact]
    public async Task IndexScript_OnCleanInstance_CreatesIndexes_AndReapplyingDoesNotFail_CA_003_01()
    {
        var documentDb = await DocumentDbFixture.SharedAsync();
        var suffix = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        var names = new Dictionary<string, string>
        {
            ["KNOCK_DB_MARKET"] = "s003_market_" + suffix,
            ["KNOCK_DB_IDENTITY"] = "s003_identity_" + suffix,
            ["KNOCK_DB_MESSAGING"] = "s003_messaging_" + suffix,
        };
        using var client = new MongoClient(documentDb.ConnectionString);
        try
        {
            await using var tools = await MongoToolsContainer.StartAsync(documentDb, names);
            var script = $"{MongoToolsContainer.DbDirectory}/001_indices.js";

            var first = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);
            first.ExitCode.Should().Be(0, first.Output);
            var market = client.GetDatabase(names["KNOCK_DB_MARKET"]);
            var afterFirst = await SnapshotAsync(client, names.Values);
            var second = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);

            second.ExitCode.Should().Be(0, $"reaplicar el script no falla: {second.Output}");
            (await SnapshotAsync(client, names.Values)).Should().Equal(afterFirst, "reaplicar no cambia los índices");
            first.Output.Should().NotContain("Kn0ck", "la salida nunca muestra la contraseña");
            foreach (var database in names.Values)
            {
                var db = client.GetDatabase(database);
                (await MongoIndexes.DescribeAsync(db, "outbox")).Should().Equal("_id_ _id:1 - ttl:-", "estado_1_creadoEn_1 estado:1,creadoEn:1 - ttl:-");
                (await MongoIndexes.DescribeAsync(db, "tareas_programadas"))
                    .Should().Equal("_id_ _id:1 - ttl:-", "estado_1_programadaPara_1 estado:1,programadaPara:1 - ttl:-");
            }

            // El catálogo declarado en db/ es el mismo que crea CatalogStore, y crearlo desde el código sobre la base del script no falla.
            await using var catalog = await CatalogDatabase.CreateAsync(seeded: false);
            await catalog.Get<CatalogStore>().EnsureIndexesAsync(CancellationToken.None);
            var byScript = await CatalogSnapshotAsync(market);
            (await CatalogSnapshotAsync(catalog.Database)).Should().Equal(byScript);
            await new CatalogStore(market, catalog.Get<IOptions<KnockMongoOptions>>(), catalog.Get<ICatalogCache>()).EnsureIndexesAsync(CancellationToken.None);
            (await CatalogSnapshotAsync(market)).Should().Equal(byScript);
        }
        finally
        {
            foreach (var database in names.Values)
            {
                await client.DropDatabaseAsync(database);
            }
        }
    }

    [Fact]
    public async Task IndexScript_WithInvalidDatabaseName_FailsWithoutCreatingDatabases_CA_003_01()
    {
        var documentDb = await DocumentDbFixture.SharedAsync();
        await using var tools = await MongoToolsContainer.StartAsync(documentDb, new Dictionary<string, string>
        {
            ["KNOCK_DB_MARKET"] = "mala;base",
        });
        var script = $"{MongoToolsContainer.DbDirectory}/001_indices.js";

        var result = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);

        result.ExitCode.Should().NotBe(0, result.Output);
        result.Output.Should().Contain("Nombre de base no válido");
    }

    /// <summary>
    /// CA-01: la comparación de índices incluye <c>expireAfterSeconds</c>; si un índice TTL lo pierde o lo cambia, la descripción
    /// cambia y la comparación falla. Hoy <c>db/001_indices.js</c> no crea TTL; el índice de esta prueba es sintético.
    /// </summary>
    [Fact]
    public async Task IndexDescription_WhenTtlIsLostOrChanged_Differs_CA_003_01()
    {
        var documentDb = await DocumentDbFixture.SharedAsync();
        var name = "s003_ttl_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        using var client = new MongoClient(documentDb.ConnectionString);
        try
        {
            var db = client.GetDatabase(name);
            await db.CreateCollectionAsync("vencibles");
            var indexes = db.GetCollection<BsonDocument>("vencibles").Indexes;
            var key = Builders<BsonDocument>.IndexKeys.Ascending("venceEn");

            await indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(key, new CreateIndexOptions { ExpireAfter = TimeSpan.FromSeconds(3600) }));
            var withTtl = await MongoIndexes.DescribeAsync(db, "vencibles");
            withTtl.Should().Equal("_id_ _id:1 - ttl:-", "venceEn_1 venceEn:1 - ttl:3600");

            await indexes.DropOneAsync("venceEn_1");
            await indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(key, new CreateIndexOptions { ExpireAfter = TimeSpan.FromSeconds(60) }));
            var changed = await MongoIndexes.DescribeAsync(db, "vencibles");
            changed.Should().NotEqual(withTtl, "un TTL distinto se detecta").And.Contain("venceEn_1 venceEn:1 - ttl:60");

            await indexes.DropOneAsync("venceEn_1");
            await indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(key));
            var lost = await MongoIndexes.DescribeAsync(db, "vencibles");
            lost.Should().NotEqual(withTtl, "un TTL perdido se detecta").And.Contain("venceEn_1 venceEn:1 - ttl:-");
        }
        finally
        {
            await client.DropDatabaseAsync(name);
        }
    }

    private static async Task<List<string>> SnapshotAsync(MongoClient client, IEnumerable<string> databases)
    {
        var all = new List<string>();
        foreach (var database in databases.Order(StringComparer.Ordinal))
        {
            var db = client.GetDatabase(database);
            var collections = (await (await db.ListCollectionNamesAsync()).ToListAsync()).Order(StringComparer.Ordinal);
            foreach (var collection in collections)
            {
                all.AddRange((await MongoIndexes.DescribeAsync(db, collection)).Select(index => $"{database}.{collection} {index}"));
            }
        }

        return all;
    }

    private static async Task<List<string>> CatalogSnapshotAsync(IMongoDatabase database) =>
    [
        .. (await MongoIndexes.DescribeAsync(database, CatalogStore.ColoniasCollection)).Select(index => "colonias " + index),
        .. (await MongoIndexes.DescribeAsync(database, CatalogStore.OficiosCollection)).Select(index => "oficios " + index),
    ];
}
