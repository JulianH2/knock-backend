using System.Reflection;
using Knock.Api.Modules.Identity;

namespace Knock.UnitTest.Architecture;

/// <summary>Lista de módulos de la API y acceso a su ensamblado (fuente única para las pruebas de arquitectura).</summary>
internal static class ModuleCatalog
{
    public const string ModulesNamespace = "Knock.Api.Modules";

    public static readonly IReadOnlyList<string> Names =
    [
        "Identity", "Catalog", "Requests", "Messaging", "Reputation", "Trust", "Notifications", "Assistant",
    ];

    public static Assembly ApiAssembly => typeof(IdentityModuleExtensions).Assembly;
}
