using System.Runtime.CompilerServices;
using AwesomeAssertions;
using Knock.Api.Modules.Catalog;
using NetArchTest.Rules;
using Xunit;

namespace Knock.UnitTest.Architecture;

/// <summary>
/// Parte del caso 16 de TT-BD-06 que ya se cumple. Sin el sufijo ni el identificador del caso, a propósito (sdd_lint cuenta
/// los dos): el caso completo (también sin EF Core, Npgsql ni NetTopologySuite) se escribe con su sufijo en la segunda parte,
/// al retirar EF Core.
/// </summary>
public class MongoAccessTests
{
    private const string _fixtureRoot = "Knock.UnitTest.Architecture.Fixtures.Mongo";

    [Fact]
    public void MongoCollectionsOnlyInModuleDataAndClientOnlyInHosting()
    {
        Types.InAssembly(ModuleCatalog.ApiAssembly)
            .That().ResideInNamespace("Knock.Api.Hosting")
            .And().HaveDependencyOnAny(MongoAccessRule.ClientType)
            .GetTypes()
            .Should().NotBeEmpty("el cliente compartido vive en Hosting; si no, la regla del cliente pasaría en vacío");

        var result = MongoAccessRule.Evaluate(ModuleCatalog.ApiAssembly, "Knock.Api");

        result.FailingTypes.Should().BeEmpty();
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void MongoAccessRule_WithViolatingFixture_Fails()
    {
        var violating = MongoAccessRule.Evaluate(typeof(MongoAccessTests).Assembly, $"{_fixtureRoot}.Violating");

        violating.IsSuccessful.Should().BeFalse();
        violating.FailingTypes.Should().Contain(name => name.EndsWith(".AlphaServiceReadsCollection", StringComparison.Ordinal));
        violating.FailingTypes.Should().Contain(name => name.EndsWith(".AlphaDataCreatesClient", StringComparison.Ordinal));
    }

    [Fact]
    public void MongoAccessRule_WithCompliantFixture_Passes()
    {
        var compliant = MongoAccessRule.Evaluate(typeof(MongoAccessTests).Assembly, $"{_fixtureRoot}.Compliant");

        compliant.FailingTypes.Should().BeEmpty();
        compliant.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Catalog_ExposesOnlyAddCatalogModuleAndCatalogApi()
    {
        var publicTypes = ModuleCatalog.ApiAssembly.GetTypes()
            .Where(type => type.IsPublic && type.Namespace?.StartsWith($"{ModuleCatalog.ModulesNamespace}.Catalog", StringComparison.Ordinal) == true)
            .Where(type => !type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false));

        publicTypes.Should().BeEquivalentTo([typeof(CatalogModuleExtensions), typeof(ICatalogApi)]);
    }
}
