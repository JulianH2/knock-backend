using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.DatabaseTest.Support;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.DatabaseTest.Scripts;

/// <summary>
/// Spec 033, T-03 (§6): <c>db/044-identity-panel.js</c> crea las cuatro colecciones del panel con sus índices y TTL, reaplicarlo no cambia nada ni borra
/// documentos y no toca otras colecciones.
/// </summary>
public sealed class Spec033PanelScriptTests
{
    [Fact]
    public async Task Script_CreaLasCuatroColeccionesConSusIndicesDeFormaIdempotente_R_03_R_15_R_23()
    {
        var documentDb = await DocumentDbFixture.SharedAsync();
        var name = "s033_panel_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        using var client = new MongoClient(documentDb.ConnectionString);
        try
        {
            await using var tools = await MongoToolsContainer.StartAsync(documentDb, new Dictionary<string, string> { ["KNOCK_DB_IDENTITY"] = name });
            var script = $"{MongoToolsContainer.DbDirectory}/044-identity-panel.js";

            var first = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);

            first.ExitCode.Should().Be(0, first.Output);
            var identity = client.GetDatabase(name);
            await identity.GetCollection<BsonDocument>("panel_cuentas").InsertOneAsync(new BsonDocument { ["sub"] = "ficticio" });
            var second = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);

            second.ExitCode.Should().Be(0, $"reaplicar el script no falla: {second.Output}");
            (await identity.GetCollection<BsonDocument>("panel_cuentas").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(1, "reaplicar no borra datos");
            (await MongoIndexes.DescribeAsync(identity, "panel_cuentas")).Should().Equal("_id_ _id:1 - ttl:-", "sub_1 sub:1 unique ttl:-");
            (await MongoIndexes.DescribeAsync(identity, "panel_sesiones")).Should().Equal(
                "_id_ _id:1 - ttl:-",
                "huellaRenovacion_1 huellaRenovacion:1 unique ttl:-",
                "huellasUsadas_1 huellasUsadas:1 - ttl:-",
                "sub_1_revocada_1_creadaEn_1 sub:1,revocada:1,creadaEn:1 - ttl:-",
                "venceEn_1 venceEn:1 - ttl:0");
            (await MongoIndexes.DescribeAsync(identity, "panel_activaciones")).Should().Equal(
                "_id_ _id:1 - ttl:-",
                "creadaEn_1 creadaEn:1 - ttl:86400",
                "cuentaId_1 cuentaId:1 unique ttl:-",
                "huella_1 huella:1 unique ttl:-");
            (await MongoIndexes.DescribeAsync(identity, "panel_auditoria")).Should().Equal(
                "_id_ _id:1 - ttl:-",
                "evento_1_fecha_-1 evento:1,fecha:-1 - ttl:-",
                "fecha_1 fecha:1 - ttl:31536000",
                "sub_1_fecha_-1 sub:1,fecha:-1 - ttl:-");
            using var names = await identity.ListCollectionNamesAsync();
            (await names.ToListAsync()).Order(StringComparer.Ordinal).Should().Equal("panel_activaciones", "panel_auditoria", "panel_cuentas", "panel_sesiones");
        }
        finally
        {
            await client.DropDatabaseAsync(name);
        }
    }

    [Theory]
    [InlineData("nombre con espacios")]
    [InlineData("a;b")]
    public async Task Script_RechazaUnNombreDeBaseNoValido_R_03(string name)
    {
        var documentDb = await DocumentDbFixture.SharedAsync();
        await using var tools = await MongoToolsContainer.StartAsync(documentDb, new Dictionary<string, string> { ["KNOCK_DB_IDENTITY"] = name });

        var result = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", $"{MongoToolsContainer.DbDirectory}/044-identity-panel.js");

        result.ExitCode.Should().NotBe(0);
    }
}
