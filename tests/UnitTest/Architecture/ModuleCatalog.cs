using System.Reflection;
using Knock.Market.Api.Modules.Catalog;

namespace Knock.UnitTest.Architecture;

/// <summary>Lista de módulos de la API y acceso a su ensamblado (fuente única para las pruebas de arquitectura).</summary>
internal static class ModuleCatalog
{
    public const string ModulesNamespace = "Knock.Market.Api.Modules";

    public static readonly IReadOnlyList<string> Names =
    [
        "Catalog",
        "Media",
        "Requests",
    ];

    public static Assembly ApiAssembly => typeof(CatalogModuleExtensions).Assembly;
}
