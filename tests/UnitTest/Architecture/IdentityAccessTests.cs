using System.Reflection;
using AwesomeAssertions;
using Knock.Api.Modules.Identity.Data;
using NetArchTest.Rules;
using Xunit;

namespace Knock.UnitTest.Architecture;

/// <summary>
/// HU-001 (caso 23): las cuatro colecciones de Identity solo se tocan desde <c>Modules/Identity/Data</c>, ningún tipo del módulo crea un
/// <c>MongoClient</c>, ningún otro módulo usa sus capas internas, el módulo expone solo <c>AddIdentityModule</c> e <c>IIdentityApi</c> y la
/// auditoría no tiene ninguna operación que la actualice o la borre. Complementa la regla general de <c>MongoAccessRule</c> (TT-BD-06).
/// </summary>
public class IdentityAccessTests
{
    private const string _identity = "Knock.Api.Modules.Identity";
    private const string _identityNamespace = "^Knock\\.Api\\.Modules\\.Identity([.]|$)";

    [Fact]
    public void IdentityCollectionsOnlyInDataAndAuditIsAppendOnly_TC_HU_001_23()
    {
        var assembly = ModuleCatalog.ApiAssembly;
        var usingCollections = Types.InAssembly(assembly)
            .That().ResideInNamespaceMatching(_identityNamespace).And().HaveDependencyOnAny(MongoAccessRule.CollectionType)
            .GetTypes().Where(type => !type.FullName.Contains('/', StringComparison.Ordinal)).Select(type => type.FullName).ToList();
        var creatingClients = Types.InAssembly(assembly)
            .That().ResideInNamespaceMatching(_identityNamespace).And().HaveDependencyOnAny(MongoAccessRule.ClientType)
            .GetTypes().ToList();
        var outsideUsingInternals = Types.InAssembly(assembly)
            .That().DoNotResideInNamespaceMatching(_identityNamespace)
            .And().DoNotResideInNamespaceMatching("^Knock\\.Api\\.Hosting([.]|$)")
            .And().HaveDependencyOnAny($"{_identity}.Data", $"{_identity}.Domain", $"{_identity}.Services", $"{_identity}.Dtos")
            .GetTypes().Select(type => type.FullName).ToList();
        var publicTypes = assembly.GetTypes()
            .Where(type => type.IsPublic && type.Namespace is not null && type.Namespace.StartsWith(_identity, StringComparison.Ordinal))
            .Select(type => type.Name).ToList();
        var storeMethods = typeof(IdentityStore).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(method => method.Name).ToList();

        usingCollections.Should().Equal(typeof(IdentityStore).FullName).And.NotBeEmpty("IdentityStore es el único tipo del módulo con IMongoCollection");
        creatingClients.Should().BeEmpty("el MongoClient solo se crea en Hosting/ (AddKnockMongo)");
        outsideUsingInternals.Should().BeEmpty("el resto del código solo conoce el módulo por IIdentityApi (y el registro de Hosting)");
        publicTypes.Should().BeEquivalentTo("IdentityModuleExtensions", "IIdentityApi");
        storeMethods.Should().NotContain(name => name.StartsWith("Update", StringComparison.Ordinal)
                || name.StartsWith("Delete", StringComparison.Ordinal) || name.StartsWith("Remove", StringComparison.Ordinal)
                || name.StartsWith("Replace", StringComparison.Ordinal),
            "la auditoría es solo de inserción: el store no tiene operaciones de cambio ni de borrado");
    }
}
