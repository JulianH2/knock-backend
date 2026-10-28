using AwesomeAssertions;
using Knock.Api.Hosting;
using Knock.Api.Modules.Catalog.Data;
using Knock.Api.Modules.Catalog.Domain;
using Knock.UnitTest.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.UnitTest.Catalog;

public sealed class CatalogStoreMaxTimeTests
{
    [Fact]
    public void EveryCatalogQueryCarriesTheConfiguredMaxTime_TC_TT_BD_06_13()
    {
        // El cliente no se conecta: las consultas solo se arman, nunca se ejecutan.
        using var client = new MongoClient(MongoTestServices.SyntheticConnectionString());
        var store = new CatalogStore(client.GetDatabase("market"), Options.Create(new KnockMongoOptions { QueryMaxTimeSeconds = 3 }));
        var position = new GeoPosition(-99.3495, 20.0505);

        var queries = new[]
        {
            store.ActiveOficiosQuery(),
            store.IntersectingQuery(position),
            store.NearestCenterQuery(position, 2000),
            store.NamesByPostalCodeQuery("42803"),
            store.ColoniaExistsQuery(ObjectId.GenerateNewId(), "42803"),
        };

        queries.Should().OnlyContain(query => query.Options.MaxTime == TimeSpan.FromSeconds(3), "cada consulta lleva maxTimeMS");
    }

    [Fact]
    public void CatalogQueriesUseTheDefaultMaxTimeWhenNotConfigured_TC_TT_BD_06_13()
    {
        using var client = new MongoClient(MongoTestServices.SyntheticConnectionString());
        var store = new CatalogStore(client.GetDatabase("market"), Options.Create(new KnockMongoOptions()));

        store.NamesByPostalCodeQuery("42803").Options.MaxTime.Should().Be(TimeSpan.FromSeconds(5));
    }
}
