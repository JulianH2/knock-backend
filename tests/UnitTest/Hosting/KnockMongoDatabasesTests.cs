using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Xunit;

namespace Knock.UnitTest.Hosting;

public sealed class KnockMongoDatabasesTests
{
    [Fact]
    public void ModulesGetTheirDatabaseByNameFromOneSharedClient_TC_TT_BD_06_18()
    {
        // Catalog → market sale del appsettings.json del repositorio; Identity → identity solo existe en esta prueba.
        using var provider = MongoTestServices.Build(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:KnockMongo"] = MongoTestServices.SyntheticConnectionString(),
                ["Mongo:Databases:Identity"] = "identity",
            },
            withRepositoryAppSettings: true,
            "Catalog",
            "Identity");

        provider.GetRequiredService<IStartupValidator>().Validate();
        var catalog = provider.GetRequiredKeyedService<IMongoDatabase>("Catalog");
        var identity = provider.GetRequiredKeyedService<IMongoDatabase>("Identity");

        catalog.DatabaseNamespace.DatabaseName.Should().Be("market");
        identity.DatabaseNamespace.DatabaseName.Should().Be("identity");
        catalog.Client.Should().BeSameAs(identity.Client);
        catalog.Client.Should().BeSameAs(provider.GetRequiredService<IMongoClient>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("iden tity")]
    [InlineData("market.catalog")]
    public void ModuleWithoutValidDatabaseKey_FailsStartupNamingTheKey_TC_TT_BD_06_18(string? database)
    {
        using var provider = MongoTestServices.Build(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:KnockMongo"] = MongoTestServices.SyntheticConnectionString(),
                ["Mongo:Databases:Identity"] = database,
            },
            withRepositoryAppSettings: true,
            "Catalog",
            "Identity");

        var startup = () => provider.GetRequiredService<IStartupValidator>().Validate();

        var message = startup.Should().Throw<OptionsValidationException>().Which.Message;
        message.Should().Contain("Mongo:Databases:Identity");
        message.Should().NotContain("Mongo:Databases:Catalog", "Catalog sí tiene su base");
    }
}
