using System.Globalization;
using AwesomeAssertions;
using Knock.Api.Modules.Catalog.Data;
using Knock.DatabaseTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.DatabaseTest.Catalog;

public sealed class CatalogIndexTests
{
    [Fact]
    public async Task CollectionsHaveDeclaredIndexesAndRejectInvalidPolygon_TC_TT_BD_06_03()
    {
        await using var database = await CatalogDatabase.CreateAsync(seeded: true);
        // Polígono «moño» sintético (aristas que se cruzan), lejos de Tula.
        var bowTie = new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "nombre", "Inválida Sintética" },
            { "cp", "99999" },
            { "municipio", "Sintético" },
            { "piloto", true },
            { "aproximado", true },
            { "geom", BsonDocument.Parse("{ type: 'MultiPolygon', coordinates: [[[[30.0, 10.0], [30.01, 10.01], [30.01, 10.0], [30.0, 10.01], [30.0, 10.0]]]] }") },
            { "centro", BsonDocument.Parse("{ type: 'Point', coordinates: [30.005, 10.002] }") },
        };

        var colonias = await IndexesAsync(database.Colonias);
        var oficios = await IndexesAsync(database.Oficios);
        var act = () => database.InScopeAsync<CatalogSeeder>(seeder => seeder.SeedAsync([], [bowTie], CancellationToken.None));

        colonias.Should().BeEquivalentTo(
        [
            ("_id:1", false),
            ("geom:2dsphere", false),
            ("centro:2dsphere", false),
            ("cp:1", false),
            ("nombreBusqueda:1", false),
            ("municipio:1,cp:1,nombre:1", true),
        ]);
        oficios.Should().BeEquivalentTo([("_id:1", false), ("clave:1", true), ("nombreBusqueda:1", false)]);
        await act.Should().ThrowAsync<MongoServerException>("el índice 2dsphere rechaza el polígono que se cruza a sí mismo");
        (await database.Colonias.CountDocumentsAsync(Builders<BsonDocument>.Filter.Eq(CatalogStore.IdField, bowTie["_id"])))
            .Should().Be(0);
    }

    /// <summary>Clave de cada índice como texto (<c>campo:tipo</c>; los números como entero, cada motor los da con otro tipo) y si es único.</summary>
    private static async Task<List<(string Key, bool Unique)>> IndexesAsync(IMongoCollection<BsonDocument> collection)
    {
        var indexes = await (await collection.Indexes.ListAsync()).ToListAsync();
        return indexes.Select(index => (
                string.Join(',', index["key"].AsBsonDocument.Select(element => element.Name + ":" + (element.Value.IsNumeric
                    ? element.Value.ToInt32().ToString(CultureInfo.InvariantCulture)
                    : element.Value.AsString))),
                index.GetValue("unique", false).ToBoolean()))
            .ToList();
    }
}
