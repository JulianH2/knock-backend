using System.Reflection;
using System.Text.RegularExpressions;
using NetArchTest.Rules;

namespace Knock.UnitTest.Architecture;

/// <summary>
/// Acceso a MongoDB (constitución, principio 10; TT-BD-06): <c>IMongoCollection&lt;T&gt;</c> solo en <c>Modules/*/Data</c> y el
/// <c>MongoClient</c> solo en <c>Hosting/</c>, que lo comparte entre módulos. El parámetro <c>root</c> de <c>Evaluate</c> es el espacio raíz
/// (<c>Knock.Api</c> o el de un fixture), con <c>.Modules</c> y <c>.Hosting</c> debajo.
/// </summary>
internal static class MongoAccessRule
{
    public const string CollectionType = "MongoDB.Driver.IMongoCollection`1";
    public const string ClientType = "MongoDB.Driver.MongoClient";

    public static BoundaryResult Evaluate(Assembly assembly, string root)
    {
        var inRoot = $"^{Regex.Escape(root)}([.]|$)";
        var moduleData = $"^{Regex.Escape($"{root}.Modules")}[.][^.]+[.]Data([.]|$)";
        var hosting = $"^{Regex.Escape($"{root}.Hosting")}([.]|$)";

        var collections = Types.InAssembly(assembly)
            .That().ResideInNamespaceMatching(inRoot)
            .And().DoNotResideInNamespaceMatching(moduleData)
            .ShouldNot().HaveDependencyOnAny(CollectionType)
            .GetResult();
        var clients = Types.InAssembly(assembly)
            .That().ResideInNamespaceMatching(inRoot)
            .And().DoNotResideInNamespaceMatching(hosting)
            .ShouldNot().HaveDependencyOnAny(ClientType)
            .GetResult();

        var failing = (collections.FailingTypes ?? []).Concat(clients.FailingTypes ?? [])
            .Select(type => type.FullName)
            .ToList();
        return new BoundaryResult(failing.Count == 0, failing);
    }
}
