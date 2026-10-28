using AwesomeAssertions;
using Knock.Api.Modules.Catalog.Data;
using Knock.DatabaseTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.DatabaseTest.Catalog;

public sealed class CatalogSeedTests
{
    [Fact]
    public async Task SeedContainsRequiredActiveTradesWithObjectIdAndNormalizedName_TC_TT_BD_06_01()
    {
        await using var database = await CatalogDatabase.CreateAsync(seeded: true);

        var oficios = await database.Oficios.Find(FilterDefinition<BsonDocument>.Empty)
            .Sort(Builders<BsonDocument>.Sort.Ascending(CatalogStore.ClaveField))
            .ToListAsync();

        oficios.Select(o => (o["clave"].AsString, o["nombre"].AsString, o["modalidad"].AsString, o["activo"].AsBoolean, o["nombreBusqueda"].AsString))
            .Should().Equal(
                ("ele", "Electricidad", "domicilio", true, "ELECTRICIDAD"),
                ("mec", "Mecánica", "taller", true, "MECANICA"),
                ("plo", "Plomería", "domicilio", true, "PLOMERIA"));
        oficios.Select(o => o["_id"]).Should().AllSatisfy(id => id.IsObjectId.Should().BeTrue());
        oficios.Select(o => (o["clave"].AsString, o["_id"].AsObjectId))
            .Should().BeEquivalentTo(CatalogSeedData.Oficios.Select(o => (o["clave"].AsString, o["_id"].AsObjectId)));
        oficios.Should().AllSatisfy(o => o.Names.Should().BeEquivalentTo(
            "_id", "clave", "nombre", "nombreBusqueda", "modalidad", "activo", "version"));
    }

    [Fact]
    public async Task SeedWithInvalidModalidadFailsWithoutWritingAnyTrade_TC_TT_BD_06_01()
    {
        await using var database = await CatalogDatabase.CreateAsync(seeded: false);
        var valid = CatalogSeedData.Oficios[0];
        var invalid = new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "clave", "sin" },
            { "nombre", "Oficio Sintético" },
            { "modalidad", "remoto" },
            { "activo", true },
        };

        var act = () => database.InScopeAsync<CatalogSeeder>(seeder => seeder.SeedAsync([valid, invalid], [], CancellationToken.None));

        await act.Should().ThrowAsync<InvalidDataException>();
        (await database.Oficios.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(0);
    }

    [Fact]
    public async Task ReapplyingSeedKeepsCountsAndIds_TC_TT_BD_06_02()
    {
        await using var database = await CatalogDatabase.CreateAsync(seeded: true);
        var before = await SnapshotAsync(database);

        await Knock.IntegrationTest.Support.CatalogTestDatabase.SeedAsync(database.Services);
        var after = await SnapshotAsync(database);

        before.Oficios.Should().HaveCount(3);
        before.Colonias.Should().HaveCount(12);
        after.Oficios.Should().Equal(before.Oficios, "mismos _id y misma version: el contenido no cambió");
        after.Colonias.Should().Equal(before.Colonias);
        after.Oficios.Select(o => o.Key).Should().OnlyHaveUniqueItems();
        after.Colonias.Select(c => c.Key).Should().OnlyHaveUniqueItems();
    }

    private static async Task<(List<(string Key, ObjectId Id, int Version)> Oficios, List<(string Key, ObjectId Id, int Version)> Colonias)> SnapshotAsync(
        CatalogDatabase database)
    {
        var oficios = await database.Oficios.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        var colonias = await database.Colonias.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        return (
            oficios.Select(o => (o["clave"].AsString, o["_id"].AsObjectId, o["version"].ToInt32())).OrderBy(o => o.Item1, StringComparer.Ordinal).ToList(),
            colonias.Select(c => ($"{c["municipio"]}|{c["cp"]}|{c["nombre"]}", c["_id"].AsObjectId, c["version"].ToInt32()))
                .OrderBy(c => c.Item1, StringComparer.Ordinal).ToList());
    }
}
