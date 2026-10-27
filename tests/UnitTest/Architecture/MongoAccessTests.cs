using System.Runtime.CompilerServices;
using System.Text.Json;
using AwesomeAssertions;
using Knock.Api.Modules.Catalog;
using NetArchTest.Rules;
using Xunit;

namespace Knock.UnitTest.Architecture;

/// <summary>Acceso a MongoDB y retiro de EF Core, Npgsql y NetTopologySuite (TT-BD-06, ADR-002).</summary>
public class MongoAccessTests
{
    private const string _fixtureRoot = "Knock.UnitTest.Architecture.Fixtures.Mongo";

    /// <summary>Prefijos de ensamblados y paquetes retirados por TT-BD-06.</summary>
    private static readonly string[] _retired = ["Microsoft.EntityFrameworkCore", "Npgsql", "NetTopologySuite", "EFCore."];

    [Fact]
    public void MongoCollectionsOnlyInDataAndNoEfCoreOrNpgsql_TC_TT_BD_06_16()
    {
        MongoCollectionsOnlyInModuleDataAndClientOnlyInHosting();
        Catalog_ExposesOnlyAddCatalogModuleAndCatalogApi();

        ModuleCatalog.ApiAssembly.GetReferencedAssemblies().Select(reference => reference.Name)
            .Should().NotContain(name => IsRetired(name), "el código de Knock.Api no usa EF Core, Npgsql ni NetTopologySuite");
        RuntimeLibraries().Should().NotContain(name => IsRetired(name), "ni Knock.Api ni sus dependencias traen esos paquetes");
    }

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

    private static bool IsRetired(string? name) =>
        name is not null && _retired.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    /// <summary>Bibliotecas del <c>deps.json</c> de esta suite, que incluye cada paquete que Knock.Api arrastra por la referencia al proyecto.</summary>
    private static List<string> RuntimeLibraries()
    {
        var depsFile = Path.Combine(AppContext.BaseDirectory, $"{typeof(MongoAccessTests).Assembly.GetName().Name}.deps.json");
        using var deps = JsonDocument.Parse(File.ReadAllText(depsFile));
        return deps.RootElement.GetProperty("libraries").EnumerateObject().Select(library => library.Name.Split('/')[0]).ToList();
    }
}
